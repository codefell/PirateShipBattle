using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UdpTest : MonoBehaviour {

    private UdpNetComp udpNetComp;

    // Use this for initialization
    void Start()
    {
        udpNetComp = new UdpNetComp("127.0.0.1", 8080);
        udpNetComp.Start();
        StartCoroutine(SendCo());
    }

    IEnumerator SendCo()
    {
        while (true)
        {
            if (udpNetComp.State == "connected")
            {
                Test();
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private int index = 0;

    public void Test()
    {
        udpNetComp.SendString("hello " + index++);
    }

    void OnDestroy()
    {
        udpNetComp.End();
    }

    // Update is called once per frame
    void Update()
    {
        List<string> msgList = udpNetComp.RecvString();
        foreach (var msg in msgList)
        {
            Debug.Log("recv " + msg);
        }
    }
}
