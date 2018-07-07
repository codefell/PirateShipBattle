using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraTextureTest : MonoBehaviour {
    public Renderer display1;
    public Renderer display2;
    private Texture2D tex1;
    private Texture2D tex2;
    public GameObject go1;
    public GameObject go2;
    public Image image;
    private Sprite sprite;

    public Texture2D currTex;

    private bool init = false;
    private new Camera camera;

    void Start()
    {
        camera = GetComponent<Camera>();
        display1.material.mainTexture = camera.targetTexture;
        display2.material.mainTexture = camera.targetTexture;
    }

    public void Test()
    {
        camera.Render();
    }
}
