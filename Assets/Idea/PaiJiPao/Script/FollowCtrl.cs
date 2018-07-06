using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCtrl : MonoBehaviour {

	public Transform target;
	public Vector3 followPos;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (target.gameObject.activeSelf) {
			transform.position = Quaternion.FromToRotation(Vector3.forward,
				target.GetComponent<Rigidbody>().velocity) * followPos 
				+ target.transform.position;
			transform.LookAt(target.transform);
		}
	}
}
