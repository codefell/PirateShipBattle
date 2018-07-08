using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System;

public class UdpNetComp {

    private ConcurrentQueue<byte[]> pendingSendBuff = new ConcurrentQueue<byte[]>();
    private ConcurrentQueue<byte[]> pendingRecvBuff = new ConcurrentQueue<byte[]>();
    private Thread thread;
    private AutoResetEvent autoResetEvent = new AutoResetEvent(false);
    private string state = "stop";
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
    private string ip;
    private int port;

    public UdpNetComp(string ip, int port)
    {
        this.ip = ip;
        this.port = port;
    }

    public void Start()
    {
        thread = new Thread(Run);
        thread.Start();
    }

    public void End()
    {
        state = "stop";
        udpClient.Close();
    }

    public string State
    {
        get
        {
            return state;
        }
    }

    private void Run()
    {
        udpClient = new UdpClient(ip, port);
        state = "connected";
        while (state == "connected")
        {
            try
            {
                SendPendingBuff();
                RecvPendingBuff();
                autoResetEvent.WaitOne(1000 / 60);
            }
            catch (Exception e)
            {
                Debug.Log("exception " + e);
                End();
                break;
            }
        }
    }

    private void SendPendingBuff()
    {
        byte[] buff;
        while (pendingSendBuff.TryDequeue(out buff))
        {
            udpClient.Send(buff, buff.Length);
        }
    }

    private void RecvPendingBuff()
    {
        while (udpClient.Available > 0)
        {
            byte[] buff = udpClient.Receive(ref remoteEndPoint);
            pendingRecvBuff.Enqueue(buff);
        }
    }

    public void Send(byte[] buff)
    {
        pendingSendBuff.Enqueue(buff);
        autoResetEvent.Set();
    }

    public void SendString(string msg)
    {
        Send(Encoding.UTF8.GetBytes(msg));
    }

    public List<string> RecvString()
    {
        byte[] buff;
        List<String> msgList = new List<string>();
        while (pendingRecvBuff.TryDequeue(out buff))
        {
            msgList.Add(Encoding.UTF8.GetString(buff));
        }
        return msgList;
    }
}
