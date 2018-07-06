/* 
	VertexDirt plug-in for Unity
	Copyright 2014-2016, Zoltan Farago, All rights reserved.
*/
using UnityEditor;
using UnityEngine;
class VertexDirtWindow : EditorWindow 
{
	static VertexDirtWindow window;
	static float tempTime;
	static string[] blendModes = {"Off (Override)","Multiply"};
	// static string[] colorOutput = {"RGB", "None"};
	// static string[] luminanceOutput = {"Red", "Green", "Blue", "Alpha", "None"};

	[MenuItem ("Tools/Zololgo/VertexDirt bake window", false, 10)]
	static void ShowWindow () 
	{
		if (!window)
		{
			window = ScriptableObject.CreateInstance(typeof(VertexDirtWindow)) as VertexDirtWindow;
			Vector2 windowSize = new Vector2(260,430);
			window.position = new Rect(100,100,windowSize.x, windowSize.y);
			window.minSize = windowSize;
			window.maxSize = windowSize;
			#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5_OR_NEWER
				window.titleContent = new GUIContent("VertexDirt Control Panel");
			#else 
				window.title = "VertexDirt Control Panel";
			#endif
			window.ShowUtility();
			VertexDirt.settings.occluderShader = "Hidden/VD-AMBIENTOCCLUSION";
		}
	}
    void OnGUI() 
	{
		// GUILayout.Space(10);
		// GUILayout.BeginHorizontal();
			// GUILayout.Label ("[RGB] bake mode", GUILayout.Width(120));
			// bakingModeIndex = EditorGUILayout.Popup(bakingModeIndex, bakingModes);	
		// GUILayout.EndHorizontal();					
		// GUILayout.Space(5);
		// GUILayout.BeginHorizontal();
			// GUILayout.Label ("[Alpha] bake mode", GUILayout.Width(120));
			// bakingModeIndex = EditorGUILayout.Popup(bakingModeIndex, bakingModes);	
		// GUILayout.EndHorizontal();	

		GUILayout.Space(5);
		GUILayout.Label ("Occlusion distance");
		VertexDirt.settings.samplingDistance = EditorGUILayout.Slider(VertexDirt.settings.samplingDistance,0.1f, 100.0f);
		//
		GUILayout.Space(5);
		GUILayout.Label ("Sampling angle");
		VertexDirt.settings.samplingAngle = EditorGUILayout.Slider(VertexDirt.settings.samplingAngle,45.0f, 145.0f);
		//
		//GUILayout.Space(5);
		//GUILayout.Label ("Sampling bias");
		//VertexDirt.settings.samplingBias = EditorGUILayout.Slider(VertexDirt.settings.samplingBias,0.00,0.1);
		//
		GUILayout.Space(20);
		//
		GUILayout.BeginHorizontal();
			GUILayout.Label ("Blend to existing colors", GUILayout.Width(150));
			VertexDirt.settings.blendModeIndex = EditorGUILayout.Popup(VertexDirt.settings.blendModeIndex, blendModes);	
		GUILayout.EndHorizontal();	
		// GUILayout.BeginHorizontal();
			// GUILayout.Label ("Active layers", GUILayout.Width(150));
			// VertexDirt.settings.bakeLayerMask = EditorGUILayout.LayerField(VertexDirt.settings.bakeLayerMask);
		// GUILayout.EndHorizontal();	
		//
		// GUILayout.BeginHorizontal();
			// GUILayout.Label ("Color output", GUILayout.Width(150));
			// VertexDirt.settings.colorOutputIndex = EditorGUILayout.Popup(VertexDirt.settings.colorOutputIndex, colorOutput);	
		// GUILayout.EndHorizontal();	
		// GUILayout.BeginHorizontal();
			// GUILayout.Label ("Luminance output", GUILayout.Width(150));
			// VertexDirt.settings.luminanceOutputIndex = EditorGUILayout.Popup(VertexDirt.settings.luminanceOutputIndex, luminanceOutput);	
		// GUILayout.EndHorizontal();	
		GUILayout.Space(20);	
		//
		GUILayout.BeginHorizontal();
			VertexDirt.settings.useCustomShadowColor = GUILayout.Toggle(VertexDirt.settings.useCustomShadowColor, "",GUILayout.Width(20));
			GUILayout.Label ("Custom shadow color", GUILayout.Width(140));
			VertexDirt.settings.customShadowColor = EditorGUILayout.ColorField(VertexDirt.settings.customShadowColor);
		GUILayout.EndHorizontal();		
		GUILayout.Space(5);
		GUILayout.BeginHorizontal();
			VertexDirt.settings.useCustomSkyColor = GUILayout.Toggle(VertexDirt.settings.useCustomSkyColor, "",GUILayout.Width(20));
			GUILayout.Label ("Custom sky color", GUILayout.Width(140));
			VertexDirt.settings.customSkyColor = EditorGUILayout.ColorField(VertexDirt.settings.customSkyColor);
		GUILayout.EndHorizontal();
		GUILayout.Space(5);
		GUILayout.BeginHorizontal();
			VertexDirt.settings.edgeSmooth = GUILayout.Toggle(VertexDirt.settings.edgeSmooth, "",GUILayout.Width(20));
			//GUILayout.FlexibleSpace();
			GUILayout.Label ("Average hard edges");
		GUILayout.EndHorizontal();

		if (GUI.Button(new Rect(133,this.position.height-75,117,20),"Online manual") ) {
			Application.OpenURL ("http://zololgo.com/downloads/vertexdirt_manual.pdf");
		}		
 		if (Selection.gameObjects != null) 
		{
			if (GUI.Button(new Rect(10,this.position.height-50,240,40),"Bake") ) 
			{
				VertexDirt.settings.occluderShader = "Hidden/VD-AMBIENTOCCLUSION";
				VertexDirt.Dirt(Selection.GetTransforms(SelectionMode.Deep));
			}
		}
    }
}