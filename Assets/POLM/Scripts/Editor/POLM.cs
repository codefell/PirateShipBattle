////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                                                                            //
//                                                      POLM - Per Object Light Mapper v0.92.4b                                               //
//                                                                                                                                            //
// The current version contains GPU Ambient Occlusion per object mapping. Currently runs on Windows under DX11 and requires a DX11 video card //
// OpenGL is NOW supported, which means MAC OS X editor is not supported - support for OpenGL compute shaders will come in later              //
// NOTE: MAC OS X does not yet supports OpenGL 4.3 which is the lowest version of OpenGL which supports Compute Shaders                       //
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

/// <summary>
/// 
/// </summary>
public static class POLMColors
{
    public static Color lightRed = new Color(0.9f, 0.453f, 0.382f);
    public static Color lightGreen = new Color(0.285f, 0.691f, 0.61f);
    public static Color lightBlue = new Color(0.222f, 0.48f, 0.722f);
    public static Color darkBlue = new Color(0.207f, 0.277f, 0.363f);
    public static Color lightGrey = new Color(0.58f, 0.625f, 0.665f);

    public static Color gr1 = new Color(0.580f, 0.625f, 0.665f); // 162 184 108
    public static Color gr2 = new Color(0.632f, 0.720f, 0.420f); // 92 167 147
    public static Color bl1 = new Color(0.074f, 0.582f, 0.726f); // 19 149 186
    public static Color bl2 = new Color(0.066f, 0.468f, 0.597f); // 17 120 153
    public static Color bl3 = new Color(0.058f, 0.355f, 0.468f); // 15 91 120
    public static Color bl4 = new Color(0.050f, 0.234f, 0.332f); // 13 60 85
    public static Color ye1 = new Color(0.917f, 0.781f, 0.265f); // 235 200 68
    public static Color or1 = new Color(0.917f, 0.664f, 0.218f); // 236 170 56☺
    public static Color or2 = new Color(0.933f, 0.542f, 0.172f); // 239 139 44
    public static Color or3 = new Color(0.550f, 0.421f, 0.125f); // 241 108 32
    public static Color or4 = new Color(0.847f, 0.304f, 0.121f); // 217 78 31
    public static Color rd1 = new Color(0.750f, 0.179f, 0.113f); // 192 46 29
}

/// <summary>
/// POLM WINDOW HELP POPUPS
/// </summary>
public class Popups : PopupWindowContent
{
    public override Vector2 GetWindowSize()
    {
        return new Vector2(220, 25);
    }

    public override void OnGUI(Rect rect)
    {
        EditorGUILayout.HelpBox("Does the option require precomputing!", MessageType.None);
    }
}

[System.Serializable]
public class ObjectSelected
{
    public ObjectBakeState bakeState;
    public float  bakePercentage = 0f;  // "The object"
    public Object obj;                  // "The object"
    public Renderer rend;               // The object's "Renderer" componet
    public Mesh oMesh;                  // A reference to the original mesh fro the assets
    public Mesh bMesh;                  // A new mesh in which to be stored the baked information
    public Mesh uvMesh;                 // A new mesh representing the object's current shosen uv layout
    public Material[] mats;             // All the materials the object is using

    public Vector3[] vertices;
    public Vector3[] normals;
    public int[]     triangles;
    public Vector2[] uvs;

    // Used to validate the mesh
    public bool isValid = true;

    public bool MultiMat
    {
        get { return mats.Length > 0; }
    }

    // UV stuff
    public POLM.UVSet uvSet;                // The uv set to be used for baking
    public POLM.Triangle2D[] triangles2D;   // The 2D flattened triangles representing the uvs    
    public List<POLM.Lumel> lumels;         // The object's lumels list
    public string meshName;                 // the name of the object
    public bool bakeMesh = true;            // Is on - th mesh will be baked, otherwise will act as a occluder only
    public bool autoSaveMesh;               // Should be baked mesh auto saved to the Assets
    public bool autoApplyMesh;              // should the mesh be auto applied to the object's renderer (ir mesh filter component)
    public int hash;                        // The object's hash - used with... (check wat it actually does)
    public bool isBaked;                    // Is the object baked
    public RenderTexture rendTex;           // Used for texture baking
    public POLMObject polmObj;              // Reference to a POLMObject script    
}

public enum ObjectBakeState { precomputing, baking }
public enum ShaderSlot { aoSlot, detailSlot }

[System.Serializable, ExecuteInEditMode]
public class POLM : EditorWindow
{
    // ----------------------------------------------------------------------------------------------------
    // VARIABLES
    // ----------------------------------------------------------------------------------------------------
    static POLMData polmData;

    //string currentProcess = "Start Baking";
    
    CurrentPlatform platform;

    bool interpolateNormals = false;
    bool dilateTexture = true;
    bool blurTexture = true;
    bool bSaveMaps = true;
    [Range(1, 10)] int marginSamples = 10;
    [Range(1, 5)]  int blurSamples = 1;
    [Tooltip("0 == 0% black. 1 == 100 % black. Use to darken the AO map while saving." +
        " This feature is usefull in some specific cases. Usually you don't want to set it greated than 0.")]
    float darkenMultiplier = 0f;

    string currentJobLabel = "START BAKING";

    int shaderSlot = 0; // 1 is AO
    string[] selStrings = new string[3] { "NONE", "AO", "Detail Slot" };
    string[] selGridUV  = new string[4] { "uv", "uv2", "uv3", "uv4" };
    bool loadTextureToShaderSlot;

    Texture2D randVectors1D;
    Texture2D randVectors2D;

    [Header("Lightmapping! --------------------------------------------------------------")]
    ComputeShader shader;

    int     textureSize         = 512;
    int     samples             = 128;
    float   radius              = 1.0f;
    float   cleanUpValue        = 0.01f;
    float   AOPower             = 0.5f;

    float   currentProgress     = 0f;
    bool    isWorking           = false;

    Texture2D banner, docsIcon;
    List<ObjectSelected> objectsSelected = new List<ObjectSelected>();

    // ----------------------------------------------------------------------------------------------------------------------------------
    // Selection grids
    int         textureSizeID   = 2;
    string[]    textureSizesStr = new string[] { "128", "256", "512", "1K", "2K" };
    int[]       textureSizesPxl = new int[] { 128, 256, 512, 1024, 2048 };
    int         samplesID       = 2;
    string[]    sampleCountStr  = new string[] { "32", "64", "128", "256", "512" };
    int[]       sampleCount     = new int[] { 32, 64, 128, 256, 512 };
    int         raySamplesID    = 0;
    //string[]    raySamplesType  = new string[] { "v1 recommended", "v2", "v3" };

    // ----------------------------------------------------------------------------------------------------------------------------------

    int          bakingMode               = 0;
    int          currentDevice            = 0;
    bool         supportsComputeShaders   = true;
    string       cpuInfo = "", gpuInfo    = "";
    Vector2      pvMeshBakeListScrollView = new Vector2();
    List<Thread> threads;

    static int ThreadGroupSize = 8;

    // ----------------------------------------------------------------------------------------------------------------------------------
    // Saving assets paths
    
    [MenuItem("Tools/Vagabond/POLM/Open POLM Tool %e", false, 1)]
    public static void ShowWindow()
    {
        //EditorWindow newWindow = GetWindowWithRect(typeof(POLM), new Rect(0, 0, 650, 615));
        EditorWindow newWindow = GetWindow(typeof(POLM));
        newWindow.Show();
    }

    [MenuItem("Tools/Vagabond/POLM/Documentation", false, 2)]
    public static void OpenDocs()
    {
        string[] pats = Directory.GetFiles("Assets", "POLM_Documentation.pdf", SearchOption.AllDirectories);

        if (pats.Length > 0)
             Application.OpenURL(pats[0]);
        else Debug.Log("Documentation is missing or it's renamed! The doc name should be - POLM_Documentation.html");
    }

    // ==================================================================================================================
    // Manage saving directories

    void LoadTextureSavingPath()
    {
        if (!Directory.Exists(Application.dataPath + polmData.tDir))
        {
            polmData.tDir = "";
            Debug.LogError("Texture Saving Directory does not exist. Revert to /Assets folder");
        }
    }

    void ChooseTextureSavingPath()
    {
        string path = EditorUtility.OpenFolderPanel("Choose Save Location", "", "");

        if (path != "")
        {
            if (!path.Contains(Application.dataPath))
            {
                polmData.tDir= "";
                Debug.LogError("The Save Directory should be in the project. Changed to \"/Assets\"");
            }
            else polmData.tDir = path.Replace(Application.dataPath, "");
        }
    }

    void LoadMeshSavingPath()
    {
        if (!Directory.Exists(Application.dataPath + polmData.mDir))
        {
            polmData.mDir = "";
            Debug.LogError("Meshes Saving Directory does not exist. Revert to /Assets folder");
        }
    }

    void ChooseMeshSavingPath()
    {
        string path = EditorUtility.OpenFolderPanel("Choose Save Location", "", "");

        if (path != "")
        {
            if (!path.Contains(Application.dataPath))
            {
                polmData.mDir = "";
                Debug.LogError("The Save Directory should be in the project. Changed to \"/Assets\"");
            }
            else polmData.mDir = path.Replace(Application.dataPath, "");
        }
    }

    // ==================================================================================================================
    // Load last used bake mode and device

    void LoadBakeMode()
    {
        if (!EditorPrefs.HasKey("bakMode"))
        {
            bakingMode = 0;
            EditorPrefs.SetInt("bakMode", 0);
        }
        else
        {
            bakingMode = EditorPrefs.GetInt("bakMode");
        }
    }

    void SetBakeMode(int _bakeMode)
    {
        EditorPrefs.SetInt("bakMode", _bakeMode);
        bakingMode = _bakeMode;
        this.Close();
        ShowWindow();
        if (_bakeMode == 0) Debug.Log("RELOAD TO TEXTURE BAKING");
        else if (_bakeMode == 1) Debug.Log("RELOAD TO PER VERTEX BAKING");
    }

    void LoadDevice()
    {
        if (!EditorPrefs.HasKey("bakeDevice"))
        {
            currentDevice = 0;
            EditorPrefs.SetInt("bakeDevice", 0);
        }
        else
        {
            currentDevice = EditorPrefs.GetInt("bakeDevice");
        }
    }

    void SetDevice(int _device)
    {
        EditorPrefs.SetInt("bakeDevice", _device);
        currentDevice = _device;
    }

    // ==================================================================================================================
    /// <summary>
    /// 
    /// </summary>
    void OnEnable()
    {
        polmData = Resources.Load("POLMData")   as POLMData;
        banner   = Resources.Load("POLMBanner") as Texture2D;
        docsIcon = Resources.Load("DocsIcon")   as Texture2D;

        LoadTextureSavingPath();
        LoadMeshSavingPath();
        LoadBakeMode();
        LoadDevice();

        //Debug.Log(SystemInfo.graphicsDeviceVersion.Contains("GL") + " ||| " + SystemInfo.supportsComputeShaders);

        cpuInfo = SystemInfo.processorType;
        gpuInfo = SystemInfo.graphicsDeviceName;
        supportsComputeShaders = SystemInfo.supportsComputeShaders;
        
        if (SystemInfo.graphicsDeviceVersion.Contains("Direct") && SystemInfo.supportsComputeShaders)
        {
            platform = CurrentPlatform.DX;
        }
        else if (SystemInfo.graphicsDeviceVersion.Contains("GLES") && SystemInfo.supportsComputeShaders)
        {
            platform = CurrentPlatform.GLES;
        }
        else if (SystemInfo.graphicsDeviceVersion.Contains("GL") && SystemInfo.supportsComputeShaders)
        {
            platform = CurrentPlatform.GL;
        }
        else platform = CurrentPlatform.NONE;

        // Deprecated !!!
        //if (platform == CurrentPlatform.GL || platform == CurrentPlatform.GLES)
        //    shader = Resources.Load("POLMCSGL") as ComputeShader;
        //else
        //    shader = Resources.Load("POLMCSDX") as ComputeShader;

        // Now only one version of copute shader is used
        shader = Resources.Load("POLMCSDX") as ComputeShader;
        
        // Hardware and platform data
        //Debug.Log(SystemInfo.graphicsDeviceVersion + " | " + platform + " | " + shader.name);

        // Loading a random samples texture
        randVectors1D = Resources.Load("RandomVectors1D") as Texture2D;
        randVectors2D = Resources.Load("RandomVectors2D") as Texture2D;

        // Scene Gizmos Drawing delegate
        SceneView.onSceneGUIDelegate += this.OnSceneGUI;
    }

    /// <summary>
    /// 
    /// </summary>
    void OnDisable()
    {
        if (isWorking)
        {
            CancelWork();
            Debug.LogWarning("Work canceled: be sure to manually cancel baking before closing the tool");
        }

        // Scene Gizmos Drawing delegate
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
    }

