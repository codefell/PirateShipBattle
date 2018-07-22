using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using LitJson;

public class ShipCtrl : MonoBehaviour {
    // Use this for initialization

    public Material[] ship_material;
    public GameObject ship_modal;
    private GameContext game_context;

    [System.Serializable]
    public class KeyFrameSyncInfo
    {
        public Vector2 pos = Vector2.zero;
        public float angle = 0;
        public float speed = 0;
        public float ts = 0;
    }

    private KeyFrameSyncInfo last_sync_kf;
    private float last_sync_recv_ts = 0;
    private Vector2 pos_last_sync_recv = Vector2.zero;
    private KeyFrameSyncInfo pending_sync_kf;

    private int player_id = 0;
    private int phy_obj_id = 0;

    private Dictionary<int, ShipCannonCtrl> ship_cannons
        = new Dictionary<int, ShipCannonCtrl>();

    public void SetShipModal(int index) {
        ship_modal.GetComponent<Renderer>().material = ship_material[index];
    }

    public int PlayerId
    {
        get
        {
            return player_id;
        }
        set
        {
            player_id = value;
        }
    }

    public int PhyObjId
    {
        get
        {
            return phy_obj_id;
        }
        set
        {
            phy_obj_id = value;
        }
    }

    public void Init(GameContext context)
    {
        game_context = context;
    }
    
    private KeyFrameSyncInfo SyncKfFromJson(JsonData sync_info, float ts)
    {
        KeyFrameSyncInfo info = new KeyFrameSyncInfo();
        info.pos.x = float.Parse(sync_info["x"].ToString());
        info.pos.y = float.Parse(sync_info["y"].ToString());
        info.angle = float.Parse(sync_info["angle"].ToString());
        info.speed = float.Parse(sync_info["speed"].ToString());
        info.ts = ts;
        return info;
    }

    public void SetWorldInfo(JsonData sync_info, float ts)
    {
        Debug.LogFormat("==> Ts {0:F}", ts);
        float client_ts = ts - game_context.server_start_ts + game_context.client_start_ts;
        //SyncKf(SyncKfFromJson(sync_info, ts), client_ts);
        if (client_ts > Time.time)
        {
            if (pending_sync_kf == null || ts >= pending_sync_kf.ts)
            {
                pending_sync_kf = SyncKfFromJson(sync_info, ts);
            }
        }
        else
        {
            if (last_sync_kf == null || ts >= last_sync_kf.ts)
            {
                SyncKf(SyncKfFromJson(sync_info, ts), client_ts);
            }
        }
    }

    private Vector2 NextPos(KeyFrameSyncInfo synckf, float nextTimeSpan)
    {
        Quaternion quaternion = Quaternion.AngleAxis(synckf.angle * Mathf.Rad2Deg, Vector3.down);
        Vector3 vel = quaternion * Vector3.forward;
        Vector3 distance = vel.normalized * synckf.speed * nextTimeSpan;
        return synckf.pos + new Vector2(distance.x, distance.z);
    }

    private void SyncKf(KeyFrameSyncInfo synckf, float client_ts)
    {
        last_sync_kf = synckf;
        last_sync_recv_ts = Time.time;
        pos_last_sync_recv = new Vector2(transform.position.x, transform.position.z);
        //Util.SetTfm(transform, synckf.pos.x, synckf.pos.y, synckf.angle);
    }

    public FireShellInfo FireCannon(int id) {
        if (!ship_cannons.ContainsKey(id)) {
            Debug.LogError("There not cannon id " + id);
            return null;
        }
        ShipCannonCtrl ctrl = ship_cannons[id];
        return ctrl.GetFireShellInfo();
        //ctrl.FireShell();
        //TODO
        //Get and return new fired shell transform information
    }

	void Start () {
        ShipCannonCtrl[] cannonCtrls = GetComponentsInChildren<ShipCannonCtrl>();
        foreach (var ctrl in cannonCtrls) {
            ship_cannons.Add(ctrl.id, ctrl);
        }
	}

    private void DrawFromTo(Vector2 from, Vector2 to, Color color, float dy)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(from.x, dy, from.y), new Vector3(to.x, dy, to.y));
        Gizmos.DrawSphere(new Vector3(from.x, dy, from.y), 0.5f);
    }

    private void OnDrawGizmos()
    {
        if (last_sync_kf != null)
        {
            float last_sync_client_ts = last_sync_kf.ts - game_context.server_start_ts + game_context.client_start_ts;
            Vector2 pos = NextPos(last_sync_kf, Time.time - last_sync_client_ts);
            DrawFromTo(last_sync_kf.pos, pos, Color.red, 0);
            //Util.SetTfm(transform, pos.x, pos.y, last_sync_kf.angle);
            //Util.SetTfm(transform, last_sync_kf.pos.x, last_sync_kf.pos.y, last_sync_kf.angle);
        }
    }

    // Update is called once per frame
    void Update () {
        if (pending_sync_kf != null 
            && (pending_sync_kf.ts - game_context.server_start_ts + game_context.client_start_ts) <= Time.time)
        {
            last_sync_kf = pending_sync_kf;
            pending_sync_kf = null;
            last_sync_recv_ts = Time.time;
        }
        if (last_sync_kf != null)
        {
            float last_sync_client_ts = last_sync_kf.ts - game_context.server_start_ts + game_context.client_start_ts;
            Vector2 pos = NextPos(last_sync_kf, Time.time - last_sync_client_ts);
            Util.SetTfm(transform, pos.x, pos.y, last_sync_kf.angle);
            //Util.SetTfm(transform, last_sync_kf.pos.x, last_sync_kf.pos.y, last_sync_kf.angle);
        }
        /*
        if (last_sync_kf != null)
        {
            Util.SetTfm(transform, last_sync_kf.pos.x, last_sync_kf.pos.y, last_sync_kf.angle);
            float last_sync_client_ts = last_sync_kf.ts - game_context.server_start_ts + game_context.client_start_ts;
            if (Time.time >= last_sync_recv_ts + game_context.simu_time)
            {
                Vector2 pos = NextPos(last_sync_kf, Time.time - last_sync_client_ts);
                Debug.Log("1 pos " + pos);
                Util.SetTfm(transform, pos.x, pos.y, last_sync_kf.angle);
            }
            else
            {
                float t = (Time.time - last_sync_recv_ts) / game_context.simu_time;
                Vector2 pos = NextPos(last_sync_kf, last_sync_recv_ts + game_context.simu_time - last_sync_client_ts);
                Debug.LogFormat("2 {0} {1} {2} {3}", 
                    last_sync_client_ts, last_sync_kf.ts, game_context.server_start_ts, game_context.client_start_ts);
                Debug.Log("2 pos " + pos);
                Vector2 p = Vector2.Lerp(pos_last_sync_recv, pos, t);
                Debug.Log("2 p " + p);
                float angle = Vector2.SignedAngle(Vector2.right, pos - pos_last_sync_recv) * Mathf.Deg2Rad;
                Util.SetTfm(transform, p.x, p.y, angle);
            }
        }
        */
    }
}
