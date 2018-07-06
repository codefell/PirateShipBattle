using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContentPandingCtrl : MonoBehaviour {
	public Text text;
	// Use this for initialization
	void Start () {
		text = GetComponent<Text>();
		Debug.Log(text.minWidth);
		Debug.Log(text.minHeight);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
