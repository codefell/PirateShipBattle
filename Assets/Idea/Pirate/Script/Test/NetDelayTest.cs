using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class NetDelayTest : MonoBehaviour {

    private NetComp tcpNetComp;
    private KcpNetComp kcpNetComp;
    private UdpNetComp udpNetComp;
    public float sendInterval = 1;
    public float sampleInterval = 1;
    private int index = 0;
    public LineRenderTest tcpOscilloscope;
    public LineRenderTest kcpOscilloscope;
    public LineRenderTest udpOscilloscope;

    public float tcpSingal = 0;
    public float kcpSingal = 0;
    public float udpSingal = 0;
    private string ec2_ip = "52.90.82.200";
    private string local_ip = "127.0.0.1";
    public int tcpPort = 8080;
    public int kcpPort = 8080;
    public int udpPort = 8081;
    public bool useEc2 = false;
    FileStream singalFileStream;

	// Use this for initialization
	void Start () {
        singalFileStream = new FileStream("singal.txt", FileMode.Create, FileAccess.Write);
        string ip = useEc2 ? ec2_ip : local_ip;
        tcpNetComp = new NetComp(ip, tcpPort);
        kcpNetComp = new KcpNetComp(ip, kcpPort, 123);
        udpNetComp = new UdpNetComp(ip, udpPort);
        tcpNetComp.Start();
        udpNetComp.Start();
        kcpNetComp.Start();
        StartCoroutine(SendCo());
        StartCoroutine(SampleCo());
	}

    private void OnDestroy()
    {
        tcpNetComp.End();
        kcpNetComp.End();
        udpNetComp.End();
        singalFileStream.Close();
    }

    IEnumerator SendCo()
    {
        while (true)
        {
            string msg = "hello " + index;
            if (tcpNetComp.State == "connected")
            {
                tcpNetComp.SendString(msg);
            }
            if (kcpNetComp.State == "connected")
            {
                kcpNetComp.SendString(msg);
            }
            if (udpNetComp.State == "connected")
            {
                udpNetComp.SendString(msg);
            }
            index++;
            yield return new WaitForSeconds(sendInterval);
        }
    }

    IEnumerator SampleCo()
    {
        while (true)
        {
            if (tcpNetComp.State == "connected")
            {
                List<string> msgList = tcpNetComp.RecvString();
                tcpSingal = msgList.Count / 5f;
            }
            if (kcpNetComp.State == "connected")
            {
                List<string> msgList = kcpNetComp.RecvString();
                kcpSingal = msgList.Count / 5f;
            }
            if (udpNetComp.State == "connected")
            {
                List<string> msgList = udpNetComp.RecvString();
                udpSingal = msgList.Count / 5f;
            }
            string data = string.Format("{0} {1} {2}\r\n", tcpSingal, kcpSingal, udpSingal);
            byte[] chunk = Encoding.UTF8.GetBytes(data);
            singalFileStream.Write(chunk, 0, chunk.Length);
            yield return new WaitForSeconds(sampleInterval);
        }
    }
	
	// Update is called once per frame
	void Update () {
        tcpOscilloscope.SetSingal(tcpSingal);
        kcpOscilloscope.SetSingal(kcpSingal);
        udpOscilloscope.SetSingal(udpSingal);
    }
}
