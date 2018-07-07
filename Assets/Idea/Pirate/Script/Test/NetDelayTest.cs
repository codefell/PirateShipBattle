using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetDelayTest : MonoBehaviour {

    private NetComp netComp;
    //private KcpNetComp netComp;
    public float sendInterval = 1;
    public float sampleInterval = 1;
    private int index = 0;
    public LineRenderTest oscilloscope;
    public float singal = 0;

	// Use this for initialization
	void Start () {
        netComp = new NetComp();
        //netComp = new KcpNetComp(123);
        netComp.Start();
        StartCoroutine(SendCo());
        StartCoroutine(SampleCo());
	}

    IEnumerator SendCo()
    {
        while (true)
        {
            if (netComp.State == "connected")
            {
                netComp.SendString("hello " + index++);
            }
            yield return new WaitForSeconds(sendInterval);
        }
    }

    IEnumerator SampleCo()
    {
        while (true)
        {
            if (netComp.State == "connected")
            {
                List<string> msgList = netComp.RecvString();
                singal = msgList.Count / 5f;
            }
            yield return new WaitForSeconds(sampleInterval);
        }
    }
	
	// Update is called once per frame
	void Update () {
        oscilloscope.SetSingal(singal);
    }
}
