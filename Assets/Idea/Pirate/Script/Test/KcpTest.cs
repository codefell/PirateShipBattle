using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KcpTest : MonoBehaviour {

    private KcpNetComp kcpNetComp;

	// Use this for initialization
	void Start () {
        kcpNetComp = new KcpNetComp("127.0.0.1", 8080, 123);
        kcpNetComp.Start();
        StartCoroutine(SendCo());
	}

    IEnumerator SendCo() {
        while (true) {
            if (kcpNetComp.State == "connected") {
                Test();
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private int index = 0;

    public void Test() {
        kcpNetComp.SendString("hello " + index++);
    }

    void OnDestroy()
    {
        kcpNetComp.End();
    }

    // Update is called once per frame
    void Update () {
        List<string> msgList = kcpNetComp.RecvString();
        foreach (var msg in msgList) {
            Debug.Log("recv " + msg);
        }
	}
}
