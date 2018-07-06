using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[System.Serializable]
public class CannonFireEvent : UnityEvent<int> {
    
}

public class CannonButton : MonoBehaviour, IPointerClickHandler {
    public int id = 0;
    public CannonFireEvent cannonFireEvent = new CannonFireEvent();

	// Use this for initialization
	void Start () {
		
	}

    public void OnPointerClick(PointerEventData pointerEventData) {
        cannonFireEvent.Invoke(id);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
