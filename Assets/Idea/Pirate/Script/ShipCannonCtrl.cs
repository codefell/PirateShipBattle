using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FireShellInfo {
    public float x;
    public float y;
    public float vx;
    public float vy;
    public float speed;
    public float radius;

    public FireShellInfo(float x, float y, float vx, float vy,
                         float speed, float radius) {
        this.x = x;
        this.y = y;
        this.vx = vx;
        this.vy = vy;
        this.speed = speed;
        this.radius = radius;
    }
}

public class ShipCannonCtrl : MonoBehaviour {

    public int id = 0;

    public GameObject shell_prefab;

	// Use this for initialization
	void Start () {
		
	}

    public FireShellInfo GetFireShellInfo() {
        return new FireShellInfo(transform.position.x,
                                 transform.position.z,
                                 transform.forward.x,
                                 transform.forward.z,
                                 0,
                                 0);
        /*
        ShipShellCtrl shell
            = Instantiate(shell_prefab).GetComponent<ShipShellCtrl>();
        shell.Init(transform.position.x, transform.position.z, 
                   transform.forward.x, transform.forward.z, 
                   speed, radius);
        */
    }

	// Update is called once per frame
	void Update () {
		
	}
}
