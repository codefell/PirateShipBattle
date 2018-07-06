using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Collections;

public class TextureBlending : EditorWindow
{
    int textureSizes = 0;

    int tex1Res = 0;
    int tex2Res = 0;
    Texture2D tex1, tex1Src;
    Texture2D tex2, tex2Src;
    Texture2D resTex;
    Texture2D savedResTex;

    Color grColor = new Color(0.1f, 0.8f, 0.25f);
    Color rdColor = new Color(0.8f, 0.1f, 0.25f);

    //bool txt1Readable = false;
    //bool txt2Readable = false;

    static EditorWindow window;

    [MenuItem("Tools/Vagabond/POLM/Texture Blend", false, 1)]
    public static void ShowWindow()
    {
        window = GetWindow(typeof(TextureBlending));
        window.minSize = new Vector2(490, 270);
        window.maxSize = new Vector2(490, 270);
        window.Show();
    }
    
    void OnEnable()
    {
        
    }

    void OnGUI()
    {
        Rect helpBoxRect = new Rect(10, 10, 470, 38);
        EditorGUI.HelpBox(helpBoxRect, "Drag'n Drop a texture to the first two slots. Click on it to highlight it in the Project." +
                                        "\nOnce you blend the texture it will have red border and green when it is saved."+
                                        "\nCurrently textures will be saved in the \"Assets\" folder.", MessageType.Info);

        if (tex1 != null && tex2 != null)
        {
            if (tex1Res == tex2Res)
                textureSizes = 0;
            else if (tex1Res > tex2Res)
                textureSizes = 1;
            else
                textureSizes = 2;
        }

        float startHeight = 50f;

        Rect dropArea1 = new Rect(10, startHeight, 150.0f, 150.0f);
        DropArea(1, dropArea1);
        if (tex1)
        {
            Rect textRect = dropArea1;
            textRect.x += 1; textRect.y += 1; textRect.width -= 2; textRect.height -= 2;
            EditorGUI.DrawPreviewTexture(textRect, tex1);

            Rect labelRect = dropArea1; labelRect.y += 150f; labelRect.height = 20f;
            GUI.Label(labelRect, tex1 != null ? tex1.name : "Drag'n Drop\n Texture " + dropArea1 + " here");


            Rect resRect = labelRect; resRect.y -= 22;
            EditorGUI.DropShadowLabel(resRect, tex1Res + " x " + tex1Res);
            if (textureSizes != 0)
            {
                resRect = labelRect;
                resRect.y += 20;
                resRect.height += 20f;
                GUI.backgroundColor = rdColor;
                if(GUI.Button(resRect, ""))
                {
                    ResizeTexture(0);
                }
                GUI.backgroundColor = Color.white;
                resRect.y -= 14;
                EditorGUI.DropShadowLabel(resRect, "Resize to " + tex2Res);
            }
        }

        Rect dropArea2 = new Rect(170f, startHeight, 150.0f, 150.0f);
        DropArea(2, dropArea2);
        if (tex2)
        {
            Rect textRect = dropArea2;
            textRect.x += 1; textRect.y += 1; textRect.width -= 2; textRect.height -= 2;
            EditorGUI.DrawPreviewTexture(textRect, tex2);

            Rect labelRect = dropArea2; labelRect.y += 150f; labelRect.height = 20f;
            GUI.Label(labelRect, tex2 != null ? tex2.name : "Drag'n Drop\n Texture " + dropArea2 + " here");


            Rect resRect = labelRect; resRect.y -= 22;
            EditorGUI.DropShadowLabel(resRect, tex2Res + " x " + tex2Res);
            if (textureSizes != 0)
            {
                resRect = labelRect;
                resRect.y += 20;
                resRect.height += 20f;
                GUI.backgroundColor = rdColor;
                if (GUI.Button(resRect, ""))
                {
                    ResizeTexture(1);
                }
                GUI.backgroundColor = Color.white;
                resRect.y -= 14;
                EditorGUI.DropShadowLabel(resRect, "Resize to " + tex1Res);
            }
        }
        
        Rect resArea3 = new Rect(330, startHeight, 150.0f, 150.0f);
        GUI.Box(resArea3, "Result Texture");
        if (resTex)
        {
            Rect textRect = resArea3;
            textRect.x += 1; textRect.y += 1; textRect.width -= 2; textRect.height -= 2;

            if (savedResTex)
            {
                Rect borderRect = textRect;
                borderRect.x -= 2; borderRect.y -= 2;
                borderRect.width += 4; borderRect.height += 4;
                Event evt = Event.current;
                if (evt.type == EventType.MouseDown)
                {
                    if (textRect.Contains(evt.mousePosition))
                    {
                        EditorGUIUtility.PingObject(savedResTex);
                    }
                }
                EditorGUI.DrawRect(borderRect, grColor);
            }
            else
            {
                Rect borderRect = textRect;
                borderRect.x -= 2; borderRect.y -= 2;
                borderRect.width += 4; borderRect.height += 4;
                EditorGUI.DrawRect(borderRect, rdColor);
            }


            if (savedResTex)
                EditorGUI.DrawPreviewTexture(textRect, savedResTex);
            else
                EditorGUI.DrawPreviewTexture(textRect, resTex);
            
            Rect buttonRect = resArea3;
            buttonRect.y += 190f;
            buttonRect.height = 20f;
            if (resTex != null)
            {
                if (GUI.Button(buttonRect, "Save Texture"))
                {
                    SaveTexture();
                }
            }
        }
        if (tex1 != null && tex2 != null && textureSizes == 0)
        {
            Rect buttonRect = resArea3;
            buttonRect.y += 168f;
            buttonRect.height = 20f;
            if (GUI.Button(buttonRect, "Blend Textures"))
            {
                resTex = new Texture2D(tex1Res, tex1Res);
                Color c = Color.white;
                for (int i = 0; i < resTex.width; i++)
                {
                    for (int j = 0; j < resTex.height; j++)
                    {
                        c = tex1.GetPixel(i, j) * tex2.GetPixel(i, j);
                        resTex.SetPixel(i, j, c);
                    }
                }
                resTex.Apply();
            }
        }        
    }

