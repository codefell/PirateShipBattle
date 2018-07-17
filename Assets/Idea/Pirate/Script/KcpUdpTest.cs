using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class KcpUdpTest : MonoBehaviour {
    public bool use_ec2 = false;
    public int port = 8080;
    public string token = "hello";

    public Text text_udp_1;
    public Text text_udp_2;
    public Text text_kcp_1;
    public Text text_kcp_2;
    public InputField input;

    private float lastUdp0PacketTs = 0;
    private float lastUdp1PacketTs = 0;
    private float lastKcp0PacketTs = 0;
    private float lastKcp1PacketTs = 0;

    private float udp0_interval = 0;
    private float udp1_interval = 0;
    private float kcp0_interval = 0;
    private float kcp1_interval = 0;

    private int udp_index0 = 0;
    private int udp_index1 = 1;
    private int kcp_index0 = 0;
    private int kcp_index1 = 1;

    private KcpUdpNetComp kcpUdpNetComp;
    // Use this for initialization

    void Start()
    {

    }

    public void Init () {
        string cmd = input.text;
        string[] args = cmd.Split(new char[] { ' ' });
        if (args.Length != 9)
        {
            Debug.Log("usage: token udp0_start_index udp1_index udp1_interval...");
        }
        else
        {
            string ip = use_ec2 ? "52.90.82.200" : "127.0.0.1";
            kcpUdpNetComp = new KcpUdpNetComp(ip, port, args[0],
                new List<byte> { 1, 2 },
                new List<byte> { 3, 4 });
            kcpUdpNetComp.Start();
            lastUdp0PacketTs = Time.time;
            lastUdp1PacketTs = Time.time;
            lastKcp0PacketTs = Time.time;
            lastKcp1PacketTs = Time.time;
            udp_index0 = int.Parse(args[1]);
            udp0_interval = float.Parse(args[2]);
            udp_index1 = int.Parse(args[3]);
            udp1_interval = float.Parse(args[4]);
            kcp_index0 = int.Parse(args[5]);
            kcp0_interval = float.Parse(args[6]);
            kcp_index1 = int.Parse(args[7]);
            kcp1_interval = float.Parse(args[8]);
        }
    }

    private void OnDestroy()
    {
        if (kcpUdpNetComp != null)
        {
            kcpUdpNetComp.End();
        }
    }

    // Update is called once per frame
    void Update () {
        if (kcpUdpNetComp == null || kcpUdpNetComp.State != KcpUdpNetComp.CompState.data)
        {
            return;
        }
        List<KcpUdpNetComp.Packet> packets = kcpUdpNetComp.RecvPackets();
        foreach (var p in packets)
        {
            string msg = Encoding.UTF8.GetString(p.data);
            switch(p.channel)
            {
                case 1:
                    text_udp_1.text += "\n" + msg;
                    break;
                case 2:
                    text_udp_2.text += "\n" + msg;
                    break;
                case 3:
                    text_kcp_1.text += "\n" + msg;
                    break;
                case 4:
                    text_kcp_2.text += "\n" + msg;
                    break;
                default:
                    Debug.LogError("unknow channel");
                    break;
            }
        }
        if (kcpUdpNetComp.State == KcpUdpNetComp.CompState.data)
        {
            if (Time.time - lastUdp0PacketTs > udp0_interval)
            {
                kcpUdpNetComp.Send(Encoding.UTF8.GetBytes("channel_1 " + udp_index0++), 1);
                lastUdp0PacketTs = Time.time;
            }
            if (Time.time - lastUdp1PacketTs > udp1_interval)
            {
                kcpUdpNetComp.Send(Encoding.UTF8.GetBytes("channel_2 " + udp_index1++), 2);
                lastUdp1PacketTs = Time.time;
            }
            if (Time.time - lastKcp0PacketTs > kcp0_interval)
            {
                kcpUdpNetComp.Send(Encoding.UTF8.GetBytes("channel_3 " + kcp_index0++), 3);
                lastKcp0PacketTs = Time.time;
            }
            if (Time.time - lastKcp1PacketTs > kcp1_interval)
            {
                kcpUdpNetComp.Send(Encoding.UTF8.GetBytes("channel_4 " + kcp_index1++), 4);
                lastKcp1PacketTs = Time.time;
            }
        }
	}
}
