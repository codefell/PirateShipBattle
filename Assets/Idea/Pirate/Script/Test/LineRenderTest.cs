using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRenderTest : MonoBehaviour {
    public Camera refCamera;
    private LineRenderer lineRenderer;
    public int segNum = 100;
    public float a = 1;
    public float b = 1;
    public float c = 1;
    public float d = 1;
    private float height = 0;
    private float width = 0;
    public float timeHistory = 10;
    private float lastFlushTime = 0;
    private float flushInterval = 0;
    private float lastSignal = 0;

    private void HistroyMove()
    {
        int i = 0;
        for (i = 0; i < lineRenderer.positionCount - 1; i++)
        {
            Vector3 pos0 = lineRenderer.GetPosition(i);
            Vector3 pos1 = lineRenderer.GetPosition(i + 1);
            pos0.y = pos1.y;
            lineRenderer.SetPosition(i, pos0);
        }
    }

    public void SetSingal(float singal)
    {
        lastSignal = singal;
    }

	// Use this for initialization
	void Start () {
        lineRenderer = GetComponent<LineRenderer>();
        height = 2 * refCamera.orthographicSize;
        width = height * refCamera.aspect;
        lineRenderer.positionCount = segNum;
        for (int i = 0; i < segNum; i++)
        {
            float x = (i - segNum / 2) * width / (segNum - 1);
            lineRenderer.SetPosition(i, new Vector3(x, 0));
        }
        flushInterval = timeHistory / (lineRenderer.positionCount - 1);
        lastFlushTime = Time.time;
	}
	
	// Update is called once per frame
	void Update () {
        if (Time.time - lastFlushTime >= flushInterval)
        {
            HistroyMove();
            int i = lineRenderer.positionCount - 1;
            Vector3 pos = lineRenderer.GetPosition(i);
            pos.y = lastSignal;
            lineRenderer.SetPosition(i, pos);
            lastSignal = 0;
            lastFlushTime = Time.time;
        }
        /*
        float deltaTime = Time.time - startTime;
        for (int i = 0; i < segNum; i++)
        {
            lineRenderer.SetPosition(i,
                new Vector3(i * a, c * Mathf.Sin((d * i + deltaTime) * b)));
        }
        */
	}
}