    void CreateTexture(ref Texture2D src, ref Texture2D dst)
    {
        RenderTexture tmp = RenderTexture.GetTemporary( src.width, src.height, 0,
                                                        RenderTextureFormat.Default,
                                                        RenderTextureReadWrite.Linear);
        // Create 1st temp texture
        Graphics.Blit(src, tmp);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = tmp;
        dst = new Texture2D(src.width, src.height);
        dst.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        dst.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);
    }

    void ResizeTexture(int operationID) // o = resize first texture to the second's resolution and 1 means vice versa
    {
        if(operationID == 0)
        {
            TextureScale.Bilinear(tex1, tex2Res, tex2Res);
            tex1Res = tex1.width;            
        }
        else
        {
            TextureScale.Bilinear(tex2, tex1Res, tex1Res);
            tex2Res = tex2.width;
        }
    }

    void SaveTexture()
    {
        var bytes = resTex.EncodeToPNG();
        System.IO.File.WriteAllBytes("Assets/BlendText.png", bytes);
        AssetDatabase.Refresh();

        savedResTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BlendText.png");
    }
    public void DropArea(int textureID, Rect drop_area)
    {
        Event evt = Event.current;

        if (drop_area.Contains(evt.mousePosition))
        {
            Rect borderRect = drop_area;
            borderRect.x -= 1; borderRect.y -= 1;
            borderRect.width += 2; borderRect.height += 2;
            EditorGUI.DrawRect(borderRect, Color.blue);
        }
        
        if (textureID == 1)
        {
            GUI.Box(drop_area, tex1 != null ? tex1.name : "Drag'n Drop\n Texture " + textureID + " here");
        }
        else
        {
            GUI.Box(drop_area, tex2 != null ? tex2.name : "Drag'n Drop\n Texture " + textureID + " here");
        }

        switch (evt.type)
        {
            case EventType.DragUpdated:


            case EventType.DragPerform:

                if (!drop_area.Contains(evt.mousePosition))
                    return;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (Object dragged_object in DragAndDrop.objectReferences)
                    {
                        // Do On Drag Stuff here                        
                        Debug.Log("Texture loaded");
                    }
                }
                if (evt.type == EventType.DragPerform)
                {
                    if (DragAndDrop.objectReferences.Length > 1)
                    {
                        Debug.Log("Texture 1 loaded");
                    }                        
                    else if (DragAndDrop.objectReferences.Length == 1)
                    {
                        Object o = DragAndDrop.objectReferences[0];

                        if(o as Texture2D)
                        {
                            if (textureID == 1)
                            {
                                tex1Src = (o as Texture2D);
                                tex1Res = tex1Src.width;
                                CreateTexture(ref tex1Src, ref tex1);
                                resTex = null;
                                savedResTex = null;
                            }
                            else
                            {
                                tex2Src = (o as Texture2D);
                                tex2Res = tex2Src.width;
                                CreateTexture(ref tex2Src, ref tex2);
                                resTex = null;
                                savedResTex = null;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Drag and drop a texture!");
                    }
                }
                break;
                
            case EventType.MouseDown:

                if (evt.type == EventType.MouseDown)
                {
                    if (drop_area.Contains(evt.mousePosition))
                    {
                        if (textureID == 1)
                            EditorGUIUtility.PingObject(tex1Src);
                        if (textureID == 2)
                            EditorGUIUtility.PingObject(tex2Src);
                    }
                }
                break;

        }

        Repaint();
    }
}

