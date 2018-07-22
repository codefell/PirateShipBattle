using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LastSyncKfTest : MonoBehaviour {

    public ShipCtrl.KeyFrameSyncInfo last_kf;

    public Vector3 obj_pos;

    public float time_span = 0;

    public float server_start_ts = 0;
    public float client_start_ts = 0;

	// Use this for initialization
	void Start () {
		
	}

    private Vector2 NextPos(ShipCtrl.KeyFrameSyncInfo synckf, float nextTimeSpan)
    {
        Quaternion quaternion = Quaternion.AngleAxis(synckf.angle * Mathf.Rad2Deg, Vector3.down);
        Vector3 vel = quaternion * Vector3.forward;
        Vector3 distance = vel.normalized * synckf.speed * nextTimeSpan * 1.2f;
        return synckf.pos + new Vector2(distance.x, distance.z);
    }

    private void DrawFromTo(Vector2 from, Vector2 to, Color color, float dy)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(from.x, dy, from.y), new Vector3(to.x, dy, to.y));
        Gizmos.DrawSphere(new Vector3(from.x, dy, from.y), 0.5f);

    }

    public void _Update(ShipCtrl.KeyFrameSyncInfo last_kf, float currTime)
    {
        float last_sync_client_ts = last_kf.ts - server_start_ts + client_start_ts;
        Vector2 pos = NextPos(last_kf, Time.time - last_sync_client_ts);
        Util.SetTfm(transform, pos.x, pos.y, last_kf.angle);
    }


    private void OnDrawGizmos()
    {
        Vector2 next_pos = NextPos(last_kf, time_span);
        DrawFromTo(last_kf.pos, next_pos, Color.green, 1);
        DrawFromTo(last_kf.pos, next_pos, Color.red, 2);
    }

    // Update is called once per frame
    void Update () {
		
	}
}
