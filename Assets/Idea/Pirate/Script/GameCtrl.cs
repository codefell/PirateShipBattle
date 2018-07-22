using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using LitJson;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameContext
{
    public float server_start_ts = 0;
    public float client_start_ts = 0;
    public float simu_time = 1f;
}

public class GameCtrl : MonoBehaviour {

    private int player_id = 0; 
    private int ship_id = 0; 
    //Use kcp/udp as connection
    private KcpUdpNetComp netComp;
    private List<JsonData> recvMsgList = new List<JsonData>();
    public string aliyun_ip = "47.94.95.39";
    public string aws_ip = "52.90.82.200";
    public string local_ip = "127.0.0.1";
    private string server_ip;
    private GameContext context = new GameContext();

    private delegate void MsgResHandle(JsonData msg);
    private Dictionary<string, MsgResHandle> msgResMap;
    private Dictionary<int, ShipCtrl> ship_id_to_ctrl 
        = new Dictionary<int, ShipCtrl>();
    private Dictionary<int, int> player_id_to_ship_id
        = new Dictionary<int, int>();

    private Dictionary<int, ShipShellCtrl> bullet_id_to_ctrl
        = new Dictionary<int, ShipShellCtrl>();

    public GameObject objPrefab;
    public Text text;
    public ShipCannonCtrl ship_cannon_ctrl;
    public GameObject explosion_prefab;
    public GameObject shell_prefab;
    public float shell_speed = 10;
    public float shell_radius = 30;
    private ShipCtrl shipCtrl;
    public CameraFollow camera_follow;
    public float ship_speed = 2;
    private bool look_forward = true;
    public AudioSource audio_souce;

    public float helm_scale = 1;
    private bool login = false;
    private bool game_start = false;

    public void FireShell(int cannon_id) {
        FireShellInfo fireShellInfo = shipCtrl.FireCannon(cannon_id);
        JsonData msg = new JsonData();
        msg["type"] = "fire_bullet";
        msg["x"] = fireShellInfo.x;
        msg["y"] = fireShellInfo.y;
        msg["vx"] = fireShellInfo.vx;
        msg["vy"] = fireShellInfo.vy;
        msg["speed"] = shell_speed;
        msg["radius"] = shell_radius;
        msg["cannon_id"] = cannon_id;
        netComp.SendJson(msg, 1);
    }

    void Start()
    {
        //Screen.SetResolution(800, 600, false);
        msgResMap = new Dictionary<string, MsgResHandle>() {
            {"login", this.OnLogin},
            {"players_ready", this.OnPlayersReady},
            {"world_info", this.OnWorldInfo},
            {"start", this.OnStart},
            {"fire_bullet", this.OnFireBullet},
            {"bullet_hit", this.OnBulletHit},
        };
        server_ip = local_ip;
    }

    public void StartGame()
    {
        //Debug.Log(server_ip);
        StartCoroutine(StartConnCo());
    }

    public void UseLocalIP(bool v)
    {
        if (v)
        {
            server_ip = local_ip;
        }
    }

    public void UseAliyunIP(bool v)
    {
        if (v)
        {
            server_ip = aliyun_ip;
        }
    }

    public void UserAwsIp(bool v)
    {
        if (v)
        {
            server_ip = aws_ip;
        }
    }

    IEnumerator StartConnCo()
    {
        JsonData jd = new JsonData();
        jd["hello"] = "world";
        jd["python"] = "ipython";
        byte[] buff = Encoding.UTF8.GetBytes(jd.ToJson());
        using (UnityWebRequest req = UnityWebRequest.Put(
            string.Format("http://{0}:8080/api/login", server_ip), buff))
        {
            req.SetRequestHeader("Content-Type", "application/json");
            Debug.Log("send request");
            yield return req.SendWebRequest();
            Debug.Log("send request end");
            if (req.isNetworkError || req.isHttpError)
            {
                Debug.Log(req.error);
            }
            else
            {
                JsonData json = JsonMapper.ToObject(req.downloadHandler.text);
                string token = json["token"].ToString();
                Debug.Log("fetch token complete " + token);
                netComp = new KcpUdpNetComp(server_ip, 8080,
                    token, new List<byte> { 2 }, new List<byte> { 1 });
                netComp.Start();
                ReqLogin();
            }
        }
    }