/*
    Texture scale code is taken from the Wiki
    http://wiki.unity3d.com/index.php/TextureScale
*/

public class TextureScale
{
    public class ThreadData
    {
        public int start;
        public int end;
        public ThreadData(int s, int e)
        {
            start = s;
            end = e;
        }
    }

    private static Color[] texColors;
    private static Color[] newColors;
    private static int w;
    private static float ratioX;
    private static float ratioY;
    private static int w2;
    private static int finishCount;
    private static Mutex mutex;

    public static void Point(Texture2D tex, int newWidth, int newHeight)
    {
        ThreadedScale(tex, newWidth, newHeight, false);
    }

    public static void Bilinear(Texture2D tex, int newWidth, int newHeight)
    {
        ThreadedScale(tex, newWidth, newHeight, true);
    }

    private static void ThreadedScale(Texture2D tex, int newWidth, int newHeight, bool useBilinear)
    {
        texColors = tex.GetPixels();
        newColors = new Color[newWidth * newHeight];
        if (useBilinear)
        {
            ratioX = 1.0f / ((float)newWidth / (tex.width - 1));
            ratioY = 1.0f / ((float)newHeight / (tex.height - 1));
        }
        else
        {
            ratioX = ((float)tex.width) / newWidth;
            ratioY = ((float)tex.height) / newHeight;
        }
        w = tex.width;
        w2 = newWidth;
        var cores = Mathf.Min(SystemInfo.processorCount, newHeight);
        var slice = newHeight / cores;

        finishCount = 0;
        if (mutex == null)
        {
            mutex = new Mutex(false);
        }
        if (cores > 1)
        {
            int i = 0;
            ThreadData threadData;
            for (i = 0; i < cores - 1; i++)
            {
                threadData = new ThreadData(slice * i, slice * (i + 1));
                ParameterizedThreadStart ts = useBilinear ? new ParameterizedThreadStart(BilinearScale) : new ParameterizedThreadStart(PointScale);
                Thread thread = new Thread(ts);
                thread.Start(threadData);
            }
            threadData = new ThreadData(slice * i, newHeight);
            if (useBilinear)
            {
                BilinearScale(threadData);
            }
            else
            {
                PointScale(threadData);
            }
            while (finishCount < cores)
            {
                Thread.Sleep(1);
            }
        }
        else
        {
            ThreadData threadData = new ThreadData(0, newHeight);
            if (useBilinear)
            {
                BilinearScale(threadData);
            }
            else
            {
                PointScale(threadData);
            }
        }

        tex.Resize(newWidth, newHeight);
        tex.SetPixels(newColors);
        tex.Apply();

        texColors = null;
        newColors = null;
    }

    public static void BilinearScale(System.Object obj)
    {
        ThreadData threadData = (ThreadData)obj;
        for (var y = threadData.start; y < threadData.end; y++)
        {
            int yFloor = (int)Mathf.Floor(y * ratioY);
            var y1 = yFloor * w;
            var y2 = (yFloor + 1) * w;
            var yw = y * w2;

            for (var x = 0; x < w2; x++)
            {
                int xFloor = (int)Mathf.Floor(x * ratioX);
                var xLerp = x * ratioX - xFloor;
                newColors[yw + x] = ColorLerpUnclamped(ColorLerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp),
                                                       ColorLerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp),
                                                       y * ratioY - yFloor);
            }
        }

        mutex.WaitOne();
        finishCount++;
        mutex.ReleaseMutex();
    }

    public static void PointScale(System.Object obj)
    {
        ThreadData threadData = (ThreadData)obj;
        for (var y = threadData.start; y < threadData.end; y++)
        {
            var thisY = (int)(ratioY * y) * w;
            var yw = y * w2;
            for (var x = 0; x < w2; x++)
            {
                newColors[yw + x] = texColors[(int)(thisY + ratioX * x)];
            }
        }

        mutex.WaitOne();
        finishCount++;
        mutex.ReleaseMutex();
    }

    private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
    {
        return new Color(c1.r + (c2.r - c1.r) * value,
                          c1.g + (c2.g - c1.g) * value,
                          c1.b + (c2.b - c1.b) * value,
                          c1.a + (c2.a - c1.a) * value);
    }
}
