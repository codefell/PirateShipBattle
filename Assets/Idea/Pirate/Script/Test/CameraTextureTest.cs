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
    private RenderTexture rt;

    private bool init = false;
    private new Camera camera;

	// Use this for initialization
	void Init () {
        rt = new RenderTexture(256, 256 * Screen.height / Screen.width, 24, RenderTextureFormat.ARGB32);
        camera = GetComponent<Camera>();
        camera.targetTexture = rt;
        Debug.Log(string.Format("screen dim {0} {1}", Screen.width, Screen.height));
        tex1 = new Texture2D(rt.width,
            rt.height,
            TextureFormat.RGB24,
            false);
        tex2 = new Texture2D(rt.width,
            rt.height,
            TextureFormat.RGB24,
            false);
        if (image != null)
        {
            sprite = Sprite.Create(tex1, 
                new Rect(0, 0, tex1.width, tex1.height),
                new Vector2(0.5f, 0.5f));
            image.sprite = sprite;
        }
        display1.material.mainTexture = tex1;
        display2.material.mainTexture = tex2;
        init = true;
    }

    void OnDestroy()
    {
        rt.Release();
    }

    private bool grab = false;

    public void Test()
    {
        if (!init)
        {
            Init();
        }
        go1.GetComponent<Renderer>().enabled = true;
        go2.GetComponent<Renderer>().enabled = false;
        currTex = tex1;
        //Debug.Log("render 1 " + currTex);
        GetComponent<Camera>().Render();

        go1.GetComponent<Renderer>().enabled = false;
        go2.GetComponent<Renderer>().enabled = true;
        currTex = tex2;
        //Debug.Log("render 2 " + currTex);
        GetComponent<Camera>().Render();

        go1.GetComponent<Renderer>().enabled = true;
        go2.GetComponent<Renderer>().enabled = true;
    }
	
	// Update is called once per frame
	void OnPostRender() {
        //Debug.Log(currTex);
        currTex.ReadPixels(new Rect(0, 0,
            Screen.width,
            Screen.height), 0, 0, false);
        currTex.Apply();
        grab = false;
    }
}
