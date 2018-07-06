using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HelmCtrl : MonoBehaviour {
	private RectTransform rectTransform;
	// Use this for initialization
	public bool inCtrl = false;
    public StatusCtrl statusCtrl;
	private Vector2 lastMousePosition;
    private Vector2 originMousePosition;
    private float helm_angle = 0;
    public float exp = 0.9f;
    public float exp_angle_step = 30;

    [System.Serializable]
    public class HelmRollEvent : UnityEvent<float> {}

    public HelmRollEvent helm_roll_event;

	void Start () {
		rectTransform = GetComponent<RectTransform>();
        statusCtrl.AddStatus("angle", "0");
	}

	Vector2 GetLocalPointerPos(Vector2 pointer_position) {
		Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            pointer_position,
            null, out localPoint);
        localPoint = localPoint -
            (Vector2)rectTransform.localPosition;
        return localPoint;
	}

    private float factor_helm(float angle) {
        //return angle;
        return Mathf.Pow(exp, Mathf.Abs(angle) / exp_angle_step);
    }

    public bool TouchMove(ref Vector2 position)
    {
#if UNITY_ANDROID
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                position = touch.position;
                return true;
            }
        }
#elif UNITY_STANDALONE_WIN
        if (Input.GetMouseButton(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif
        return false;
    }

	// Update is called once per frame
	void Update () {
		Vector2 currMousePosition = Vector2.zero;
        Vector2 pointer_position = Vector2.zero;
        if (TouchMove(ref pointer_position))
        {
            pointer_position = GetLocalPointerPos(pointer_position);
            if (!inCtrl)
            {
                lastMousePosition = pointer_position;
                originMousePosition = lastMousePosition;
                inCtrl = true;
            }
        }
        else
        {
			if (inCtrl) {
                rectTransform.localRotation = Quaternion.identity;
                helm_angle = 0;
                helm_roll_event.Invoke(helm_angle);
                inCtrl = false;
            }

        }
		if (inCtrl) {
            currMousePosition = pointer_position;
            Quaternion quaternion = 
                Quaternion.FromToRotation(lastMousePosition,
                    currMousePosition);
            float z_angle = quaternion.eulerAngles.z;
            if (z_angle > 180)
            {
                z_angle = z_angle - 360;
            }
            float factor = 1;
            if (Mathf.Sign(z_angle) * Mathf.Sign(helm_angle) >= 0) {
                factor = factor_helm(helm_angle);
            }
            quaternion = Quaternion.Euler(0, 0, z_angle * factor); 
            rectTransform.localRotation *= quaternion;
            z_angle = quaternion.eulerAngles.z;
            if (z_angle > 180) {
                z_angle = z_angle - 360;
            }
            helm_angle += z_angle;
            statusCtrl.SetStatus("angle", helm_angle.ToString());
			lastMousePosition = currMousePosition;
            helm_roll_event.Invoke(helm_angle);
		}
	}
}
