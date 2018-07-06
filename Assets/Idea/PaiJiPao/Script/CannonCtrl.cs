using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonCtrl : MonoBehaviour {
	public GameObject shell;
	public float speed = 100;

	public float VertAngleFactor = 1f;
	public float HorzAngleFactor = 1f;

	public FollowCtrl followCtrl;
	public Transform firePos;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		float vertAngle = 0, horzAngle = 0;
		if (Input.GetKey(KeyCode.W)) {
			vertAngle = VertAngleFactor;
		} else if (Input.GetKey(KeyCode.S)) {
			vertAngle = -VertAngleFactor;
		}
		if (Input.GetKey(KeyCode.A)) {
			horzAngle = -HorzAngleFactor;
		} else if (Input.GetKey(KeyCode.D)) {
			horzAngle = HorzAngleFactor;
		}
		transform.rotation = Quaternion.Euler(vertAngle, horzAngle, 0)
			* transform.rotation;
		if (Input.GetMouseButtonDown(0)) {
			GameObject so = Instantiate(shell, firePos.position, firePos.rotation);
			so.GetComponent<Rigidbody>().velocity = 
				firePos.forward * speed;
			followCtrl.target = so.transform;
		}
	}
}
