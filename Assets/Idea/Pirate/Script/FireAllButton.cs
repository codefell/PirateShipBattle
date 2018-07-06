using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class FireAllButton : MonoBehaviour, IPointerClickHandler {
    public UnityEvent fireAllEvent;

    public void OnPointerClick(PointerEventData eventData)
    {
        fireAllEvent.Invoke();
    }

    // Use this for initialization
    void Start () {
		
	}

	// Update is called once per frame
	void Update () {
		
	}
}
