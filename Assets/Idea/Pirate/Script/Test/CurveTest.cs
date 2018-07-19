using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurveTest : MonoBehaviour {
    public AnimationCurve curve = new AnimationCurve();
    private LineRenderer lineRender;
    public float width = 10;
    public float height = 5;
    public float maxTime = 0;

	// Use this for initialization
	void Start () {
        lineRender = GetComponent<LineRenderer>();
        maxTime = curve.keys[curve.length - 1].time;
    }
	
	// Update is called once per frame
	void Update () {
        float left = -width / 2;
        float step = width / lineRender.positionCount;
        float timeStep = maxTime / lineRender.positionCount;
        for (int i = 0; i < lineRender.positionCount; i++)
        {
            float x = left + i * step;
            float y = height * (curve.Evaluate(i * timeStep) - 0.5f);
            lineRender.SetPosition(i, new Vector3(x, y, 0));
        }
	}
}
