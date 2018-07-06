using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipCannonUICtrl : MonoBehaviour {

    public GameCtrl game_ctrl;

	// Use this for initialization
	void Start () {
		
	}

    public void FireCannon(int id) {
        game_ctrl.FireShell(id);
        //ship_ctrl.FireCannon(id);
        //TODO = 
        //call ship_ctrl.FireCannon to get shell transform information
        //call game ctrl fire bullet
    }
	
	// Update is called once per frame
	void Update () {
	}
}
