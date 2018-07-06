using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public Vector3 follow_pos = new Vector3(0, 10, -10);
    public GameObject follow_obj = null;
    public bool look_forward = true;
    public float sample_speed = 0;
    public float sample_interval = 1;
    private float last_sample_time = 0;
	// Use this for initialization
	void Start () {
		
	}

    public void SetLookZDir(bool forward) {
        if (forward) {
            follow_pos.z = -Mathf.Abs(follow_pos.z);
        }
        else {
            follow_pos.z = Mathf.Abs(follow_pos.z);
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (follow_obj != null) {
            SetLookZDir(look_forward);
            Vector3 dest_pos = follow_obj.transform.position + follow_pos;
            if (Time.time - last_sample_time >= sample_interval) {
                sample_speed = (dest_pos - transform.position).magnitude
                                                              / sample_interval;
                last_sample_time = Time.time;
            }
            else {
                transform.position = Vector3.MoveTowards(
                    transform.position, dest_pos, sample_speed * Time.deltaTime);
            }
            transform.LookAt(follow_obj.transform, Vector3.up);
        }
	}
}
