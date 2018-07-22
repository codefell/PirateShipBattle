using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Text;
using UnityEngine;
using LitJson;

public class KcpUdpNetComp {
    public enum CompState
    {
        stop,
        handshake,
        data
    }
    private string token;
    private string ip;
    private int port;
    private uint conn_id = 1;
    private Dictionary<byte, KcpChannel> kcpChannels = new Dictionary<byte, KcpChannel>();
    private HashSet<byte> udpChannels = new HashSet<byte>();
    private UdpClient udpClient;
    private ConcurrentQueue<Packet> pendingSendBuff
        = new ConcurrentQueue<Packet>();
    private ConcurrentQueue<Packet> pendingRecvBuff
        = new ConcurrentQueue<Packet>();

    private CompState state = CompState.stop;
    private uint lastSendHandShakeTS = 0;
    private System.Diagnostics.Stopwatch stopwatch
        = new System.Diagnostics.Stopwatch();
    private AutoResetEvent autoResetEvent = new AutoResetEvent(false);

    private IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

    private Thread thread;

    public static String dumpBuff(byte[] buff, int length)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < length; i++)
        {
            sb.Append(string.Format("{0:X} ", buff[i]));
        }
        return sb.ToString();
    }

    private class KcpChannel
    {
        public KCP kcp;
        public byte channel;
        public KcpChannel(byte channel, uint conv, Action<byte[], int> sendAction)
        {
            kcp = new KCP(conv, (data, length) =>
            {
                Packet p = new Packet(data, length, channel);
                byte[] buff = p.toBuff();
                Debug.Log("kcp send " + buff.Length + ", " + dumpBuff(buff, buff.Length));
                sendAction(buff, buff.Length);
            });
        }
    }

    public CompState State
    {
        get
        {
            return state;
        }
    }

    public class Packet
    {
        public byte[] data;
        public byte channel;
        public int length;
        public Packet(byte[] data, int length, byte channel)
        {
            this.data = data;
            this.length = length;
            this.channel = channel;
        }

        public byte[] toBuff()
        {
            byte[] packet = new byte[length + 1];
            BitConverter.GetBytes(channel).CopyTo(packet, 0);
            Array.Copy(data, 0, packet, 1, length);
            return packet;
        }

        public static Packet fromBuff(byte[] buff)
        {
            byte channel = buff[0];
            byte[] data = new byte[buff.Length - 1];
            Array.Copy(buff, 1, data, 0, data.Length);
            return new Packet(data, data.Length, channel);
        }
    }

    public KcpUdpNetComp(string ip, int port, string token, List<byte> udpChannels,
        List<byte> kcpChannels)
    {
        this.token = token;
        this.ip = ip;
        this.port = port;
        this.udpChannels = new HashSet<byte>(udpChannels);
        foreach (var c in kcpChannels)
        {
            this.kcpChannels.Add(c, null); //new KcpChannel(c, _SendBuff));
        }
        thread = new Thread(Run);
    }

    private void SendPendingBuff()
    {
        Packet packet;
        while (pendingSendBuff.TryDequeue(out packet))
        {
            if (kcpChannels.ContainsKey(packet.channel))
            {
                kcpChannels[packet.channel].kcp.Send(packet.data);
            }
            else if (udpChannels.Contains(packet.channel))
            {
                byte[] buff = packet.toBuff();
                _SendBuff(buff, buff.Length);
            }
        }
    }

    private void HandShake()
    {
        while (state == CompState.handshake)
        {
            uint currMs = (uint)stopwatch.ElapsedMilliseconds;
            if (lastSendHandShakeTS + 100 <= currMs)
            {
                byte[] tokenBuff = Encoding.UTF8.GetBytes(token);
                Packet packet = new Packet(tokenBuff, tokenBuff.Length, 0);
                byte[] buff = packet.toBuff();
                _SendBuff(buff, buff.Length);
                lastSendHandShakeTS = currMs;
            }
            Thread.Sleep(100);
            while (udpClient.Available > 0)
            {
                byte[] buff = udpClient.Receive(ref remoteEndPoint);
                Packet packet = Packet.fromBuff(buff);
                string conn_id = Encoding.UTF8.GetString(packet.data);
                Debug.Log("Handshake recv buff " + packet.channel + ", " + dumpBuff(packet.data, packet.length));
                if (packet.channel == 0)
                {
                    this.conn_id = uint.Parse(conn_id);
                    byte[] channels = new byte[kcpChannels.Keys.Count];
                    kcpChannels.Keys.CopyTo(channels, 0);
                    foreach (var channel in channels)
                    {
                        uint conv = this.conn_id * 0x100 + channel;
                        kcpChannels[channel] = new KcpChannel(channel, conv, _SendBuff);
                    }
                    state = CompState.data;
                    break;
                }
            }
        }
    }

    private void UpdateUdp()
    {
        while (udpClient.Available > 0)
        {
            byte[] buff = udpClient.Receive(ref remoteEndPoint);
            Packet packet = Packet.fromBuff(buff);

            if (packet.channel == 0)
            {
                continue;
            }

            if (kcpChannels.ContainsKey(packet.channel))
            {
                kcpChannels[packet.channel].kcp.Input(packet.data);
            }
            else if (udpChannels.Contains(packet.channel))
            {
                pendingRecvBuff.Enqueue(packet);
            }
        }
    }

    private void UpdateKcp()
    {
        uint currMs = (uint)stopwatch.ElapsedMilliseconds;
        foreach (var e in kcpChannels)
        {
            byte channel = e.Key;
            KcpChannel kcpChannel = e.Value;
            kcpChannel.kcp.Update(currMs);
            int peekSize = 0;
            while ((peekSize = kcpChannel.kcp.PeekSize()) > 0)
            {
                byte[] buff = new byte[peekSize];
                kcpChannel.kcp.Recv(buff);
                Packet p = new Packet(buff, buff.Length, channel);
                pendingRecvBuff.Enqueue(p);
            }
        }
    }

    private void Run()
    {
        udpClient = new UdpClient(ip, port);
        try
        {
            HandShake();
            while (state != CompState.stop)
            {
                SendPendingBuff();
                UpdateUdp();
                UpdateKcp();
                autoResetEvent.WaitOne(10);
            }
        }
        catch (Exception e)
        {
            Debug.Log("Exception " + e);
            End();
            udpClient.Close();
        }
    }

    private void _SendBuff(byte[] data, int length)
    {
        udpClient.Send(data, length);
    }

    public void Send(byte[] data, byte channel)
    {
        if (udpChannels.Contains(channel) || kcpChannels.ContainsKey(channel))
        {
            pendingSendBuff.Enqueue(new Packet(data, data.Length, channel));
            autoResetEvent.Set();
        }
    }
    
    public void SendJson(JsonData json, byte channel)
    {
        Send(Encoding.UTF8.GetBytes(json.ToJson()), channel);
    }

    public void Start()
    {
        state = CompState.handshake;
        stopwatch.Restart();
        thread.Start();
    }

    public void End()
    {
        state = CompState.stop;
        stopwatch.Stop();
    }

    public List<Packet> RecvPackets()
    {
        List<Packet> packets = new List<Packet>();
        Packet p;
        while (pendingRecvBuff.TryDequeue(out p))
        {
            packets.Add(p);
        }
        return packets;
    }

    public List<JsonData> RecvJson()
    {
        List<JsonData> msg_list = new List<JsonData>();
        Packet p;
        while (pendingRecvBuff.TryDequeue(out p))
        {
            JsonData jd = new JsonData();
            jd["channel"] = p.channel;
            jd["msg"] = JsonMapper.ToObject(Encoding.UTF8.GetString(p.data));
            msg_list.Add(jd);
        }
        return msg_list;
    }
}
