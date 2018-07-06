
//	VertexDirt plug-in for Unity
//	Copyright 2014-2017, Zoltan Farago, All rights reserved.
using UnityEngine;	
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif 		
public class VertexDirtSettingsStruct
{
	// the shader used on occluders. If not exist or empty, objects keeps their shaders during bake
	public string occluderShader;
	//	clip planes of the sampling camera.
	public float samplingBias = 0.001f;
	public float samplingDistance = 10.0f;
	//	The FOV of the sampling camera. Please note that this value normally should be between 100-160.
	public float samplingAngle = 135.0f;
	//	Enable to smoothing out hard edges. Basically just averages the normals of the vertices in the same position.
	public bool edgeSmooth = false;
	//	Set true if you want to render the inside of the objects. set true to render thickness.
	public bool invertNormals = false;
	//	The range of edge smoothing. The normals of vertices closer than this value will be averaged.
	public float edgeSmoothBias = 0.01f;
	//	sampling camera backdrop. 
	public CameraClearFlags skyMode = CameraClearFlags.SolidColor;
	//	bake the background colour/cubeMap only
	public bool disableOccluders = false;
	//	The colour of the Sky.
	public Color customSkyColor = new Color32(255,250,230,255);
	//	Colour tint for the occluders. This property is designed for the VDOccluder shader.
	public Color customShadowColor = new Color32(150,155,160,255);
	//	enable custom sky color
	public bool useCustomSkyColor = true;
	//	enable custom shadow color
	public bool useCustomShadowColor = true;
	//	enable custom shadow color
	public bool useSkyCube = false;	
	//	The cubeMap of the sampling camera's sky.
	public Material skyCube;
	// blendmode index
	public int blendModeIndex = 0;	
	// outputs
	public int colorOutputIndex = 0;
	public int luminanceOutputIndex = 4;
	//bakeLayerMask
	public int bakeLayerMask = 0;
}

//	Main Vertex dirt class. VertexDirt is an Ambient Occlusion baking plug-in.
public static class VertexDirt
{
	// private variables for mesh merging and vertex sampling
	private static Vector3[] v;
	private static Vector3[] n;
	private static Color32[] c;
	 
	//	public variable, but this is used by the baking.
	public static VertexSample vertexSample = new VertexSample();
	//	resolution of the sample. 16 is fine and fast. 64 gives better quality but slower.
	public static int sampleWidth = 64;
	public static int sampleHeight = 64;
	// not implemented yet: vertexdirt baking mode: 0-Nothing, 1-AmbOcc, 2-ClearWithSky, 3-Thickness
	public static int rgbBakingMode = 0;
	public static int alphaBakingMode = 0;
	//
	public static VertexDirtSettingsStruct settings = new VertexDirtSettingsStruct();
	
	//	Main function for vertex baking. The Object[] array will be used.
    public static void Dirt(Transform[] sels) 
	{
		int pnum = 0;
		#if UNITY_EDITOR
		double tempTime = EditorApplication.timeSinceStartup;
		EditorUtility.DisplayProgressBar("VertexDirt baking", "Preparing" , 0.0f);
		#endif
 		if (sels != null && sels.Length > 0) 
		{
			//	vertex camera
			GameObject camGO = new GameObject("VDSamplerCamera"); 
			Camera cam = camGO.AddComponent(typeof(Camera)) as Camera;
			RenderTexture ren = new RenderTexture(sampleWidth, sampleHeight, 16, RenderTextureFormat.ARGB32);
			camGO.AddComponent(typeof(VDSampler));
			cam.targetTexture = ren;
			#if Unity_4_0 || Unity_4_1 || Unity_4_2 || Unity_4_3 || Unity_4_4 || Unity_4_5 || Unity_4_6 || Unity_4_7
			if (!Application.HasProLicense) { cam.targetTexture = null;}
			#endif
			cam.renderingPath = RenderingPath.Forward;
			cam.pixelRect = new Rect(0,0,sampleWidth, sampleHeight);
			cam.aspect = 1.0f;	
			cam.nearClipPlane = settings.samplingBias;
			cam.farClipPlane = settings.samplingDistance;
			cam.fieldOfView = Mathf.Clamp ( settings.samplingAngle, 5, 160 );
			cam.clearFlags = settings.skyMode;
			cam.backgroundColor = settings.useCustomSkyColor ? settings.customSkyColor : Color.white;
			//cam.cullingMask = settings.bakeLayerMask;
			cam.enabled = false;
			Material tempSkybox = RenderSettings.skybox;
			if (settings.skyMode == CameraClearFlags.Skybox) { RenderSettings.skybox = settings.skyCube; }
			UpdateShaderVariables();
			cam.SetReplacementShader(Shader.Find(settings.occluderShader), settings.disableOccluders ? "ibl-only" : "");
			
			for (int t = 0; t<sels.Length; t++) 
			{
				if (sels[t].gameObject.GetComponent<MeshFilter>()) 
				{
					PrepareVertices(sels[t]);
					PrepareColors();
					SmoothVertices();
					CalcColors(camGO, cam);
					ApplyColors(sels[t]);
					pnum += v.Length;
					#if UNITY_EDITOR
					if (EditorUtility.DisplayCancelableProgressBar("VertexDirt baking", "Processing "+(t+1)+" of "+sels.Length+" objects ("+pnum+" vertices processed)" , 1.0f*(t+1)*(1.0f/sels.Length))) 
					{
						break;
					}
					#endif
				}
			}
			RenderSettings.skybox = tempSkybox;
			cam.targetTexture = null;
			GameObject.DestroyImmediate(ren);
			GameObject.DestroyImmediate(camGO);
			VDColorHandlerBase[] handlers = UnityEngine.Object.FindObjectsOfType<VDColorHandlerBase>();

			for (int i = 0; i<handlers.Length; i++) 
			{
				if (handlers[i].originalMesh && !handlers[i].coloredMesh) 
				{
					handlers[i].coloredMesh = UnityEngine.Object.Instantiate(handlers[i].originalMesh);
					handlers[i].gameObject.GetComponent<MeshFilter>().mesh = handlers[i].coloredMesh;
				}
			}
		}
		#if UNITY_EDITOR
		EditorUtility.ClearProgressBar();
		//EditorUtility.DisplayDialog("VertexDirt baking", "Baking of "+sels.Length+" objects ("+pnum+" vertices)\nis finished in "+Mathf.RoundToInt(EditorApplication.timeSinceStartup - tempTime)+" seconds.", "OK");
		Debug.Log ("Baking of "+sels.Length+" objects ("+pnum+" vertices) finished in "+Mathf.RoundToInt((float)EditorApplication.timeSinceStartup - (float)tempTime)+" seconds.");
		#endif
    }

