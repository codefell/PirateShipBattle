using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using LitJson;

public class ShipCtrl : MonoBehaviour {
    // Use this for initialization

    public Material[] ship_material;
    public GameObject ship_modal;

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

    // Update is called once per frame
    void Update () {
	}
}