    public void ReqLogin()
    {
        if (login) return;
        login = true;
        JsonData msg = new JsonData();
        msg["type"] = "login";
        netComp.SendJson(msg, 1);
    }

    public void ReqSetAngularVel(int id, float omega) {
        JsonData msg = new JsonData();
        msg["type"] = "set_angular_vel";
        msg["omega"] = (double)omega;
        netComp.SendJson(msg, 2);
    }

    public void ReqSetVel(int id, float x, float y) {
        JsonData msg = new JsonData();
        msg["type"] = "set_vel";
        msg["id"] = id;
        msg["x"] = (double)x;
        msg["y"] = (double)y;
        netComp.SendJson(msg, 1);
    }

    public void ReqSetSpeed(int id, float speed) {
        JsonData msg = new JsonData();
        msg["type"] = "set_speed";
        msg["id"] = id;
        msg["speed"] = speed;
        netComp.SendJson(msg, 1);
    }

    private int GetJsonInt(JsonData json, string key) {
        return int.Parse(json[key].ToString());
    }

    private float GetJsonFloat(JsonData json, string key)
    {
        return float.Parse(json[key].ToString());
    }

    private void OnFireBullet(JsonData json) {
        int bullet_id = GetJsonInt(json, "id");
        float x = GetJsonFloat(json, "x");
        float y = GetJsonFloat(json, "y");
        float vx = GetJsonFloat(json, "vx");
        float vy = GetJsonFloat(json, "vy");
        float speed = GetJsonFloat(json, "speed");
        float radius = GetJsonFloat(json, "radius");
        ShipShellCtrl shell 
            = Instantiate(shell_prefab).GetComponent<ShipShellCtrl>();
        shell.Init(x, y, vx, vy, speed, radius);
        bullet_id_to_ctrl.Add(bullet_id, shell);

        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.Play();

        Debug.Log(string.Format("create bullet {0} {1} {2} {3} {4} {5}",
                                x, y, vx, vy, speed, radius));
    }

    private void OnBulletHit(JsonData json) {
        int bullet_id = GetJsonInt(json, "bullet_id");
        int body_id = GetJsonInt(json, "body_id");
        float x = GetJsonFloat(json, "x");
        float y = GetJsonFloat(json, "y");
        Instantiate(explosion_prefab, new Vector3(x, 0.5f, y), Quaternion.identity);
        if (bullet_id_to_ctrl.ContainsKey(bullet_id)) {
            ShipShellCtrl shipShellCtrl = bullet_id_to_ctrl[bullet_id];
            Destroy(shipShellCtrl.gameObject);
        }
        Debug.Log(string.Format("bullet hit {0}, {1}, {2}, {3}", bullet_id, body_id, x, y));
    }

    private void OnPlayersReady(JsonData json) {
        JsonData players = json["players"];
        int count = players.Count;
        for (int i = 0; i < count; i++) {
            JsonData player_info = players[i];
            int player_id = int.Parse(player_info["id"].ToString());
            int ship_id = int.Parse(player_info["ship_id"].ToString());
            look_forward = bool.Parse(player_info["look_forward"].ToString());
            JsonData tfm = player_info["tfm"];
            float x = float.Parse(tfm["x"].ToString());
            float y = float.Parse(tfm["y"].ToString());
            float w = float.Parse(tfm["w"].ToString());
            float h = float.Parse(tfm["h"].ToString());
            float angle = float.Parse(tfm["angle"].ToString());

            GameObject o = Instantiate(objPrefab);
            //o.transform.GetChild(0).localScale = new Vector3(w, 1, h);
            Util.SetTfm(o.transform, x, y, angle);
            ShipCtrl shipCtrl = o.GetComponent<ShipCtrl>();
            ship_id_to_ctrl.Add(ship_id, o.GetComponent<ShipCtrl>());
            player_id_to_ship_id.Add(player_id, ship_id);

            shipCtrl.SetShipModal(look_forward ? 1 : 0);

            if (player_id == this.player_id) {
                this.ship_id = ship_id;
                this.shipCtrl = shipCtrl;
                camera_follow.look_forward = look_forward;
                camera_follow.follow_obj = shipCtrl.gameObject;
            }
        }
        /*
        int objId = int.Parse(json["id"].ToString());
        Log("Create Object " + objId);
        JsonData tfm = json["tfm"];
        float x = float.Parse(tfm[0].ToString());
        float y = float.Parse(tfm[1].ToString());
        float w = float.Parse(tfm[2].ToString());
        float h = float.Parse(tfm[3].ToString());
        float angle = float.Parse(tfm[4].ToString());
        GameObject o = Instantiate(objPrefab);
        o.transform.GetChild(0).localScale = new Vector3(w, 1, h);
        Util.SetTfm(o.transform, x, y, angle);
        objectMap.Add(objId, o);
        */
    }

