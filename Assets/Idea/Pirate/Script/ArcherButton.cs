using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[System.Serializable]
public class ArcherSwitchEvent : UnityEvent<bool> { }

public class ArcherButton : MonoBehaviour, IPointerClickHandler {
    private Animator animator;
    private bool archerDown = true;
    public ArcherSwitchEvent archerSwitchEvent;
	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();
	}

	// Update is called once per frame
	void Update () {
		
	}

    public void OnPointerClick(PointerEventData eventData)
    {
        archerDown = !archerDown;
        animator.SetBool("down", archerDown);
        archerSwitchEvent.Invoke(archerDown);
    }
}
