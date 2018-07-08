using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Text;
using UnityEngine;

public class KcpNetComp {
    private KCP kcp;
    private System.Diagnostics.Stopwatch stopwatch
                  = new System.Diagnostics.Stopwatch();
    private AutoResetEvent autoResetEvent
                  = new AutoResetEvent(false);

    private ConcurrentQueue<byte[]> pendingSendBuffer
        = new ConcurrentQueue<byte[]>();
    private ConcurrentQueue<byte[]> pendingRecvBuffer
        = new ConcurrentQueue<byte[]>();

    private UdpClient udpClient;
    private string state = "stop";
    private Thread thread;
    public string ip;
    public int port;
    private IPEndPoint remoteIpEndpoint = new IPEndPoint(IPAddress.Any, 0);

    public KcpNetComp(string ip, int port, uint conv) {
        this.ip = ip;
        this.port = port;
        kcp = new KCP(conv, SendBuff);
        kcp.NoDelay(1, 10, 2, 1);
        thread = new Thread(Run);
    }

    public void Send(byte[] buffer) {
        pendingSendBuffer.Enqueue(buffer);
        autoResetEvent.Set();
    }

    public void SendString(string msg) {
        Send(Encoding.UTF8.GetBytes(msg));
    }

    public List<String> RecvString() {
        List<String> msgList = new List<string>();
        byte[] buff;
        while (pendingRecvBuffer.TryDequeue(out buff)) {
            msgList.Add(Encoding.UTF8.GetString(buff));
        }
        return msgList;
    }

    private void SendBuff(byte[] data, int length) {
        udpClient.Send(data, length);
    }

    private void SendPendingBuffer() {
        byte[] buff;
        while (pendingSendBuffer.TryDequeue(out buff)) {
            //TODO error handle
            kcp.Send(buff);
        }
    }

    private void RecvPendingBuffer() {
        int peekSize = 0;
        while ((peekSize = kcp.PeekSize()) > 0) {
            byte[] buff = new byte[peekSize];
            kcp.Recv(buff);
            pendingRecvBuffer.Enqueue(buff);
        }
    }

    public void Start() {
        thread.Start();
    }

    public void End() {
        state = "stop";
        udpClient.Close();
    }

    public string State {
        get {
            return state;
        }
    }

    private void UpdateKcpInput() {
        while (udpClient.Available > 0) {
            byte[] buff = udpClient.Receive(ref remoteIpEndpoint);
            kcp.Input(buff);
        }
    }

    private void Run() {
        udpClient = new UdpClient(ip, port);
        state = "connected";
        stopwatch.Start();
        while (state == "connected") {
            try
            {
                SendPendingBuffer();
                UpdateKcpInput();
                uint currMs = (uint)stopwatch.ElapsedMilliseconds;
                kcp.Update(currMs);
                RecvPendingBuffer();
                uint nextTime = kcp.Check(currMs);
                uint sleepTime = nextTime - currMs;
                //Debug.Log("next time " + (sleepTime/ 1000f));
                if (nextTime > 0)
                {
                    bool ret = autoResetEvent.WaitOne(1000 / 60);
                    //Debug.Log("event ret " + ret);
                }
            }
            catch (Exception e) {
                Debug.Log("Exception " + e);
                End();
                break;
            }
        }
    }
}
