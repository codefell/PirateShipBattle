using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using LitJson;
using UnityEngine;

class NetComp
{
    private ConcurrentQueue<byte[]> sendQueue = new ConcurrentQueue<byte[]>();
    private ConcurrentQueue<byte[]> recvQueue = new ConcurrentQueue<byte[]>();
    private string ip = "52.90.82.200";
    //private string ip = "127.0.0.1";
    private int port = 8080;
    private string state = "stop";
    private Thread thread;
    private Socket socket;

    private byte[] readHeadBuff = new byte[4];
    private byte[] readBodyBuff;
    private bool readHead = true;
    private int readOffset = 0;

    private byte[] writeBuff;
    private int writeOffset = 0;

    public NetComp() {
        thread = new Thread(Run);
    }

    public void Start() {
        state = "connecting";
        readOffset = 0;
        readHead = true;
        writeOffset = 0;
        thread.Start();
    }

    public bool isDone {
        get {
            return state == "stop";
        }
    }

    public void End() {
        state = "stop";
        thread.Join();
    }

    private byte[] CreatePacket(byte[] body)
    {
        byte[] packet = new byte[body.Length + 4];
        int len = IPAddress.HostToNetworkOrder(body.Length);
        Array.Copy(BitConverter.GetBytes(len), packet, 4);
        Array.Copy(body, 0, packet, 4, body.Length);
        return packet;
    }

    public void UpdateRead() {
        if (readHead) {
            readOffset += socket.Receive(readHeadBuff,
                                         readOffset,
                                         4 - readOffset,
                                         SocketFlags.None);
            if (readOffset == 4) {
                int len = BitConverter.ToInt32(readHeadBuff, 0);
                len = IPAddress.NetworkToHostOrder(len);
                readBodyBuff = new byte[len];
                readHead = false;
                readOffset = 0;
            }
        }
        if (!readHead) {
            readOffset += socket.Receive(readBodyBuff,
                                         readOffset,
                                         readBodyBuff.Length - readOffset,
                                         SocketFlags.None);
            if (readOffset == readBodyBuff.Length) {
                recvQueue.Enqueue(readBodyBuff);
                readHead = true;
                readOffset = 0;
            }
        }
    }

    public string State
    {
        get
        {
            return this.state;
        }
    }

    public void UpdateWrite() {
        if (writeBuff == null) {
            if (!sendQueue.TryDequeue(out writeBuff)) {
                return;
            }
            writeBuff = CreatePacket(writeBuff);
            writeOffset = 0;
        }
        writeOffset += socket.Send(writeBuff, writeOffset, writeBuff.Length - writeOffset, SocketFlags.None);
        if (writeOffset == writeBuff.Length) {
            writeBuff = null;
        }
    }

    public void Run()
    {
        while (state == "connecting")
        {
            try
            {
                Debug.Log("connecting " + ip + ":" + port);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ip, port);
                state = "connected";
                Debug.Log("connected.");
                break;
            }
            catch (SocketException e)
            {
                Thread.Sleep(1000);
                Debug.Log("retry connect...");
            }
        }

        socket.NoDelay = true;
        socket.Blocking = false;
        List<Socket> readSockList = new List<Socket>();
        List<Socket> writeSockList = new List<Socket>();

        while(state == "connected") {
            try
            {
                readSockList.Clear();
                readSockList.Add(socket);
                writeSockList.Clear();
                if (!sendQueue.IsEmpty)
                {
                    writeSockList.Add(socket);
                }
                Socket.Select(readSockList, writeSockList, null, 1000 / 60 * 1000);
                if (readSockList.Count > 0)
                {
                    UpdateRead();
                }
                if (writeSockList.Count > 0)
                {
                    UpdateWrite();
                }
            } catch (Exception e) {
                //Console.WriteLine("exception " + e);
                state = "stop";
            } 
        }
        socket.Close();
    }

    public void SendJson(JsonData json) {
        SendString(json.ToJson());
    }

    public void SendString(string msg) {
        Send(Encoding.UTF8.GetBytes(msg));
    }

    public void Send(byte[] buff) {
        sendQueue.Enqueue(buff);
    }

    public List<JsonData> RecvJson() {
        List<JsonData> jsonList = new List<JsonData>();
        foreach (var buff in Recv()) {
            jsonList.Add(JsonMapper.ToObject(Encoding.UTF8.GetString(buff)));
        }
        return jsonList;
    }

    public List<String> RecvString() {
        List<String> strList = new List<string>();
        foreach (var buff in Recv()) {
            strList.Add(Encoding.UTF8.GetString(buff));
        }
        return strList;
    }

    public List<byte[]> Recv() {
        List<byte[]> buffList = new List<byte[]>();
        byte[] buff;
        while (recvQueue.TryDequeue(out buff)) {
            buffList.Add(buff);
        }
        return buffList;
    }
}