    private void Log(string logMsg) {
        text.text += logMsg + "\n";
    }

    private void OnLogin(JsonData json)
    {
        player_id = int.Parse(json["id"].ToString());
        Log("OnLogin " + player_id);
    }

    private void OnDestroy()
    {
        if (netComp != null)
        {
            netComp.End();
        }
    }

    public void End()
    {
        if (netComp != null)
        {
            netComp.End();
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    private void OnStart(JsonData json)
    {
        ship_id = player_id_to_ship_id[player_id];
        shipCtrl = ship_id_to_ctrl[ship_id];
        audio_souce.loop = true;
        audio_souce.PlayDelayed(1);
        game_start = true;
        context.server_start_ts = float.Parse(json["ts"].ToString());
        context.client_start_ts = Time.time;
        foreach (var e in ship_id_to_ctrl)
        {
            e.Value.Init(context);
        }
        /*
        camera_follow.look_forward = look_forward;
        camera_follow.follow_obj = shipCtrl.gameObject;
        */
        Debug.Log("OnStart");
    }

    private void OnWorldInfo(JsonData worldInfo)
    {
        Debug.Log("OnWorldInfo " + worldInfo.ToJson());
        Debug.Log(worldInfo["ts"].ToString());
        float ts = float.Parse(worldInfo["ts"].ToString());
        Debug.LogFormat("{0:F} {1:F}", ts, context.server_start_ts);
        worldInfo = worldInfo["world_info"];
        int count = worldInfo.Count;
        for (int i = 0; i < count; i++)
        {
            int id = int.Parse(worldInfo[i]["id"].ToString());
            if (ship_id_to_ctrl.ContainsKey(id))
            {
                JsonData tfm = worldInfo[i]["tfm"];
                float x = float.Parse(tfm["x"].ToString());
                float y = float.Parse(tfm["y"].ToString());
                float angle = float.Parse(tfm["angle"].ToString());
                ShipCtrl o = ship_id_to_ctrl[id];
                o.SetWorldInfo(tfm, ts);
                //Util.SetTfm(o.transform, x, y, angle);
            }
        }
    }

    private void DispatchMsg(JsonData json)
    {
        string type = json["type"].ToString();
        //if (type != "world_info")
        Debug.Log("dispatch " + json.ToJson());
        MsgResHandle handle = msgResMap[type];
        handle(json);
    }

    private void UpdateNet()
    {
        List<JsonData> msgList = netComp.RecvJson();
        foreach (var msg in msgList)
        {
            //Debug.Log("recv msg " + msg.ToJson());
            DispatchMsg(msg["msg"]);
        }
    }

    public void OnHelmRoll(float angle) {
        if (game_start)
        {
            ReqSetAngularVel(ship_id, helm_scale * angle * Mathf.Deg2Rad);
        }
    }

    public void SwitchMove(bool stop_move) {
        Debug.Log("Switch Move " + stop_move + ", " + game_start + ", " + stop_move);
        if (game_start)
        {
            if (stop_move)
            {
                ReqSetSpeed(ship_id, 0);
            }
            else
            {
                ReqSetSpeed(ship_id, ship_speed);
            }
        }
    }

    public void FireAll()
    {
        if (game_start)
        {
            FireShell(0);
            FireShell(1);
            FireShell(2);
            FireShell(3);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (netComp != null && netComp.State == KcpUdpNetComp.CompState.data)
        {
            UpdateNet();
        }
    }
}