    void UpdateSelection()
    {

    }

    void SelectPOLMObjects()
    {

    }

    bool toCleanUpObjectList = false;

    /// <summary>
    /// Drawing GUI stuff
    /// </summary>
    void OnGUI()
    {
        if (bakingMode == 1)
        {
            if (toCleanUpObjectList)
            {
                CleanUpObjectList();
                toCleanUpObjectList = false;
                return;
            }
        }

        Repaint();

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // GROUP 1 - LABEL/BANNER PLUS Baking Mode Buttons

        EditorGUILayout.BeginVertical("Box"); // BEGIN VERTICAL GORUP 1

        GUI.backgroundColor = Color.white;
        GUI.DrawTexture(GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, 69.6f), banner);

        float editorWidth = Screen.width;

        if (GUI.Button(new Rect(editorWidth - 55, 10, 40, 40), docsIcon))
        {
            OpenDocs();
        }

        // Try to use if needed one of the methods below to get the window half width
        // EditorGUIUtility.currentViewWidth || EditorGUIUtility.fieldWidth
        float buttonWidth = Screen.width / 2 - 10;

        // COMPUTE DEVICE BUTTONS
        EditorGUILayout.BeginHorizontal();

        if (currentDevice == 0) GUI.color = POLMColors.ye1;
        else                    GUI.color = Color.white;

        if (supportsComputeShaders)
        {
            if (GUILayout.Button("GPU: " + (supportsComputeShaders ? "Compute Shaders Supported\n" : "Compute Shaders Not Supported\n") + gpuInfo, new GUILayoutOption[] { GUILayout.Width(buttonWidth), GUILayout.Height(35f) }))
            {
                //currentDevice = 0;
                SetDevice(0);
            }
        }
        else
        {
            GUI.color = POLMColors.lightRed;

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Compute Shaders not supported \neither by the GPU or Platform", new GUILayoutOption[] { GUILayout.Width(buttonWidth - 8), GUILayout.Height(28f) });
            EditorGUILayout.EndVertical();
        }

        if (currentDevice == 1) GUI.color = POLMColors.ye1;
        else                    GUI.color = Color.white;

        if (bakingMode == 0)
        {
            GUI.color = POLMColors.lightRed;
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Baking Texture on CPU not supported!", new GUILayoutOption[] { GUILayout.Width(buttonWidth), GUILayout.Height(27f) });
            EditorGUILayout.EndVertical();
            GUI.color = Color.white;
        }
        else
        {
            if (GUILayout.Button("CPU:\n" + cpuInfo, new GUILayoutOption[] { GUILayout.Width(buttonWidth), GUILayout.Height(35f) }))
            {
                // Set device id
                SetDevice(1);
            }
        }

        EditorGUILayout.EndHorizontal();
        
        // BAKE MODE BUTTONS
        EditorGUILayout.BeginHorizontal();

             if (bakingMode == 0) GUI.color = POLMColors.ye1;
        else if (bakingMode == 1) GUI.color = Color.white;

        if (GUILayout.Button("Texture Baking", new GUILayoutOption[] { GUILayout.Width(buttonWidth), GUILayout.Height(50f) }))
        {
            // Force currend device to GPU as CPI Texture baking is not yet supported
            SetDevice(0);

            // Set bake mode
            SetBakeMode(0);
        }

        if (bakingMode == 1) GUI.color = POLMColors.ye1;
        else if (bakingMode == 0) GUI.color = Color.white;

