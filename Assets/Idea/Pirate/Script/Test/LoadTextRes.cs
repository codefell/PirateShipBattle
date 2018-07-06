using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using UnityEngine.Networking;

public class LoadTextRes : MonoBehaviour {
	// Use this for initialization
	JsonData jsonData;
	int index = 0;
	float startTime;

	void Start () {
		string info = Resources.Load("info").ToString();
		jsonData = JsonMapper.ToObject(info);
		startTime = Time.time;
		//StartCoroutine(connectServer());
	}

	IEnumerator connectServer() {
		using(UnityWebRequest www = UnityWebRequest.Put("http://127.0.0.1:8000", 
			"{\"state\":\"connected\", \"value\":12}")) {
				www.SetRequestHeader("Content-type", "application/json");
				yield return www.Send();
				if (www.isNetworkError || www.isHttpError) {
					Debug.Log(www.responseCode + ", " + www.error);
				}
				else {
					Debug.Log("receive " + www.downloadHandler.text);
				}
			}
	}

	void GetTransform(int i, out float x, out float y, out float angle) {
		i = Mathf.Min(i, jsonData.Count);
		x = float.Parse(jsonData[i]["pos"]["x"].ToJson());
		y = float.Parse(jsonData[i]["pos"]["y"].ToJson());
		angle = float.Parse(jsonData[i]["angle"].ToJson());
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButtonDown(0)) {
			startTime = Time.time;
			index = 0;
		}
		float x, y, angle;
		GetTransform(index, out x, out y, out angle);
		index = (index + 1) % jsonData.Count;
		transform.position = new Vector3(x, 0, y);
		transform.rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * angle,
			Vector3.down);
	}
}
