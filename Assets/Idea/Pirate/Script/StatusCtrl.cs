using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class StatusCtrl : MonoBehaviour {

    private Dictionary<string, string> status_map
        = new Dictionary<string, string>();

    private Text status_board;
	// Use this for initialization
	void Start () {
        status_board = GetComponent<Text>();
	}

    public void AddStatus(string key, string init_value) {
        status_map.Add(key, init_value);
        status_board.text = GetBoardStatus();
    }

    public void SetStatus(string key, string value) {
        status_map[key] = value;
        status_board.text = GetBoardStatus();
    }

    public void DelStatus(string key) {
        status_map.Remove(key);
        status_board.text = GetBoardStatus();
    }

    string GetBoardStatus() {
        string status = "";
        foreach (var item in status_map) {
            status += string.Format("{0}: {1}\n", item.Key, item.Value);
        }
        return status;
    }
	
	// Update is called once per frame
	void Update () {

	}
}
