using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchTest : MonoBehaviour {
    public Text text;
	// Use this for initialization
	void Start () {
#if UNITY_ANDROID
        text.text = "android";
#elif UNITY_STANDALONE_WIN
        text.text = "Window";
#endif
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.touchCount > 0) {
            string msg = "";
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                string phase = touch.phase.ToString();
                int tapCount = touch.tapCount;
                Vector2 position = touch.position;
                int fingerId = touch.fingerId;
                msg += string.Format("phase {0}, tapCount {1}, position {2}, fingerId {3}, touchCount {4}\n",
                    phase, tapCount, position, fingerId, Input.touchCount);
            }
            text.text = msg;
        }
	}
}
