using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipShellCtrl : MonoBehaviour {
    public float speed = 10;
    public float radius = 6;
    public Vector3 start_pos = Vector3.zero;
    public Vector3 vel = Vector3.zero;
    public float start_ts = 0;
	// Use this for initialization
	void Start () {
        Init(start_pos.x, start_pos.z, vel.x, vel.z, speed, radius);
	}

    public void Init(float x, float y, float vx, float vy, float speed, float radius) {
        start_pos.x = x;
        start_pos.z = y;
        start_pos.y = 1;
        vel.x = vx;
        vel.z = vy;
        this.speed = speed;
        this.radius = radius;
        transform.position = start_pos;
        vel = vel.normalized * speed;
        start_ts = Time.time;
    }
	
	// Update is called once per frame
	void Update () {
        float delta_ts = Time.time - start_ts;
        Vector3 travel = delta_ts * vel;
        if (travel.magnitude >= radius) {
            Destroy(gameObject);
        }
        transform.position = start_pos + delta_ts * vel;
	}
}