	//	function to update shader properties
	public static void UpdateShaderVariables() 
	{
		Shader.SetGlobalColor("_VDSkyColor", settings.useCustomSkyColor ? settings.customSkyColor : Color.black);
		Shader.SetGlobalColor("_VDOccluderColor", settings.useCustomShadowColor ? settings.customShadowColor : Color.black);
		Shader.SetGlobalFloat("_VDsamplingDistance", settings.samplingDistance);
	}

	private static void PrepareVertices(Transform go) 
	{
		int vertexCount = 0;
		v = new Vector3[0];
		n = new Vector3[0];
		c = new Color32[0];
		
		if (!go.gameObject.GetComponent<VDColorHandlerBase>()) 
		{
			go.gameObject.AddComponent(typeof(VDColorHandler));
		}
		if (go.gameObject.GetComponent<MeshFilter>() != null && go.gameObject.GetComponent<MeshFilter>().sharedMesh != null) {
			
			v = go.gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;
			n = go.gameObject.GetComponent<MeshFilter>().sharedMesh.normals;

			for (int t = 0; t < v.Length; t++) 
			{
				v[t] = go.TransformPoint(v[t]);
				n[t] = Vector3.Normalize(go.TransformDirection(n[t]));
			}
			vertexCount += v.Length;
		}
	}
	
	public static void PrepareColors() 
	{
		c = new Color32[v.Length];			
	}

	private static void SmoothVertices() 
	{
		if (settings.edgeSmooth) 
		{
			for (int a = 0; a < v.Length; a++) 
			{
				List<int> tempV = new List<int>();
				tempV.Add(a);
				for (int d = a; d < v.Length; d++) 
				{
					if (Vector3.Distance(v[a],v[d]) < settings.edgeSmoothBias) 
					{
						tempV.Add(d);
					}
				}
				Vector3 tempSumN = Vector3.zero;
				for (int dd = 0; dd<tempV.Count; dd++)
				{
					tempSumN += n[tempV[dd]];
				}
				tempSumN /= (float)tempV.Count*1.0f;
				for (int nn = 0; nn<tempV.Count; nn++)
				{
					n[tempV[nn]] = tempSumN;
				}
			}
			for (int k = 0; k <c.Length; k++) 
			{
				c[k] = new Color32 (255,255,255,255);
			}
		}
	}
	
	private static void CalcColors(GameObject camGO, Camera cam) 
	{	
		for (int vv = 0; vv<v.Length; vv++) 
		{
			if (settings.invertNormals) 
			{
				camGO.transform.position = v[vv]-n[vv]*settings.samplingBias;
				camGO.transform.LookAt(v[vv] - n[vv]);
			}
			else 
			{
				camGO.transform.position = v[vv]+n[vv]*settings.samplingBias;
				camGO.transform.LookAt(v[vv] + n[vv]);
			}
			vertexSample.index = vv;
			vertexSample.isCalulated = false;
			cam.Render();
			while (!vertexSample.isCalulated) {}
			c[vv] = vertexSample.color; // * Color(lum,lum,lum,1);
		}
	}
	
	public static void SetColorSample(Color32 c) 
	{
		vertexSample.color = ColorAndLuminance(c);
		vertexSample.isCalulated = true;
	}	
	
	public static Color ColorAndLuminance(Color c) 
	{
		c.a = 0.2126f*c.r + 0.7152f*c.g + 0.0722f*c.b;
		return c;
	}

	public static Color32 MultiplyColor32 (Color c0, Color c1) {
		return c0 * c1;
	}
	
 	private static void ApplyColors(Transform go) 
	{
		if (go != null) {
			VDColorHandlerBase ch = go.gameObject.GetComponent<VDColorHandlerBase>();
			if (ch != null) {
				#if UNITY_EDITOR
				Undo.RecordObject(ch, "Apply Vertex Colors to mesh");
				#endif
				MeshFilter mf = ch.gameObject.GetComponent<MeshFilter>();
				if (mf != null) {
					// vertex colors already exists
					if (ch.originalMesh.colors != null && ch.originalMesh.colors.Length > 0) {
						if (VertexDirt.settings.blendModeIndex == 1) {
							Color32[] blendedColors = new Color32[ch.originalMesh.colors.Length];
							for (int b = 0; b<blendedColors.Length; b++) {
								blendedColors[b] = MultiplyColor32 ((Color)c[b], (Color)ch.originalMesh.colors32[b]);
							}
							ch.colors = blendedColors;
						}
						else {
							ch.colors = c;
						}
					}
					else {
						ch.colors = c;
					}
				}
			}
		}
	}
}

//	Class for passing samples from sampler camera to the VertexDirt class. For internal use only
public class VertexSample 
{
	public Color32 color = new Color32(255,255,255,255);
	public int index = 0;
	public bool isCalulated = false;
}