        if (GUILayout.Button("Per Vertex Baking", new GUILayoutOption[] { GUILayout.Width(buttonWidth), GUILayout.Height(50f) }))
        {
            //bakingMode = 1;
            SetBakeMode(1);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        // END GROUP 1 - LABEL/BANNER PLUS Baking Mode Buttons
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        GUI.color = Color.white;

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // GROUP 2 - All Options Texture Baking
        if (bakingMode == 0)
        {
            if (shader != null)
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // BEGIN SUBGROUP - CHOOSE SAVING FOLDER
                GUI.backgroundColor = POLMColors.lightBlue;
                GUILayout.BeginHorizontal("box");
                GUI.backgroundColor = Color.white;
                bSaveMaps = EditorGUILayout.Toggle("Save Texture", bSaveMaps, new GUILayoutOption[] { GUILayout.MaxWidth(180f) });
                if (GUILayout.Button("Choose Folder: " + (polmData.tDir== "" ? "/Assets" : ("/Assets" + polmData.tDir+ "/"))))
                {
                    ChooseTextureSavingPath();
                }
                GUILayout.EndHorizontal();                                
                // END SUBGROUP - CHOOSE SAVING FOLDER
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // BEGIN SUBGROUP 1 - Main Options

                GUI.backgroundColor = POLMColors.lightRed;

                GUILayout.BeginVertical("Box");                 // --------------- BEGIN VERTICAL MAIN OPTIONS

                GUI.backgroundColor = Color.white;
                
                //GUILayout.BeginHorizontal();
                //interpolateNormals = EditorGUILayout.Toggle("Interpolate Normals", interpolateNormals);
                ////GUILayout.Label("YES", EditorStyles.boldLabel, new GUILayoutOption[] { GUILayout.Width(32f) });
                //GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Texture Size", EditorStyles.boldLabel, new GUILayoutOption[] { GUILayout.MaxWidth(145f) });
                textureSizeID = GUILayout.SelectionGrid(textureSizeID, textureSizesStr, textureSizesStr.Length);
                // Does it require precompute
                //GUILayout.Label("YES", EditorStyles.boldLabel, new GUILayoutOption[] { GUILayout.Width(32f) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("AO Samples", EditorStyles.boldLabel, new GUILayoutOption[] { GUILayout.MaxWidth(145f) });
                samplesID = GUILayout.SelectionGrid(samplesID, sampleCountStr, sampleCountStr.Length);
                // Does it require precompute
                //GUILayout.Label("NO", EditorStyles.boldLabel, new GUILayoutOption[] { GUILayout.Width(32f) });
                GUILayout.EndHorizontal();

                // TODO : revert when ray sample using hashes and cosine direction is fixed
                //GUILayout.BeginHorizontal();
                //GUILayout.Label("Samples Generation", new GUILayoutOption[] { GUILayout.MaxWidth(145f) });
                //raySamplesID = GUILayout.SelectionGrid(raySamplesID, raySamplesType, raySamplesType.Length);
                ////GUILayout.Label("NO", EditorStyles.boldLabel, new GUILayoutOption[] { GUILayout.Width(32f) });
                //GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                radius = EditorGUILayout.Slider("Radius", radius, 0.01f, 2f);
                // Does it require precompute
                //GUILayout.Label("NO", EditorStyles.boldLabel, new GUILayoutOption[] { GUILayout.Width(32f) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                cleanUpValue = EditorGUILayout.Slider("Clean Up Value", cleanUpValue, 0.0f, 1f);
                // Does it require precompute
                //GUILayout.Label("NO", EditorStyles.boldLabel, new GUILayoutOption[] { GUILayout.Width(32f) });
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                AOPower = EditorGUILayout.Slider("AO Power", AOPower, 0.01f, 1f);
                // Does it require precompute
                //GUILayout.Label("NO", EditorStyles.boldLabel, new GUILayoutOption[] { GUILayout.Width(32f) });
                GUILayout.EndHorizontal();

                // Color field to choose a texture BG color
                polmData.textureBGColor = EditorGUILayout.ColorField("Texture BG Color", polmData.textureBGColor);

                GUILayout.EndVertical();                        // --------------- END VERTICAL MAIN OPTIONS
                // END SUBGROUP 1 - Main Options
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // BEGIN SUBGROUP 2 - Post Process Options
                GUI.backgroundColor = POLMColors.lightBlue;

                EditorGUILayout.BeginVertical("Box");           // --------------- BEGIN VERTICAL POST PROCESS

                GUI.backgroundColor = Color.white;
                GUILayout.Label(" Post Process ", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();
                dilateTexture = EditorGUILayout.Toggle("Dilate textures", dilateTexture, new GUILayoutOption[] { GUILayout.MaxWidth(180f) });
                if (dilateTexture)
                {
                    marginSamples = (int)EditorGUILayout.Slider(marginSamples, 1, 10);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                blurTexture = EditorGUILayout.Toggle("Blur texture", blurTexture, new GUILayoutOption[] { GUILayout.MaxWidth(180f) });
                if (blurTexture)
                {
                    blurSamples = (int)EditorGUILayout.Slider(blurSamples, 1, 5);
                }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                darkenMultiplier = EditorGUILayout.Slider("Darken baked texture", darkenMultiplier, 0.0f, 1f);
                // Does it require precompute
                //GUILayout.Label("NO", EditorStyles.boldLabel, new GUILayoutOption[] { GUILayout.Width(32f) });
                GUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();                   // --------------- END VERTICAL POST PROCESS

                GUILayout.Space(10f);

                // END SUBGROUP 2 - Post Process Options
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // BEGIN SUBGROUP 3 - Start Baking
                GUI.backgroundColor = POLMColors.or1;

                EditorGUILayout.BeginVertical("Box");       // ----- BEGIN VERTICAL

                GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginHorizontal();          // ----- BEGIN HORIZONTAL

                if (objectsSelected.Count > 0)
                {
                    if (GUILayout.Button(!isWorking ? "Bake AO" : "Cancel", new GUILayoutOption[] { GUILayout.MaxWidth(140f), GUILayout.Height(25f) }))
                    {
                        if (!isWorking)
                        {
                            CleanUp();

                            EditorCoroutine.start(BakeAO());                            
                        }
                        else
                        {
                            CancelWork();
                        }
                    }
                }
                else
                {
                    GUI.backgroundColor = POLMColors.lightGreen;
                    EditorGUILayout.BeginVertical("box");
                    GUILayout.Label("Select Some Objects", new GUILayoutOption[] { GUILayout.MaxWidth(140f), GUILayout.Height(18f) });
                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = Color.white;
                }


                // Draw Progress Bar
                GUILayout.Label("", EditorStyles.boldLabel);
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.width += rect.x;
                rect.x -= rect.x;
                rect.y -= 3f;

                //currentJobLabel = isWorking ? "Bake in progress" : currentProcess;

                rect.x += 160f;
                rect.width -= 160f;
                rect.height += rect.height * 0.55f;
                EditorGUI.ProgressBar(rect, currentProgress, currentJobLabel);
                EditorGUILayout.EndHorizontal();        // ----- END HORIZONTAL
                EditorGUILayout.EndVertical();          // ----- END VERTICAL

                GUILayout.Space(5f);

                // Auto load texture to slot toggle group
                GUILayout.BeginVertical("Box");
                EditorGUILayout.HelpBox("Automatically add the baked texture to one of the Standard shader slots, if the renderer's material is using it. " +
                                        "If loading the texture to the AO slot, the main color should be brighter. If you load the texture to the Detailed slot, the main color should be darker, and the uv channel will be automatically set to UV or UV2 automatically based on the chosen set - per object in the list!", MessageType.Info);
                EditorGUILayout.LabelField("Auto load texture to Standard shader slot:");
                shaderSlot = GUILayout.SelectionGrid(shaderSlot, selStrings, 3);
                GUILayout.EndVertical();

                // ====================================================================================================
                // Information
                // EditorGUILayout.HelpBox(info, MessageType.Info, true);
                // ====================================================================================================

                // END SUBGROUP 3 - Start Baking
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            }
        }
        else // PER VERTEX OPTIONS
        {
            if (shader != null)
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // BEGIN SUBGROUP - CHOOSE SAVING FOLDER
                GUI.backgroundColor = POLMColors.lightBlue;
                GUILayout.BeginHorizontal("box");
                GUI.backgroundColor = Color.white;
                bSaveMaps = EditorGUILayout.Toggle("Save Mesh", bSaveMaps, new GUILayoutOption[] { GUILayout.MaxWidth(180f) });
                if (GUILayout.Button("Choose Folder: " + (polmData.mDir == "" ? "/Assets" : ("/Assets" + polmData.mDir + "/"))))
                {
                    ChooseMeshSavingPath();
                }
                GUILayout.EndHorizontal();
                // END SUBGROUP - CHOOSE SAVING FOLDER
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // SubGroup 1 - Main Options
                GUI.backgroundColor = POLMColors.lightRed;

                EditorGUILayout.BeginVertical("Box");    // -----  BEGIN SETTINGS VERTICAL GROUP
                GUI.backgroundColor = Color.white;

                GUILayout.BeginHorizontal();
                GUILayout.Label("AO Samples", new GUILayoutOption[] { GUILayout.MaxWidth(145f) });
                samplesID = GUILayout.SelectionGrid(samplesID, sampleCountStr, sampleCountStr.Length);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                radius = EditorGUILayout.Slider("Radius", radius, 0.01f, 2f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                cleanUpValue = EditorGUILayout.Slider("Clean Up Value", cleanUpValue, 0.0f, 1f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                AOPower = EditorGUILayout.Slider("AO Power", AOPower, 0.01f, 1f);
                GUILayout.EndHorizontal();

                //// Choose a saving folder
                //GUILayout.BeginHorizontal();
                //bSaveMaps = EditorGUILayout.Toggle("Save Mesh", bSaveMaps, new GUILayoutOption[] { GUILayout.MaxWidth(180f) });
                //if (GUILayout.Button("Choose Folder: " + saveVCMeshesPathVis == "" ? "Assets" : saveVCMeshesPathVis))
                //{
                //    ChooseMeshSavingPath();
                //}
                //GUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();          // ----- END SETTINGS VERTICAL GROUP

                GUILayout.Space(5f);


                //////////////////////////////////////////////////
                // Begin Bake AO Button/ProgressBar group
                GUI.backgroundColor = POLMColors.or1;
                EditorGUILayout.BeginVertical("Box");       // ----- BEGIN VERTICAL
                GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginHorizontal();
                if (objectsSelected.Count > 0)
                {                    
                    if (GUILayout.Button(!isWorking ? "Bake Per Vertex AO" : "Cancel",
                                            new GUILayoutOption[] { GUILayout.MaxWidth(140f), GUILayout.Height(25f) }))
                    {
                        if (!isWorking)
                        {
                            CleanUp();

                            if (currentDevice == 0)
                                EditorCoroutine.start(BakeAOPV());
                            else
                                EditorCoroutine.start(BakeAOPV_CPU());
                        }
                        else
                        {
                            CancelWork();
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUI.backgroundColor = POLMColors.lightGreen;
                    EditorGUILayout.BeginVertical("box");
                    GUILayout.Label("Select Some Objects", new GUILayoutOption[] { GUILayout.MaxWidth(140f), GUILayout.Height(18f) });
                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = Color.white;
                }

                // Draw Progress Bar
                GUILayout.Label("", EditorStyles.boldLabel);
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.width += rect.x;
                rect.x -= rect.x;
                rect.y -= 3f;

                string label;
                if (currentProgress == 0f) label = "Start Baking";
                else if (currentProgress > 0f && currentProgress < 1f) label = "Baking in progress...";
                else label = "Bake Done!";

                rect.x += 160f;
                rect.width -= 160f;
                rect.height += rect.height * 0.55f;
                EditorGUI.ProgressBar(rect, currentProgress, label);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                // End Bake AO Button/ProgressBar group
                //////////////////////////////////////////////////
            }
        }
        // END GROUP 2 - All Options Texture Baking
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // Make some space
        GUILayout.Space(5f);

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // BEGIN GROUP 3 - SELECTED OBJECTS
        // UPDATE SELECTION - COMMON FOR BOTH BAKING METODS
        
        GUILayout.Space(5f);

        EditorGUILayout.BeginHorizontal();   // BEGIN HORIZONTAL BUTTONS GROUP
        if (GUILayout.Button("Update Selection"))
        {
            List<Renderer> allRends = new List<Renderer>();
            allRends = new List<Renderer>();
            GameObject[] gos = Selection.gameObjects;
            foreach (GameObject g in gos)
            {
                allRends.AddRange(g.GetComponentsInChildren<Renderer>());
            }

            objectsSelected.Clear();
            foreach (Renderer r in allRends)
            {
                ObjectSelected so = new ObjectSelected();
                so.obj  = r;
                so.rend = r;

                // Load Original Mesh
                if (bakingMode == 1)
                {
                    if (!so.rend.gameObject.GetComponent<POLMObject>())
                    {
                        so.polmObj = so.rend.gameObject.AddComponent<POLMObject>();
                        so.polmObj.Init();
                    }
                    else
                    {
                        so.polmObj = so.rend.gameObject.GetComponent<POLMObject>();
                        so.polmObj.Init();
                    }
                    so.oMesh = so.polmObj.o_Mesh;
                }
                else
                {
                    if (so.rend as MeshRenderer)
                        so.oMesh = so.rend.transform.GetComponent<MeshFilter>().sharedMesh;
                    else if (so.rend as SkinnedMeshRenderer)
                        so.oMesh = (so.rend as SkinnedMeshRenderer).sharedMesh;
                }

                // Load Baked Mesh
                if (so.rend as MeshRenderer)
                {
                    if (so.rend.GetComponent<MeshFilter>().sharedMesh)
                        so.bMesh = (Mesh)Instantiate(so.rend.GetComponent<MeshFilter>().sharedMesh);
                    else { Debug.Log("The renderer has no reference to a mesh and will be missed"); so.isValid = false; continue; }
                }
                else if (so.rend as SkinnedMeshRenderer)
                {
                    if ((so.rend as SkinnedMeshRenderer).sharedMesh)
                        so.bMesh = (Mesh)Instantiate((so.rend as SkinnedMeshRenderer).sharedMesh);
                    else { Debug.Log("The renderer has no reference to a mesh and will be missed"); so.isValid = false; continue; }
                }
                so.bMesh.name = (so.bMesh.name.Remove(so.bMesh.name.Length - 7)) + "_Baked";
                so.bMesh.hideFlags = HideFlags.HideAndDontSave;

                so.vertices         = so.oMesh.vertices;
                so.normals          = so.oMesh.normals;
                if(so.normals.Length != so.vertices.Length)
                {
                    so.isValid = false;
                }

                so.triangles        = so.oMesh.triangles;
                
                so.mats             = r.sharedMaterials;
                so.meshName         = r.name + "_AO";
                so.autoSaveMesh     = true;
                so.autoApplyMesh    = true;
                so.hash             = r.GetHashCode();
                
                objectsSelected.Add(so);
            }
        }
        if (bakingMode == 1) // Show this button only if baking to vertex color
        {
            if (GUILayout.Button("SELECT POLM OBJECTS"))
            {
                POLMObject[] allPolmObjects = GameObject.FindObjectsOfType<POLMObject>() as POLMObject[];
                Selection.objects = allPolmObjects as Object[];

                objectsSelected.Clear();
                foreach (POLMObject o in allPolmObjects)
                {
                    ObjectSelected so = new ObjectSelected();
                    so.obj = o;
                    so.rend = o.GetComponent<Renderer>();
                    so.polmObj = o;

                    so.oMesh = so.polmObj.o_Mesh;
                    if (so.rend as MeshRenderer)
                    {
                        if (so.rend.GetComponent<MeshFilter>().sharedMesh)
                            so.bMesh = (Mesh)Instantiate(so.rend.GetComponent<MeshFilter>().sharedMesh);
                        else { Debug.Log("The renderer has no reference to a mesh and will be missed"); so.isValid = false; continue; }
                    }
                    else if (so.rend as SkinnedMeshRenderer)
                    {
                        if ((so.rend as SkinnedMeshRenderer).sharedMesh)
                            so.bMesh = (Mesh)Instantiate((so.rend as SkinnedMeshRenderer).sharedMesh);
                        else { Debug.Log("The renderer has no reference to a mesh and will be missed"); so.isValid = false; continue; }
                    }
                    so.bMesh.name = (so.bMesh.name.Remove(so.bMesh.name.Length - 7)) + "_Baked";
                    so.bMesh.hideFlags = HideFlags.HideAndDontSave;

                    //if (so.rend as MeshRenderer)
                    //    so.oMesh = so.rend.transform.GetComponent<MeshFilter>().sharedMesh;
                    //else if (so.rend as SkinnedMeshRenderer)
                    //    so.oMesh = (so.rend as SkinnedMeshRenderer).sharedMesh;

                    so.vertices = so.oMesh.vertices;
                    so.normals = so.oMesh.normals;
                    so.triangles = so.oMesh.triangles;
                    so.uvs = so.oMesh.uv;

                    so.mats = so.rend.sharedMaterials;
                    so.meshName = so.rend.name + "_AO";
                    so.autoSaveMesh = true;
                    so.autoApplyMesh = true;
                    so.hash = so.rend.GetHashCode();

                    objectsSelected.Add(so);
                }
            }
        }
        if (GUILayout.Button("Clear Selection"))
        {
            objectsSelected.Clear();
        }
        if (GUILayout.Button("All As \"To Bake\""))
        {
            foreach (ObjectSelected o in objectsSelected)
                o.bakeMesh = true;
        }
        if (GUILayout.Button("All As Blockers"))
        {
            foreach (ObjectSelected o in objectsSelected)
                o.bakeMesh = false;
        }
        EditorGUILayout.EndHorizontal();   // END HORIZONTAL BUTTONS GROUP

        // Manually call SceneView Repaint in order to refresh the bounds gizmos
        SceneView.RepaintAll();

        //GUILayout.Space(5f);        
        //EditorGUILayout.BeginHorizontal("Box");
        //if (GUILayout.Button("Selection as Blockers"))
        //{

        //}
        //if (GUILayout.Button("Selection as \"To Bake\""))
        //{

        //}
        //EditorGUILayout.EndHorizontal();
        GUILayout.Space(5f);
        
        EditorGUILayout.BeginVertical("box");                                               // BEGIN VERTICAL GROUP 3

        // Draw per selected object settings
        pvMeshBakeListScrollView = GUILayout.BeginScrollView(pvMeshBakeListScrollView);
        int i = 0;
        foreach (ObjectSelected o in objectsSelected)
        {
            i++;

            GUI.color = isWorking ? (!o.isBaked ? POLMColors.lightRed : POLMColors.lightGreen) : Color.white;
            EditorGUILayout.BeginHorizontal("box");
            GUI.color = Color.white;

            // The object id in the list
            GUILayout.Label(i.ToString(), new GUILayoutOption[] { GUILayout.MaxWidth(20f) });
            if (o.obj && o.rend)
            {
                if (bakingMode == 0)
                    o.obj = EditorGUILayout.ObjectField(o.obj, typeof(Object), true, new GUILayoutOption[] { GUILayout.MaxWidth(200f) });
                else
                {
                    if (!o.polmObj)
                    {
                        toCleanUpObjectList = true;
                        break;
                    }
                    else o.obj = EditorGUILayout.ObjectField(o.obj, typeof(Object), true, new GUILayoutOption[] { GUILayout.MaxWidth(200f) });
                }
            }
            else
            {
                //GUILayout.Label("Missing", new GUILayoutOption[] { GUILayout.MaxWidth(200f) });
                toCleanUpObjectList = true;
                break;
            }

            if (bakingMode == 0)
            {

                if (o.isValid)
                {
                    o.bakeMesh      = GUILayout.Toggle(o.bakeMesh, "Bake");
                    o.uvSet         = (UVSet)GUILayout.SelectionGrid((int)o.uvSet, selGridUV, selGridUV.Length);

                    int matsCount = o.mats.Length;
                    bool multiMats = o.MultiMat;
                    GUI.contentColor = multiMats ? POLMColors.lightGreen : POLMColors.lightRed;
                    EditorGUILayout.LabelField(matsCount > 1 ? "Multi Material" : "Single Material", new GUILayoutOption[] { GUILayout.Width(100f) });
                    GUI.contentColor = Color.white;

                }
                else
                {
                    GUI.backgroundColor = POLMColors.lightRed;
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField("Objects invalid : check normals, uvs etc...");
                    EditorGUILayout.EndVertical();
                    GUI.backgroundColor = Color.white;
                }
                //o.autoSaveMesh  = GUILayout.Toggle(o.autoSaveMesh, "Auto Save Texture");
            }
            else
            {
                o.bakeMesh      = GUILayout.Toggle(o.bakeMesh, "Bake Mesh");
                //o.autoSaveMesh  = GUILayout.Toggle(o.autoSaveMesh, "Аuto Save Mesh");
                //o.autoApplyMesh = GUILayout.Toggle(o.autoApplyMesh, "Аuto Apply Mesh");
            }

            // Objects "Baked" state
            GUILayout.Label(o.bakeMesh ? (o.isBaked ? "\u2713" : "---") : "SKIPPED", new GUILayoutOption[] { GUILayout.MaxWidth(30f) });

            EditorGUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        EditorGUILayout.EndVertical();                                                      // END VERTICAL GROUP 3
        // END GROUP 3 - SELECTED OBJECTS
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }

    /// <summary>
    /// Cleanup non valid elements
    /// </summary>
    void CleanUpObjectList()
    {
        objectsSelected.RemoveAll(x => x.polmObj == null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sceneView"></param>
    void OnSceneGUI(SceneView sceneView)
    {
        //// Do your drawing here using Handles.
        //// Do your drawing here using GUI.
        //Handles.color = new Color(0f, 1f, 0f, 0.5f);
        //Handles.BeginGUI();
        //int i = 0;
        //foreach (ObjectSelected o in objectsSelected)
        //{
        //    i++;
        //    if (o.obj)
        //    {

        //        Handles.matrix = Matrix4x4.identity;
        //        Handles.Label(o.rend.bounds.center + (Vector3.up * (o.rend.bounds.extents.y + 0.3f)), i.ToString());

        //        Matrix4x4 oM = Matrix4x4.TRS(   o.rend.bounds.center,
        //                                        o.rend.transform.rotation,
        //                                        o.rend.transform.lossyScale);
        //        Handles.matrix = oM;
        //        Handles.DrawWireCube(Vector3.zero, o.oMesh.bounds.size);
        //    }
        //}
        //Handles.EndGUI();
    }
    
    // ----------------------------------------------------------------------------------------------------------------------------------
    // MAIN FUNCTIONS
    // ----------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Bake the Ambient Occlusion Map Using GPU
    /// </summary>
    /// <returns></returns>
    public IEnumerator BakeAO()
    {
        isWorking = true;

        if (shaderSlot > 0)
        {
            foreach (ObjectSelected so in objectsSelected)
            {
                if (so.bakeMesh)
                {
                    if (shaderSlot == 1)
                    {
                        foreach (Material m in so.mats)
                            if (m.HasProperty("_OcclusionMap")) m.SetTexture("_OcclusionMap", null);
                    }
                    else if (shaderSlot == 2)
                    {
                        foreach (Material m in so.mats)
                            if (m.HasProperty("_DetailAlbedoMap")) m.SetTexture("_DetailAlbedoMap", null);
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // find the kernel which bakes Ambient Occlusion
        int kernel = shader.FindKernel("BakeAO");

        // -----------------------------------------------------------------------------------------------------------------------------------------
        // Creating the BVH Tree and prepare the buffers to upload to the GPU
        BVHNode bvh = null;
        List<IPrimitive> bvhNodes = null;
        Build_BVH_Global(ref bvh, ref bvhNodes);

        // Creatling a buffer with triangles to upload to GPU
        List<LBVHNODE> gpuNodes = new List<LBVHNODE>();
        List<LBVHTriangle> primitives = new List<LBVHTriangle>();
        for (int i = 0; i < bvhNodes.Count; i++)
        {

            // CREATING THE BVH NODES TO BE UPLOADED INTO GPU MEMORY
            gpuNodes.Add(new LBVHNODE(  bvhNodes[i].BBox.min,
                                        bvhNodes[i].BBox.max,
                                        (uint)bvhNodes[i].LChildID,
                                        (uint)bvhNodes[i].RChildID,
                                        (uint)bvhNodes[i].NodeID,
                                        bvhNodes[i].IsLeaf ? (uint)1 : (int)0));
            // CREATING THE PRIMITIVES(TRIANGLES) TO BE UPLOADED INTO GPU MEMORY
            if (bvhNodes[i].IsLeaf)
            {
                BVHTriangle bvhTr = (bvhNodes[i] as BVHTriangle);
                primitives.Add(new LBVHTriangle(bvhTr.v0, bvhTr.v1, bvhTr.v2, (uint)bvhNodes[i].NodeID));
            }
            else
            {
                primitives.Add(new LBVHTriangle(Vector3.zero, Vector3.zero, Vector3.zero, (uint)bvhNodes[i].NodeID));
            }
        }

        // End - Creating the BVH Tree
        // -----------------------------------------------------------------------------------------------------------------------------------------
                
        // -----------------------------------------------------------------------------------------------------------------------------------------
        // Initialize the buffer with nodes which will be used in the shader
        ComputeBuffer nodesBuffer = new ComputeBuffer(gpuNodes.Count, 64, ComputeBufferType.Default);  // Last Node version stride was 64
        nodesBuffer.SetData(gpuNodes.ToArray());

        // Initialize the triangle buffer which will be used in the shader
        ComputeBuffer primsBuffer = new ComputeBuffer(primitives.Count, 64, ComputeBufferType.Default); // 64 bytes of size
        primsBuffer.SetData(primitives.ToArray());

        shader.SetBuffer(kernel, "nodes", nodesBuffer);
        shader.SetBuffer(kernel, "tris", primsBuffer);

        // 1D AND 2D TEXTURES CONTAINING RANDOM VECTORS
        shader.SetTexture(kernel, "RandomVectors1D", randVectors1D);
        shader.SetTexture(kernel, "RandomVectors2D", randVectors2D);

        // Set the samples count
        samples = sampleCount[samplesID];
        shader.SetInt("samplesTypeID", raySamplesID);

        ComputeBuffer randsXBuf = new ComputeBuffer(64, 4, ComputeBufferType.Default);
        ComputeBuffer randsYBuf = new ComputeBuffer(64, 4, ComputeBufferType.Default);
        // ------------------------------------------------------------------------------------------------------------------
        foreach (ObjectSelected so in objectsSelected)
        {
            // CANCEL WHENEVER NEEDED
            if (!isWorking)
            {
                Debug.Log("Baking Canceled");
                break;
            }

            currentProgress = 0f;

            if (!so.bakeMesh) continue;

            currentJobLabel = "PRECOMPUTING";
            Repaint();
            yield return null;

            float t1 = Time.realtimeSinceStartup;
            PrecomputeObject(so);
            Debug.Log("Precomputed in: " + (Time.realtimeSinceStartup - t1));

            // isValid is set to false in case the uvs are missing etc.
            if (so.isValid == false) { continue; }

            // Debug - draw lumels in scene
            //foreach (Lumel l in so.lumels)
            //{ if(l.valid > 0f) Debug.DrawRay(l.position, l.n * 0.05f, Color.green, 10f); }

            // Lumels (Pixels) 
            ComputeBuffer lumelsBuffer = new ComputeBuffer(so.lumels.Count, 32, ComputeBufferType.Default);  // 32 bytes in size 
            lumelsBuffer.SetData(so.lumels.ToArray());

            // -----------------------------------------------------------------------------------------------------------------------------------------
            // Setup(upload) some stuff to GPU memory

            // Set up texture's BG color
            int bgColorKernel = shader.FindKernel("SetTextureBGColor");
            Vector4 bgColor = new Vector4(polmData.textureBGColor.r, polmData.textureBGColor.g, polmData.textureBGColor.b, polmData.textureBGColor.a);
            shader.SetVector("textureBGColor", bgColor);
            shader.SetTexture(bgColorKernel, "rendTex", so.rendTex);
            shader.Dispatch(bgColorKernel, textureSize / 8, textureSize / 8, 1);


            shader.SetFloat("AOStep", 1f / samples);
            shader.SetFloat("AOPower", AOPower);
            shader.SetFloat("fRadius", radius);
            shader.SetFloat("cleanUpValue", Mathf.Lerp(0.01f, 0.45f, cleanUpValue));
            shader.SetInt("imageSize", textureSize);
            shader.SetInt("samples", samples);
            shader.SetTexture(kernel, "rendTex", so.rendTex);
            shader.SetBuffer(kernel, "lumels", lumelsBuffer);

            shader.SetBuffer(kernel, "nodes", nodesBuffer);

            shader.SetTexture(kernel, "RandomVectors1D", randVectors1D);
            shader.SetTexture(kernel, "RandomVectors2D", randVectors2D);

            // Apply the texture to all materials the renderer is using

            if (shaderSlot > 0)
            {
                foreach (Material m in so.mats)
                {
                    // _OcclusionMap
                    // _DetailAlbedoMap
                    if (shaderSlot == 1)
                    {
                        if (m.HasProperty("_OcclusionMap")) m.SetTexture("_OcclusionMap", so.rendTex);
                        if (m.HasProperty("_DetailAlbedoMap")) m.SetTexture("_DetailAlbedoMap", null);
                    }
                    else if (shaderSlot == 2)
                    {
                        if (m.HasProperty("_OcclusionMap")) m.SetTexture("_OcclusionMap", null);
                        if (m.HasProperty("_DetailAlbedoMap")) m.SetTexture("_DetailAlbedoMap", so.rendTex);
                        if (m.HasProperty("_UVSec"))
                        {
                            if (so.uvSet == UVSet.UV)
                            {
                                m.SetFloat("_UVSec", 0f);
                            }
                            else if (so.uvSet == UVSet.UV2)
                            {
                                m.SetFloat("_UVSec", 1f);
                            }
                        }
                    }
                }
            }

            // ---------------------------------------------------------------------------------------------------------------------------------------
            // Dispatch AO baking kernel
            // ---------------------------------------------------------------------------------------------------------------------------------------

            float t2 = Time.realtimeSinceStartup;

            currentJobLabel = "BAKING";
            Repaint();
            yield return null;

            int sampleID = 0;
            bool bakeCanceled = false;
            for (int i = 0; i < samples; i++)
            {
                // CANCEL WHENEVER NEEDED
                if (!isWorking)
                {
                    if (so.bakeMesh)
                    {
                        bakeCanceled = true;
                        if (shaderSlot == 1)
                        {
                            foreach (Material m in so.mats)
                            {
                                if (m.HasProperty("_OcclusionMap")) m.SetTexture("_OcclusionMap", null);
                            }
                        }
                        else if (shaderSlot == 2)
                        {
                            foreach (Material m in so.mats)
                            {
                                if (m.HasProperty("_DetailAlbedoMap")) m.SetTexture("_DetailAlbedoMap", null);
                            }
                        }
                    }
                    break;
                }

                shader.SetInt("sampleID", sampleID);
                shader.SetInt("randShiftX", Random.Range(0, 64 - 8));
                shader.SetInt("randShiftY", Random.Range(0, 64 - 8));
                List<int> randsX = new List<int>();
                List<int> randsY = new List<int>();
                for (int r = 0; r < 64; r++)
                {
                    int r1 = Random.Range(0, 64);
                    randsX.Add(r1);
                    int r2 = Random.Range(0, 64);
                    randsY.Add(r2);
                }
                randsXBuf.SetData(randsX.ToArray());
                randsYBuf.SetData(randsY.ToArray());
                shader.SetBuffer(kernel, "randsX", randsXBuf);
                shader.SetBuffer(kernel, "randsY", randsYBuf);

                // Dispatch the bake kernel
                shader.Dispatch(0, textureSize / ThreadGroupSize, textureSize / ThreadGroupSize, 1);

                currentProgress = (float)i / samples;

                sampleID++;

                yield return null;
                SceneView.RepaintAll();
            }
            
            Debug.Log("AO baked in: " + (Time.realtimeSinceStartup - t2));

            if (!bakeCanceled)
            {
                currentProgress = 1f;

                currentJobLabel = "BLURRING AND DILATING TEXTURE";
                Repaint();

                // ==============================================================================
                // Calculate the final AO map using the information from the calculated lumels
                // If not saving the RenderTexture directly
                // ==============================================================================
                Texture2D finalTexture = new Texture2D(textureSize, textureSize);
                Lumel[] calcLumels = new Lumel[so.lumels.Count];
                lumelsBuffer.GetData(calcLumels);
                for (int j = 0; j < textureSize; j++)
                {
                    for (int i = 0; i < textureSize; i++)
                    {
                        int lumelID = j * textureSize + i;
                        if (calcLumels[lumelID].valid == 1f)
                        {
                            float colorPart = calcLumels[lumelID].lumelColor;
                            Color c = new Color(colorPart, colorPart, colorPart, 1.0f);
                            finalTexture.SetPixel(i, j, c);                            
                        }
                        else
                        {
                            Color c = new Color(0f, 0f, 0f, 0f);
                            finalTexture.SetPixel(i, j, c);
                        }
                    }
                }

                yield return new WaitForSeconds(0.2f);

                if (lumelsBuffer != null)
                    lumelsBuffer.Release();

                Texture2D tempTexture = null;

                // Dilate and Blur
                if (dilateTexture || blurTexture)
                {
                    tempTexture = RenderTextureToTexture2D(so.rendTex);
                    shader.SetInt("imageSize", textureSize);
                    if (dilateTexture)
                    {
                        int dilateKernel = shader.FindKernel("Dilate");
                        for (int i = 0; i < marginSamples; i++)
                        {
                            tempTexture = RenderTextureToTexture2D(so.rendTex);
                            shader.SetTexture(dilateKernel, "rendTex", so.rendTex);
                            shader.SetTexture(dilateKernel, "rendTexRead", tempTexture);
                            shader.Dispatch(dilateKernel, textureSize / 8, textureSize / 8, 1);
                        }
                    }
                    if (blurTexture)
                    {
                        int blurKernel = shader.FindKernel("Blur");

                        for (int i = 0; i < blurSamples; i++)
                        {
                            tempTexture = RenderTextureToTexture2D(so.rendTex);
                            shader.SetTexture(blurKernel, "rendTex", so.rendTex);
                            shader.SetTexture(blurKernel, "rendTexRead", tempTexture);
                            shader.Dispatch(blurKernel, textureSize / 8, textureSize / 8, 1);
                        }
                    }
                }

                // Darken texture based on "darkenMultiplier" value
                if (darkenMultiplier > 0f)
                {
                    int darkenKernel = shader.FindKernel("Darken");
                    shader.SetFloat("darkenMultiplier", Mathf.Lerp(1f, 0f, darkenMultiplier));
                    tempTexture = RenderTextureToTexture2D(so.rendTex);
                    shader.SetTexture(darkenKernel, "rendTex", so.rendTex);
                    shader.SetTexture(darkenKernel, "rendTexRead", tempTexture);
                    shader.Dispatch(darkenKernel, textureSize / 8, textureSize / 8, 1);
                }

                if (bSaveMaps)
                {
                    currentJobLabel = "SAVING TEXTURE";
                    Repaint();
                    yield return null;

                    SaveRenderTextureToFile(so, so.rendTex, so.rend.name + "_AO.png");
                }

                currentJobLabel = "OBJECT DONE";
                Repaint();
                yield return null;

            }
            else
            {
                currentJobLabel = "BAKE CANCELED";
                Repaint();
                yield return null;
                
                currentProgress = 0f;

                if (lumelsBuffer != null)
                    lumelsBuffer.Release();
            }
           
            // Set baked flag to true
            so.isBaked = true;
        }

        randsXBuf.Release();
        randsYBuf.Release();
        nodesBuffer.Release();
        primsBuffer.Release();

        BakingDone();
    }
    
    /// <summary>
    /// Calculate AO per vertex using the GPU
    /// </summary>
    /// <returns></returns>
    public IEnumerator BakeAOPV()
    {
        // Rising the baking flag
        isWorking = true;

        // Set the samples count
        samples = sampleCount[samplesID];

        // -----------------------------------------------------------------------------------------------------------------------------------------
        // Find the kernel which bakes Ambient Occlusion Per Vercer
        int kernel = shader.FindKernel("BakeAOPV");

        // -----------------------------------------------------------------------------------------------------------------------------------------
        // Creating the BVH Tree and prepare the buffers to upload to the GPU
        BVHNode bvh = null;
        List<IPrimitive> bvhNodes = null;
        Build_BVH_Global(ref bvh, ref bvhNodes);

        // Creatling a buffer with triangles to upload to GPU
        List<LBVHNODE>     gpuNodes   = new List<LBVHNODE>();
        List<LBVHTriangle> primitives = new List<LBVHTriangle>();
        for(int i = 0; i < bvhNodes.Count; i++)
        {

            // CREATING THE BVH NODES TO BE UPLOADED INTO GPU MEMORY
            gpuNodes.Add(new LBVHNODE(  bvhNodes[i].BBox.min,
                                        bvhNodes[i].BBox.max,
                                        (uint)bvhNodes[i].LChildID, 
                                        (uint)bvhNodes[i].RChildID,
                                        (uint)bvhNodes[i].NodeID,
                                        bvhNodes[i].IsLeaf ? (uint)1 : (int)0));
            // CREATING THE PRIMITIVES(TRIANGLES) TO BE UPLOADED INTO GPU MEMORY
            if (bvhNodes[i].IsLeaf)
            {
                BVHTriangle bvhTr = (bvhNodes[i] as BVHTriangle);
                primitives.Add(new LBVHTriangle(bvhTr.v0, bvhTr.v1, bvhTr.v2, (uint)bvhNodes[i].NodeID));
            }
            else
            {
                primitives.Add(new LBVHTriangle(Vector3.zero, Vector3.zero, Vector3.zero, (uint)bvhNodes[i].NodeID));
            }
        }
        // End - Creating the BVH Tree
        // -----------------------------------------------------------------------------------------------------------------------------------------

        // Initialize the buffer with nodes which will be used in the shader
        ComputeBuffer nodesBuffer = new ComputeBuffer(gpuNodes.Count, 64, ComputeBufferType.Default);  // Last Node version stride was 64
        nodesBuffer.SetData(gpuNodes.ToArray());

        // Initialize the triangle buffer which will be used in the shader
        ComputeBuffer primsBuffer = new ComputeBuffer(primitives.Count, 64, ComputeBufferType.Default); // 64 bytes of size
        primsBuffer.SetData(primitives.ToArray());

        shader.SetBuffer(kernel, "nodes", nodesBuffer);
        shader.SetBuffer(kernel, "tris",  primsBuffer);

        // 1D AND 2D TEXTURES CONTAINING RANDOM VECTORS
        shader.SetTexture(kernel, "RandomVectors1D", randVectors1D);
        shader.SetTexture(kernel, "RandomVectors2D", randVectors2D);

        // ------------------------------------------------------------------------------------------------------------------
        foreach (ObjectSelected so in objectsSelected)
        {
            if (!so.oMesh)
            { Debug.Log("Mesh must be of type MeshRenderer or SkinnedMeshRenderer"); continue; }

            if (!so.bakeMesh) continue;

            // CANCEL WHENEVER NEEDED
            if (!isWorking)
            {
                Debug.Log("Baking Canceled");
                break;
            }

            Vector3[] meshVerts = so.oMesh.vertices;
            Vector3[] meshNorms = so.oMesh.normals;
            int vertexCount = meshVerts.Length;
            
            if ( so.lumels == null)
                 so.lumels = new List<Lumel>();
            else so.lumels.Clear();

            for (int i = 0; i < meshVerts.Length; i++)
            {
                meshVerts[i] = so.rend.transform.TransformPoint(meshVerts[i]);
                meshNorms[i] = so.rend.transform.TransformDirection(meshNorms[i]);
                so.lumels.Add(new Lumel(meshVerts[i], meshNorms[i], 1f, 1f));
            }

            // Calculating random samples to update to the GPU
            ComputeBuffer lumelsBuffer = new ComputeBuffer(meshVerts.Length, 32, ComputeBufferType.Default);  // 32 bytes in size 
            lumelsBuffer.SetData(so.lumels.ToArray());

            shader.SetFloat("AOStep", 1f / samples);
            shader.SetFloat("AOPower", AOPower);
            shader.SetFloat("fRadius", radius);
            shader.SetFloat("cleanUpValue", Mathf.Lerp(0.01f, 0.45f, cleanUpValue));
            shader.SetInt("samples", samples);
            shader.SetInt("vertexCount", vertexCount);
            shader.SetBuffer(kernel, "lumels", lumelsBuffer);

            int _samples = samples;
            int sampleID = 0;
            shader.SetFloat("AOStep", 1f / (_samples));
            shader.SetVector("randRay1", Random.onUnitSphere);
            shader.SetVector("randRay2", Random.onUnitSphere);

            bool bakeCanceled = false;

            // LOOPING OVER SAMPLES COUNT
            for (int i = 0; i < _samples; i++)
            {
                // CANCEL WHENEVER NEEDED
                if (!isWorking)
                {
                    bakeCanceled = true;
                    Debug.Log("Baking Canceled");
                    break;
                }

                shader.SetInt("sampleID", sampleID);
                // DISPATCHING KERNEL ----------------------------------------------------
                shader.Dispatch(kernel, vertexCount, 1, 1);
                currentProgress = ((float)i + 1) / _samples;
                sampleID++;
                
                yield return null;
                SceneView.RepaintAll();
            }

            if (!bakeCanceled)
            {
                // ------------------------------------------------------------------------------------------------------------------
                // APPLY VERTEX COLORS TO THE MESH
                Color[] meshColors = new Color[vertexCount];
                Lumel[] _lumels = new Lumel[lumelsBuffer.count];
                lumelsBuffer.GetData(_lumels);
                for (int i = 0; i < _lumels.Length; i++)
                {
                    meshColors[i].r = _lumels[i].lumelColor;
                    meshColors[i].g = _lumels[i].lumelColor;
                    meshColors[i].b = _lumels[i].lumelColor;
                }

                so.bMesh.colors = meshColors;

                string meshSaveLoc = "";
                if (so.autoSaveMesh)
                {
                    meshSaveLoc = SaveMesh(so.bMesh, so.meshName, true);
                }
                Mesh m = AssetDatabase.LoadAssetAtPath(meshSaveLoc, typeof(Mesh)) as Mesh;

                if (so.polmObj)
                {
                    so.polmObj.b_Mesh = m;
                    so.polmObj.PreviewBakedMesh();
                }
                else Debug.Log("POLMObject component not added to object");

                // ------------------------------------------------------------------------------------------------------------------
                // Set baked flag to true
                so.isBaked = true;
                currentProgress = 1f;
            }
            else
            {
                lumelsBuffer.Release();
                break;
            }
            lumelsBuffer.Release();
        }

        yield return null;

        nodesBuffer.Release();
        primsBuffer.Release();

        BakingDone();
    }

    /// <summary>
    /// Calculate AO per vertex using the CPU
    /// </summary>
    /// <returns></returns>
    public IEnumerator BakeAOPV_CPU()
    {
        // Rising the baking flag
        isWorking = true;

        // Set the samples count
        samples = sampleCount[samplesID];

        // -----------------------------------------------------------------------------------------------------------------------------------------
        // Creating the BVH Tree
        // -----------------------------------------------------------------------------------------------------------------------------------------

        //BVHNode bvh = null;
        //// This is a buffer used to store the triangles bboxes
        //List<IPrimitive> trPrims  = new List<IPrimitive>();
        //// This is a buffer used to store nodes while the bvh tree is building
        //List<IPrimitive> bvhNodes = new List<IPrimitive>();
        //BuildBVHPrimitives(ref trPrims);
        //// Creating the bvh
        //bvh = new BVHNode(trPrims, 0f, 1f, ref bvhNodes); // TODO : check why are both parameters 0 and 1
        //// Add the root at the end as well
        //bvhNodes.Add(bvh);

        BVHNode bvh = null;
        List<IPrimitive> bvhNodes = null;
        Build_BVH_Global(ref bvh, ref bvhNodes);

        // -----------------------------------------------------------------------------------------------------------------------------------------
        // Backing per object PerVertex-AO
        // -----------------------------------------------------------------------------------------------------------------------------------------

        foreach (ObjectSelected so in objectsSelected)
        {
            if (!so.oMesh)
            { Debug.Log("Mesh must be of type MeshRenderer or SkinnedMeshRenderer"); continue; }

            if (!so.bakeMesh) continue;

            // CANCEL WHENEVER NEEDED
            if (!isWorking)
            {
                Debug.Log("Baking Canceled");
                break;
            }
            
            Vector3[] meshVerts = so.oMesh.vertices;
            Vector3[] meshNorms = so.oMesh.normals;
            
            if (so.lumels == null)
                 so.lumels = new List<Lumel>();
            else so.lumels.Clear();

            for (int i = 0; i < so.oMesh.vertices.Length; i++)
            {
                meshVerts[i] = so.rend.transform.TransformPoint(meshVerts[i]);
                meshNorms[i] = so.rend.transform.TransformDirection(meshNorms[i]);
                so.lumels.Add(new Lumel(meshVerts[i], meshNorms[i], 1f, 1f));
            }
            
            float valueToRemove = 1f / (int)samples;

            bool bakingCanceled = false;

            // LOOPING SAMPLES
            for (int s = 0; s < samples; s++)
            {
                // CANCEL WHENEVER NEEDED
                if (!isWorking)
                {
                    Debug.Log("Baking Canceled");
                    bakingCanceled = true;
                    break;
                }

                for (int i = 0; i < so.lumels.Count; i++)
                {
                    // CANCEL WHENEVER NEEDED
                    if (!isWorking)
                    {
                        Debug.Log("Baking Canceled");
                        bakingCanceled = true;
                        break;
                    }

                    Lumel lum = so.lumels[i];
                    Vector3 rayO = so.lumels[i].position + so.lumels[i].n * 0.00001f;
                    Vector3 rayD = so.lumels[i].n;

                    Vector3 outDir = Random.onUnitSphere;
                    if (Vector3.Dot(rayD, outDir) < 0.0f) outDir = -outDir;
                    Ray ray = new Ray(rayO, outDir);

                    HitInfo hitInfo = new HitInfo();
                    if (bvh.TraverseWithStack(ray, ref bvhNodes, ref hitInfo))
                    {
                        lum.lumelColor -= (valueToRemove * Mathf.Lerp(1f, 0f, hitInfo.hitDistance / radius));
                        so.lumels[i] = lum;
                    }

                }
                currentProgress = ((float)s + 1) / samples;

                yield return null;
                SceneView.RepaintAll();
            }

            if (!bakingCanceled)
            {
                // ------------------------------------------------------------------------------------------------------------------
                // APPLY VERTEX COLORS TO THE MESH
                Color[] meshColors = new Color[so.lumels.Count];
                for (int i = 0; i < so.lumels.Count; i++)
                {
                    meshColors[i].r = so.lumels[i].lumelColor;
                    meshColors[i].g = so.lumels[i].lumelColor;
                    meshColors[i].b = so.lumels[i].lumelColor;
                }
                so.bMesh.colors = meshColors;

                string meshSaveLoc = "";
                if (so.autoSaveMesh)
                {
                    meshSaveLoc = SaveMesh(so.bMesh, so.meshName, true);
                }
                Mesh m = AssetDatabase.LoadAssetAtPath(meshSaveLoc, typeof(Mesh)) as Mesh;

                if (so.polmObj)
                {
                    so.polmObj.b_Mesh = m;
                    so.polmObj.PreviewBakedMesh();
                }
                else Debug.Log("POLMObject component not added to object");

                // ------------------------------------------------------------------------------------------------------------------
                so.isBaked = true;
                currentProgress = 1f;
            }
            else
            {
                break;
            }
        }

        yield return null;
        BakingDone();
    }
    
    /// <summary>
    /// Saving the mesh as an Asset
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="name"></param>
    /// <param name="makeNewInstance"></param>
    /// <param name="optimizeMesh"></param>
    public static string SaveMesh(Mesh mesh, string name, bool makeNewInstance)
    {        
        string path = ("Assets" + polmData.mDir + "/" + name + ".asset");

        Mesh meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;
        AssetDatabase.CreateAsset(meshToSave, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return path;
    }

    // ----------------------------------------------------------------------------------------------------------------------------------
    // HELPER FUNCTIONS
    // ----------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Clean up everything
    /// </summary>
    void CleanUp()
    {
        foreach (ObjectSelected so in objectsSelected)
        { so.isBaked = false; }

        isWorking = false;
        currentProgress = 0f;
    }

    /// <summary>
    /// 
    /// </summary>
    void CancelWork()
    {
        foreach (ObjectSelected so in objectsSelected)
        { so.isBaked = false; }

        isWorking = false;
        currentProgress = 1f;
    }

    /// <summary>
    /// 
    /// </summary>
    void BakingDone()
    {
        isWorking = false;
        currentProgress = 1f;
    }

    /// <summary>
    /// Preprocess the objects
    /// </summary>
    void PrecomputeObject(ObjectSelected so)
    {
        // Part 1 ------------------------------------------------------------------------------------------------------------------------------
        textureSize = textureSizesPxl[textureSizeID];
        samples = sampleCount[samplesID];

        currentProgress = 0f;

        if (so.rendTex)
        {
            DestroyImmediate(so.rendTex);
        }

        if(platform == CurrentPlatform.DX)
            so.rendTex = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
        else if(platform == CurrentPlatform.GL || platform == CurrentPlatform.GLES)
            so.rendTex = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

        so.rendTex.enableRandomWrite = true;
        so.rendTex.Create();

        // Part 2 ------------------------------------------------------------------------------------------------------------------------------

        Vector3[]   uvPnts      = null;
        int[]       uvTris      = null;
        Vector3[]   vertices2D  = new Vector3[so.oMesh.vertices.Length];

        so.isValid = true;

        switch (so.uvSet)
        {
            case UVSet.UV:

                if (so.oMesh.uv.Length != so.oMesh.vertexCount)
                {
                    Debug.LogError("UV Set 1 not available for mesh: " + so.oMesh.name, so.rend.transform);
                    so.isValid = false;
                    return;
                }
                else so.uvs = so.oMesh.uv;
              
                break;
            case UVSet.UV2:

                if (so.oMesh.uv2.Length != so.oMesh.vertexCount)
                {
                    Debug.LogError("UV Set 2 not available for mesh: " + so.oMesh.name, so.rend.transform);
                    so.isValid = false;
                    return;
                }
                else so.uvs = so.oMesh.uv2;

                break;
            case UVSet.UV3:

                if (so.oMesh.uv3.Length != so.oMesh.vertexCount)
                {
                    Debug.LogError("UV Set 3 not available for mesh: " + so.oMesh.name, so.rend.transform);
                    so.isValid = false;
                    return;
                }
                else so.uvs = so.oMesh.uv3;
                break;
            case UVSet.UV4:

                if (so.oMesh.uv4.Length != so.oMesh.vertexCount)
                {
                    Debug.LogError("UV Set 4 not available for mesh: " + so.oMesh.name, so.rend.transform);
                    so.isValid = false;
                    return;
                }
                else so.uvs = so.oMesh.uv4;
                break;
        }

        // Return in case the objects is not validated
        if (!so.isValid) return;

        for (int i = 0; i < vertices2D.Length; i++)
        {
            vertices2D[i] = new Vector3(so.uvs[i].x, so.uvs[i].y, 0f);
            // Draw 2D points
            //Debug.DrawRay(vertices2D[i], Vector3.back * 0.1f, Color.green, 10f);            
        }
        
        so.uvMesh = new Mesh();
        so.uvMesh.vertices  = vertices2D;
        so.uvMesh.triangles = so.oMesh.triangles;
        // NOTE : uvs currently not setup as not needed - add them if needed
        uvPnts = vertices2D;
        uvTris = so.oMesh.triangles;

        so.uvMesh.hideFlags = HideFlags.HideAndDontSave;

        so.lumels = new List<Lumel>(new Lumel[textureSize * textureSize]);

        // Part 3 -----------------------------------------------------------------------------------------------------------------------------------------

        so.triangles2D = new Triangle2D[uvTris.Length / 3];

        for (int t = 0; t < uvTris.Length / 3; t++)
        {
            so.triangles2D[t] = new Triangle2D(uvPnts[uvTris[t * 3]], uvPnts[uvTris[t * 3 + 1]], uvPnts[uvTris[t * 3 + 2]]);

            // Debug 2D triangle normal
            //Vector3 trCross = Vector3.Cross(so.triangles2D[t].v1 - so.triangles2D[t].v0, so.triangles2D[t].v2 - so.triangles2D[t].v0).normalized;
            //if (trCross.z < 0f) Debug.DrawRay(so.triangles2D[t].v0, trCross, Color.red, 5f);
            //else Debug.DrawRay(so.triangles2D[t].v0, trCross, Color.green, 5f);

            // Drawing 2D triangles
            //Debug.DrawLine(so.triangles2D[t].v0, so.triangles2D[t].v1, Color.red, 10f);
            //Debug.DrawLine(so.triangles2D[t].v1, so.triangles2D[t].v2, Color.green, 10f);
            //Debug.DrawLine(so.triangles2D[t].v2, so.triangles2D[t].v0, Color.blue, 10f);
        }

        
        // -----------------------------------------------------------------------------------------------------------------------------------------
        // Building Lumels using Rasterization

        try
        {
            currentProgress = 0f;
            Rasterizer.BuildLumels(so, interpolateNormals);
            currentProgress = 1f;
        }
        catch (System.Exception e) // if object is NULL or other exception in thrown
        {
            Debug.Log(e.Message);
        }

        // End building lumels
        // -----------------------------------------------------------------------------------------------------------------------------------------
    }
        
    /// <summary>
    /// Build the primitives to all selected objects to be used in the BVH
    /// </summary>
    void Build_BVH_Global(ref BVHNode bvh, ref List<IPrimitive> bvhNodes)
    {
        // Current created primitive id
        int pID = -1;

        // This is a buffer used to store the triangles bboxes
        List<IPrimitive> primitives = new List<IPrimitive>();

        foreach (ObjectSelected o in objectsSelected)
        {
            if (o.rend && o.oMesh)
            {
                if (!o.isValid)
                {
                    continue;
                }

                Vector3[] vertices = o.oMesh.vertices;
                Vector3[] normals = o.oMesh.normals;
                int[] triangles = o.oMesh.triangles;

                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = o.rend.transform.TransformPoint(vertices[i]);
                    normals[i] = o.rend.transform.TransformDirection(normals[i]);
                }

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 v0 = vertices[triangles[i]];
                    Vector3 v1 = vertices[triangles[i + 1]];
                    Vector3 v2 = vertices[triangles[i + 2]];
                    BVHTriangle p = new BVHTriangle(v0, v1, v2, ++pID);
                    primitives.Add(p);
                }
            }
            else Debug.Log("Renderer or mesh is missing");
        }

        // This is a buffer used to store nodes while the bvh tree is building
        bvhNodes = new List<IPrimitive>();

        // Creating the bvh
        bvh = new BVHNode(primitives, 0f, 1f, ref bvhNodes); // TODO : check why are both parameters 0 and 1

        // Add the root at the end as well
        bvhNodes.Add(bvh);
    }
    
    void DilateTextureOnGPU(RenderTexture rt)
    {

    }

    void BlurTextureOnGPU(RenderTexture rt)
    {

    }

    /// <summary>
    /// Save a Texture2D to file
    /// </summary>
    /// <param name="t2D"></param>
    /// <param name="textureName"></param>
    void SaveTexture2DToFile(Texture2D t2D, string textureName)
    {
        //--------------------------------------------------
        if (blurTexture)
        {
            t2D = BlurTexture1(t2D, blurSamples);
        }

        // Step 4: Get the bytes from the final AO map
        var bytes = t2D.EncodeToPNG();

        // Step 5: Save the final AO map tp file
        File.WriteAllBytes(Application.dataPath + "/" + textureName, bytes);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Convert RenderTexture to Textire2D and save it to file
    /// </summary>
    /// <param name="rT"></param>
    /// <param name="textureName"></param>
    void SaveRenderTextureToFile(ObjectSelected so, RenderTexture rT, string textureName)
    {
        
        Texture2D AOMap = RenderTextureToTexture2D(rT);
        var bytes = AOMap.EncodeToPNG();
        File.WriteAllBytes("Assets" + polmData.tDir + "/" + textureName, bytes);

        AssetDatabase.Refresh();

        // Load the texture into the occlusion slot of the material
        //Texture2D tex = AssetDatabase.LoadAssetAtPath("Assets/" + textureName, typeof(Texture2D)) as Texture2D;
        Texture2D tex = AssetDatabase.LoadAssetAtPath("Assets" + polmData.tDir + "/" + textureName, typeof(Texture2D)) as Texture2D;

        foreach (Material m in so.mats)
        {
            // _OcclusionMap
            // _DetailAlbedoMap

            if (shaderSlot == 1)
            {
                if (m.HasProperty("_OcclusionMap")) m.SetTexture("_OcclusionMap", tex);
                if (m.HasProperty("_DetailAlbedoMap")) m.SetTexture("_DetailAlbedoMap", null);
            }
            else if (shaderSlot == 2)
            {
                if (m.HasProperty("_OcclusionMap")) m.SetTexture("_OcclusionMap", null);
                if (m.HasProperty("_DetailAlbedoMap")) m.SetTexture("_DetailAlbedoMap", tex);
                if (m.HasProperty("_UVSec"))
                {
                    if (so.uvSet == UVSet.UV)
                    {
                        m.SetFloat("_UVSec", 0f);
                    }
                    else if (so.uvSet == UVSet.UV2)
                    {
                        m.SetFloat("_UVSec", 1f);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Merge and Save diffuse and ambient occlusion textures
    /// </summary>
    /// <param name="dif"></param>
    /// <param name="ao"></param>
    void MergeAndSaveDifAOTextures(ObjectSelected so, Texture2D ao)
    {
        Texture2D dif = so.rend.sharedMaterial.mainTexture as Texture2D;
        Color c1, c2;
        Texture2D finalTexture = new Texture2D(textureSize, textureSize);
        for (int i = 0; i < textureSize; i++)
        {
            for (int j = 0; j < textureSize; j++)
            {
                c1 = dif.GetPixel(i, j); 
                c2 = ao.GetPixel(i, j);

                //Blend = (src.rgb * src.a) + (dest.rgb * (1 - src.a))
                //Add = (src.rgb * src.a) + (dest.rgb)
                //Multiply = (src.rgb * dest.rgb * src.a) + (dest.rgb * (1 - src.a))

                //Color fc = (c1 * c2 * c1.a) + (c2 * (1f - c1.a));
                //Color fc = new Color((c1.r * c2.r) / 1f, (c1.g * c2.g) / 1f, (c1.b * c2.b) / 1f, c1.a);

                Color fc = new Color( Mathf.Min(c1.r + c2.r, 1f), Mathf.Min(c1.g + c2.g, 1f), Mathf.Min(c1.b + c2.b, 1f), c1.a);

                finalTexture.SetPixel(i, j, fc);
            }
        }
        SaveTexture2DToFile(finalTexture, "finalTexture.png");
    }

    /// <summary>
    /// Convert RenderTexture to Texture2D
    /// </summary>
    /// <param name="rTexture"></param>
    /// <returns></returns>
    Texture2D RenderTextureToTexture2D(RenderTexture rTexture)
    {
        RenderTexture.active = rTexture;
        Texture2D t2D = new Texture2D(textureSize, textureSize);
        t2D.ReadPixels(new Rect(0, 0, rTexture.width, rTexture.height), 0, 0, false);
        t2D.Apply();
        return t2D;
    }

    /// <summary>
    /// Convert Texture2D to RenderTexture. Not Implemented!
    /// </summary>
    /// <param name="exture2D"></param>
    /// <returns></returns>
    RenderTexture Texture2DToRenderTexture(Texture2D exture2D)
    {
        Debug.Log("Not Implemented");
        RenderTexture rndT = new RenderTexture(textureSize, textureSize, 32);
        rndT.Create();
        return rndT;
    }

    /// <summary>
    /// Calculate Edge Lumels
    /// </summary>
    void CalcEdgeLumels(ObjectSelected so)
    {
        List<Lumel> edgeLumels = new List<Lumel>(new Lumel[so.lumels.Count]);
        // Make a copy and move it up with one pixel
        for (int x = 1; x < textureSize - 1; x++)
        {
            for (int y = 1; y < textureSize - 1; y++)
            {
                int lumelID = y * textureSize + x;

                if (so.lumels[lumelID].valid == 1)
                {
                    Lumel l = new Lumel();
                    if (so.lumels[(y - 1) * textureSize + x].valid == 0)
                    {
                        l = so.lumels[(y - 1) * textureSize + x];
                        l.valid = 2;
                        edgeLumels[(y - 1) * textureSize + x] = l;
                    }
                    if (so.lumels[(y + 1) * textureSize + x].valid == 0)
                    {
                        l = so.lumels[(y + 1) * textureSize + x];
                        l.valid = 2;
                        edgeLumels[(y + 1) * textureSize + x] = l;
                    }
                    if (so.lumels[y * textureSize + (x - 1)].valid == 0)
                    {
                        l = so.lumels[y * textureSize + (x - 1)];
                        l.valid = 2;
                        edgeLumels[y * textureSize + (x - 1)] = l;
                    }
                    if (so.lumels[y * textureSize + (x + 1)].valid == 0)
                    {
                        l = so.lumels[y * textureSize + (x + 1)];
                        l.valid = 2;
                        edgeLumels[y * textureSize + (x + 1)] = l;
                    }
                }
            }
        }
        for (int i = 0; i < edgeLumels.Count; i++)
        {
            if (edgeLumels[i].valid == 2)
            {
                so.lumels[i] = edgeLumels[i];
            }
        }

        //Texture2D objectUvs = new Texture2D(textureSize, textureSize);
        //for (int j = 0; j < textureSize; j++)
        //{
        //    for (int i = 0; i < textureSize; i++)
        //    {
        //        if (lumels[j * textureSize + i].valid == 2)
        //        {
        //            objectUvs.SetPixel(i, j, Color.red);
        //        }
        //        //else objectUvs.SetPixel(i, j, Color.black);
        //    }
        //}
        //SaveTexture2DToFile(objectUvs, "testUVs.png");
    }

    /// <summary>
    /// Dilate a texture
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    Texture2D DilateTexture(Texture2D texture)
    {
        // ================================= Working single sample version
        Texture2D originalTexture = texture;
        Texture2D newTexture = new Texture2D(textureSize, textureSize);

        for (int i = 1; i < textureSize - 1; i++)
        {
            for (int j = 1; j < textureSize - 1; j++)
            {
                newTexture.SetPixel(i, j, new Color(0f, 0f, 0f, 0f));
            }
        }

        // Make a copy and move it up with one pixel
        for (int i = 1; i < textureSize - 1; i++)
        {
            for (int j = 1; j < textureSize - 1; j++)
            {
                //newTexture.SetPixel(i, j, Color.black);
                if (originalTexture.GetPixel(i, j).a == 1f)
                {
                    if (originalTexture.GetPixel(i, j - 1).a == 0f)
                    {
                        newTexture.SetPixel(i, j - 1, originalTexture.GetPixel(i, j));
                    }
                    if (originalTexture.GetPixel(i, j + 1).a == 0f)
                    {
                        newTexture.SetPixel(i, j + 1, originalTexture.GetPixel(i, j));
                    }
                    if (originalTexture.GetPixel(i - 1, j).a == 0f)
                    {
                        newTexture.SetPixel(i - 1, j, originalTexture.GetPixel(i, j));
                    }
                    if (originalTexture.GetPixel(i + 1, j).a == 0f)
                    {
                        newTexture.SetPixel(i + 1, j, originalTexture.GetPixel(i, j));
                    }
                }
            }
        }

        for (int i = 1; i < textureSize - 1; i++)
        {
            for (int j = 1; j < textureSize - 1; j++)
            {
                if (originalTexture.GetPixel(i, j).a == 1f)
                {
                    newTexture.SetPixel(i, j, texture.GetPixel(i, j));
                }
            }
        }

        return newTexture;
        // ================================= Working single sample version
    }

    /// <summary>
    /// Blur Texture
    /// </summary>
    /// <param name="lightmap"></param>
    /// <param name="smoothing"></param>
    /// <returns></returns>
    Texture2D BlurTexture1(Texture2D lightmap, int smoothing)
    {
        Color[] pixels = lightmap.GetPixels(0);
        Color[] newPixels = new Color[pixels.Length];

        for (int width = 0; width < lightmap.width; width++)
        {
            for (int height = 0; height < lightmap.height; height++)
            {
                int pixelsBlended = 0;
                Color blendedColor = new Color(0, 0, 0, 1);

                for (int offsetW = -smoothing; offsetW <= smoothing; offsetW++)
                {
                    for (int offsetH = -smoothing; offsetH <= smoothing; offsetH++)
                    {
                        //If inside the texture
                        if (width + offsetW >= 0 && width + offsetW < lightmap.width &&
                            height + offsetH >= 0 && height + offsetH < lightmap.height)
                        {

                            Color pixelColor = pixels[(height + offsetH) * lightmap.width + (width + offsetW)];
                            if (pixelColor.r > 0 || pixelColor.g > 0 || pixelColor.b > 0)
                            { //Ignore texels that are black (this all but kill edge artifacts)
                                blendedColor += pixelColor;
                                pixelsBlended++;
                            }
                        }
                    }
                }

                newPixels[height * lightmap.width + width] = (blendedColor / (pixelsBlended > 0 ? pixelsBlended : 1)) + Color.black;
            }
        }

        lightmap.SetPixels(newPixels, 0);
        return lightmap;
    }

    /// <summary>
    /// TODO find a way to implement better blur funcion - i.e. Gaussian Blur
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    Texture2D BlurTexture2(Texture2D texture)
    {
        Texture2D originalTexture = texture;
        Color[] cols = new Color[9];
        Texture2D filteredTexture = new Texture2D(textureSize, textureSize);
        for (int i = 1; i < textureSize - 1; i++)
        {
            for (int j = 1; j < textureSize - 1; j++)
            {
                cols[0] = originalTexture.GetPixel(i - 1, j - 1);
                cols[1] = originalTexture.GetPixel(i, j - 1);
                cols[2] = originalTexture.GetPixel(i + 1, j - 1);

                cols[3] = originalTexture.GetPixel(i - 1, j);
                cols[4] = originalTexture.GetPixel(i + 1, j);

                cols[5] = originalTexture.GetPixel(i - 1, j + 1);
                cols[6] = originalTexture.GetPixel(i, j + 1);
                cols[7] = originalTexture.GetPixel(i + 1, j + 1);
                cols[8] = originalTexture.GetPixel(i, j);

                Color finalColor = new Color();
                int validColors = 0;
                foreach (Color c in cols)
                {
                    if (c.a == 1f)
                    {
                        finalColor += c;
                        validColors++;
                    }
                }
                finalColor = finalColor / validColors;
                filteredTexture.SetPixel(i, j, finalColor);
            }
        }
        filteredTexture.Apply();
        return filteredTexture;
    }

    /// <summary>
    /// Is a point inside of triangle
    /// </summary>
    /// <param name="p"></param>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    bool IsPointInTriangle(Vector3 p, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 b = GetBarycentric(p0, p1, p2, p);

        if (b.x < 0f) return false;
        if (b.y < 0f) return false;
        if (b.z < 0f) return false;
        return true;        
    }

    /// <summary>
    /// Is point in a rectangle
    /// </summary>
    /// <param name="p"></param>
    /// <param name="quad"></param>
    /// <returns></returns>
    bool IsPointInRectangle(Vector3 p, Vector3[] quad)
    {
        Vector3 edge1 = quad[1] - quad[0];
        Vector3 edge2 = quad[2] - quad[1];
        Vector3 edge3 = quad[3] - quad[2];
        Vector3 edge4 = quad[0] - quad[3];

        if (Vector3.Cross(edge1, p - quad[0]).z <= 0f &&
            Vector3.Cross(edge2, p - quad[1]).z <= 0f &&
            Vector3.Cross(edge3, p - quad[2]).z <= 0f &&
            Vector3.Cross(edge4, p - quad[3]).z <= 0f)
            return true;

        return false;
    }

    /// <summary>
    /// Do triangle and reactangle overlap
    /// NOT YET IMPLEMENTED
    /// </summary>
    /// <param name="p"></param>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    bool IsTriangleInRectangle2D(Vector3 p, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 edge1 = p1 - p0;
        Vector3 edge2 = p2 - p1;
        Vector3 edge3 = p0 - p2;

        if (Vector3.Cross(edge1, p - p0).z <= 0f &&
            Vector3.Cross(edge2, p - p1).z <= 0f &&
            Vector3.Cross(edge3, p - p2).z < 0f)
            return true;

        return false;
    }

    /// <summary>
    /// Get barycentric coordinates
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    Vector3 GetBarycentric(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 p)
    {
        Vector3 B = new Vector3();
        B.x =   ((v2.y - v3.y) * (p.x - v3.x)  + (v3.x - v2.x) * (p.y - v3.y)) /
                ((v2.y - v3.y) * (v1.x - v3.x) + (v3.x - v2.x) * (v1.y - v3.y));
        B.y =   ((v3.y - v1.y) * (p.x - v3.x)  + (v1.x - v3.x) * (p.y - v3.y)) /
                ((v3.y - v1.y) * (v2.x - v3.x) + (v1.x - v3.x) * (v2.y - v3.y));
        B.z = 1f - B.x - B.y;
        return B;
    }

    /// <summary>
    /// Creates uniformely distributed points on sphere - not wokring
    /// </summary>
    /// <param name="N"></param>
    /// <returns></returns>
    Vector3[] UniformPointsOnSphere(float N)
    {
        var points = new List<Vector3>();
        var i = Mathf.PI * (3 - Mathf.Sqrt(Random.Range(-N, N)));
        var o = 2 / N;
        for (var k = 0; k < N; k++)
        {
            var y = k * o - 1 + (o / 2);
            var r = Mathf.Sqrt(1 - y * y);
            var phi = k * i;
            points.Add(new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r));
        }
        return points.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Vector3 UniformPointOnSphere()
    {
        System.Random rand = new System.Random();

        double v0 = rand.NextDouble();
        double v1 = rand.NextDouble();
        double v2 = rand.NextDouble();
        return new Vector3((float)v0, (float)v1, (float)v2);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourcePoint"></param>
    /// <param name="tr"></param>
    /// <returns></returns>
    public static Vector3 ClosestPointToTriangle(Vector3 sourcePoint, Triangle tr)
    {
        Vector3 edge0 = tr.v1 - tr.v0;
        Vector3 edge1 = tr.v2 - tr.v0;
        Vector3 v0 = tr.v0 - sourcePoint;

        float a = Vector3.Dot(edge0, edge0);
        float b = Vector3.Dot(edge0, edge1);
        float c = Vector3.Dot(edge1, edge1);
        float d = Vector3.Dot(edge0, v0);
        float e = Vector3.Dot(edge1, v0);

        float det = a * c - b * b;
        float s = b * e - c * d;
        float t = b * d - a * e;

        if (s + t < det)
        {
            if (s < 0f)
            {
                if (t < 0f)
                {
                    if (d < 0f)
                    {
                        s = Mathf.Clamp(-d / a, 0f, 1f);
                        t = 0f;
                    }
                    else
                    {
                        s = 0f;
                        t = Mathf.Clamp(-e / c, 0f, 1f);
                    }
                }
                else
                {
                    s = 0f;
                    t = Mathf.Clamp(-e / c, 0f, 1f);
                }
            }
            else if (t < 0f)
            {
                s = Mathf.Clamp(-d / a, 0f, 1f);
                t = 0f;
            }
            else
            {
                float invDet = 1f / det;
                s *= invDet;
                t *= invDet;
            }
        }
        else
        {
            if (s < 0f)
            {
                float tmp0 = b + d;
                float tmp1 = c + e;
                if (tmp1 > tmp0)
                {
                    float numer = tmp1 - tmp0;
                    float denom = a - 2f * b + c;
                    s = Mathf.Clamp(numer / denom, 0f, 1f);
                    t = 1f - s;
                }
                else
                {
                    t = Mathf.Clamp(-e / c, 0f, 1f);
                    s = 0f;
                }
            }
            else if (t < 0f)
            {
                if (a + d > b + e)
                {
                    float numer = c + e - b - d;
                    float denom = a - 2f * b + c;
                    s = Mathf.Clamp(numer / denom, 0f, 1f);
                    t = 1f - s;
                }
                else
                {
                    s = Mathf.Clamp(-e / c, 0f, 1f);
                    t = 0f;
                }
            }
            else
            {
                float numer = c + e - b - d;
                float denom = a - 2f * b + c;
                s = Mathf.Clamp(numer / denom, 0f, 1f);
                t = 1f - s;
            }
        }
        Vector3 closestPos = (tr.v0 + (edge0 * s) + (edge1 * t));
        return closestPos;
    }

    // ----------------------------------------------------------------------------------------------------------------------------------
    // CUSTOM TYPES
    // ----------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// used for creating 2D acceleration structures and calculating lumels
    /// </summary>
    public class Triangle2D
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;

        public Triangle2D(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            if (Vector3.Cross(v1 - v0, v2 - v0).z < 0f)
            {
                // Need to flip the uv triangle
                this.v0 = v0;
                this.v1 = v2;
                this.v2 = v1;
            }
            else
            {
                this.v0 = v0;
                this.v1 = v1;
                this.v2 = v2;
            }
        }

        public bool IsInRectangle()
        {
            return false;
        }
    }

    /// <summary>
    /// Triangle structure used to upload data to compute shader buffers
    /// </summary>
    public struct Triangle
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
        public Vector3 n;
    }

    /// <summary>
    /// The is a struct that holds information
    /// for a point in space which is mapped to the pixel of a texutre
    /// in regards to the uvs
    /// </summary>
    public struct Lumel
    {
        public Vector3 position;
        public Vector3 n;
        public float valid;
        public float lumelColor;

        public Lumel(Vector3 _position,
                     Vector3 _normal,
                     float _valid,
                     float _color)
        {
            position = _position;
            n = _normal;
            valid = _valid;
            lumelColor = _color;
        }
    }

    /// <summary>
    /// Used by the KD-Tree Acceleration Structure
    /// </summary>
    public struct Node
    {
        public Vector3 bMin;        // the bound min point
        public Vector3 bMax;        // the bound max point
        public uint nodeType;       // 0 = root, 1 = child, 2 = leaf
        //public uint nodeSide;     // is the node left or right child
        public uint parentID;       // current node's parent id in the buffer
        public uint nodeID;         // current node id in the buffer
        public uint leftNodeID;     // the current node right child id in the buffer
        public uint rightNodeID;    // the current node left child id in the buffer
        public uint triangleStart;  // where in the buffer we should start testing triangles from
        public uint trianglesCount; // how many triangle from the buffer we have to test
        public short ropeLeft, ropeFront, ropeRight, ropeBack, ropeTop, ropeBottom;

        //public uint tested;
        //public uint temp;
    };

    /// <summary>
    /// 
    /// </summary>
    public struct LBVHTriangle
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
        public uint trID;

        public Vector3 empty1;
        public Vector3 empty2;

        public LBVHTriangle(Vector3 _v0, Vector3 _v1, Vector3 _v2, uint _trID)
        {
            v0 = _v0;
            v1 = _v1;
            v2 = _v2;
            trID = _trID;
            empty1 = Vector3.zero;
            empty2 = Vector3.zero;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public struct LBVHNODE  // 37 bytes	- TODO: find a way to make it either 32 or 64 bytes
    {
        // Bounds
        public Vector3 bMin;    // 12 bytes
        public Vector3 bMax;    // 12 bytes

        // Children
        public uint   nodeID;   // 4 bytes
        public uint LChildID;   // 4 bytes
        public uint RChildID;   // 4 bytes
        public uint isLeaf;     // 4 byte

        public Vector3 tempty1; // 12 bytes
        public Vector3 tempty2; // 12 bytes

        public LBVHNODE(Vector3 _bMin, Vector3 _bMax, uint _LChildID, uint _RChildID, uint _nodeID, uint _isLeaf)
        {
            bMin = _bMin;
            bMax = _bMax;
            nodeID = _nodeID;
            LChildID = _LChildID;
            RChildID = _RChildID;
            isLeaf = _isLeaf;
            tempty1 = new Vector3(0f, 0f, 0f);
            tempty2 = new Vector3(0f, 0f, 0f);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    public enum UVSet { UV, UV2, UV3, UV4 }

    /// <summary>
    /// 
    /// </summary>
    public enum CurrentPlatform { DX, GL, GLES, NONE }

    // ----------------------------------------------------------------------------------------------------------------------------------
    // DEBUGGING
    // ----------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Creates a sphere in space, to visualize debug information
    /// </summary>
    /// <param name="pos"></param>
    public static void CreateSphere(Vector3 pos)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        DestroyImmediate(g.GetComponent<Collider>());
        g.transform.localScale *= 0.0025f;
        g.transform.position = pos;
    }
    /// <summary>
    /// Creates a sphere in space, to visualize debug information
    /// </summary>
    /// <param name="pos"></param>
    public static void CreateSphere(Vector3 pos, float raidus)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        DestroyImmediate(g.GetComponent<Collider>());
        g.transform.localScale *= raidus;
        g.transform.position = pos;
    }
    /// <summary>
    /// Creates a sphere in space, to visualize debug information
    /// </summary>
    /// <param name="pos"></param>
    public static void CreateSphere(Vector3 pos, float raidus, string name)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = name;
        DestroyImmediate(g.GetComponent<Collider>());
        g.transform.localScale *= raidus;
        g.transform.position = pos;
    }
    /// <summary>
    /// Creates a AABB transparent object in space, to visualize debug information
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="bboxName"></param>
    static void CreateBBoxGizmo(Vector3 min, Vector3 max, string bboxName)
    {
        Debug.Log("creating gizmo");
        Vector3 p0 = min;
        Vector3 p1 = new Vector3(max.x, min.y, min.z);
        Vector3 p2 = new Vector3(max.x, min.y, max.z);
        Vector3 p3 = new Vector3(min.x, min.y, max.z);
        Vector3 p4 = new Vector3(min.x, max.y, min.z);
        Vector3 p5 = new Vector3(max.x, max.y, min.z);
        Vector3 p6 = max;
        Vector3 p7 = new Vector3(min.x, max.y, max.z);
        Vector3[] verts = new Vector3[] { p0, p1, p2, p3, p4, p5, p6, p7 };
        int[] tris = new int[] { 0, 1, 2, 2, 3, 0, 0, 3, 7, 7, 4, 0, 4, 7, 5, 5, 7, 6, 5, 6, 2, 2, 1, 5, 0, 4, 5, 5, 1, 0, 6, 7, 3, 3, 2, 6 };
        Mesh newMesh = new Mesh();
        newMesh.Clear();
        newMesh.vertices = verts;
        newMesh.triangles = tris;

        Vector2[] newUV = new Vector2[newMesh.vertices.Length];
        for (int v = 0; v < newMesh.uv.Length; v++)
        {
            newMesh.uv[v] = new Vector2((float)Random.value, (float)Random.value);
        }
        for (int v = 0; v < newMesh.uv.Length; v++)
        {
            newMesh.uv[v] = new Vector2((float)Random.value, (float)Random.value);
        }
        newMesh.uv = newUV;
        GameObject newBox = new GameObject();
        newBox.name = "bbox_depth_" + bboxName;
        newBox.AddComponent<MeshFilter>().mesh = newMesh;
        newBox.AddComponent<MeshRenderer>();
        Material newMat = new Material(Shader.Find("Transparent/Diffuse"));
        newMat.color = new Color(Random.value, Random.value, Random.value, 0.15f);
        newBox.GetComponent<MeshRenderer>().material = newMat;
        //newBox.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="verts"></param>
    /// <param name="tris"></param>
    void CreateMesh(Vector3[] verts, int[] tris)
    {
        Mesh newMesh = new Mesh();
        newMesh.Clear();
        newMesh.vertices = verts;
        newMesh.triangles = tris;
        GameObject g = new GameObject();
        g.AddComponent<MeshFilter>().mesh = newMesh;
        g.AddComponent<MeshRenderer>();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="verts"></param>
    /// <param name="tris"></param>
    void VisualizeMesh(Mesh m)
    {
        GameObject g = new GameObject();
        g.AddComponent<MeshFilter>().mesh = m;
        g.AddComponent<MeshRenderer>();
        g.transform.Rotate(Vector3.up * 180f);
    }
}

/// <summary>
/// Rasterization class used to build the lumels
/// </summary>
public static class Rasterizer
{
    /// <summary>
    /// Build lumels
    /// </summary>
    /// <param name="so"></param>
    public static void BuildLumels(ObjectSelected so, bool interpolateNormals)
    {
        int res = so.rendTex.width;                
        so.lumels = new List<POLM.Lumel>(new POLM.Lumel[res * res]);
        bool hasNormals = false;
        if (so.normals != null)
            hasNormals = so.normals.Length == so.vertices.Length;

        // ------------------------------------------------------------------------------------------------------------------------------------------------------
        // Foreach UV triangle
        // ------------------------------------------------------------------------------------------------------------------------------------------------------

        for (int i = 0; i < so.triangles.Length; i += 3)
        {
            Vector3 u0 = so.uvs[so.triangles[i + 0]] * res;
            Vector3 u1 = so.uvs[so.triangles[i + 1]] * res;
            Vector3 u2 = so.uvs[so.triangles[i + 2]] * res;

            // ------------------------------------------------------------------------------------------------------------------------------------------------------
            // Calculate Triangle BBox
            // ------------------------------------------------------------------------------------------------------------------------------------------------------

            // TODO : optimize memory allocation               

            float minX_f = Mathf.Min(u0.x, Mathf.Min(u1.x, u2.x));
            float maxX_f = Mathf.Max(u0.x, Mathf.Max(u1.x, u2.x));
            float minY_f = Mathf.Min(u0.y, Mathf.Min(u1.y, u2.y));
            float maxY_f = Mathf.Max(u0.y, Mathf.Max(u1.y, u2.y));

            int minX = Mathf.FloorToInt(minX_f);
            int maxX = Mathf.CeilToInt(maxX_f);
            int minY = Mathf.FloorToInt(minY_f);
            int maxY = Mathf.CeilToInt(maxY_f);

            // ------------------------------------------------------------------------------------------------------------------------------------------------------
            // Drawing Rasterized Pixels
            // ------------------------------------------------------------------------------------------------------------------------------------------------------

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    int lumelID = y * res + x;

                    Vector3 pixelCenter = new Vector3(x + 0.5f, y + 0.5f, 0f);
                    if (RectInTriangle(pixelCenter, u0, u1, u2))
                    {
                        Vector3 p0 = so.rend.transform.TransformPoint(so.vertices[so.triangles[i + 0]]);
                        Vector3 p1 = so.rend.transform.TransformPoint(so.vertices[so.triangles[i + 1]]);
                        Vector3 p2 = so.rend.transform.TransformPoint(so.vertices[so.triangles[i + 2]]);

                        Vector3 bary = GetBarycentric(u0, u1, u2, pixelCenter);
                        Vector3 localP = bary.x * p0 + bary.y * p1 + bary.z * p2;

                        Vector3 normal = Vector3.zero;
                        if (!hasNormals || !interpolateNormals)
                        {
                            normal = (Vector3.Cross((p1 - p0).normalized, (p2 - p0).normalized)).normalized;
                        }
                        else
                        {
                            Vector3 n0 = so.normals[so.triangles[i + 0]];
                            Vector3 n1 = so.normals[so.triangles[i + 1]];
                            Vector3 n2 = so.normals[so.triangles[i + 2]];
                            float a, b, c;
                            a = bary.x;
                            b = bary.y;
                            c = bary.z;
                            localP = a * p0 + b * p1 + c * p2;
                            normal = a * n0 + b * n1 + c * n2;
                            normal.Normalize();
                        }

                        so.lumels[lumelID] = new POLM.Lumel(localP, normal, 1f, 1f);

                    }
                }
            }            
        }        
    }
    public static float sign(Vector3 p, Vector3 v1, Vector3 v2)
    {
        return (p.x - v2.x) * (v1.y - v2.y) - (v1.x - v2.x) * (p.y - v2.y);
    }

    public static Vector3 GetBarycentric(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 p)
    {
        Vector3 B = new Vector3();
        B.x = ((v2.y - v3.y) * (p.x - v3.x) + (v3.x - v2.x) * (p.y - v3.y)) /
            ((v2.y - v3.y) * (v1.x - v3.x) + (v3.x - v2.x) * (v1.y - v3.y));
        B.y = ((v3.y - v1.y) * (p.x - v3.x) + (v1.x - v3.x) * (p.y - v3.y)) /
            ((v3.y - v1.y) * (v2.x - v3.x) + (v1.x - v3.x) * (v2.y - v3.y));
        B.z = 1f - B.x - B.y;
        return B;
    }

    public static bool RectInTriangle(Vector3 p, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        bool b1 = sign(p, v0, v1) < 0.0f;
        bool b2 = sign(p, v1, v2) < 0.0f;
        bool b3 = sign(p, v2, v0) < 0.0f;

        if ((b1 == b2) && (b2 == b3))
        {
            return true;
        }
        return false;
    }
}