/*
VertexDirt plug-in for Unity
Copyright 2014-2017, Zoltan Farago, All rights reserved.
*/
using UnityEditor;
using UnityEngine;

class VDSaveVertexColoredMeshes : EditorWindow {

	private string path = "Plugins/VertexDirt/Saved meshes";
	static  VDSaveVertexColoredMeshes window;

	[MenuItem ("Tools/Zololgo/VertexDirt save meshes", false, 20)]

	public static void Init() {
		if (!window){
			window = ScriptableObject.CreateInstance<VDSaveVertexColoredMeshes>();
			window.position = new Rect(200,200, 640,140);
			window.minSize = new Vector2 (640,140);
			window.maxSize = new Vector2 (640,140);
			#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5_OR_NEWER
			window.titleContent = new GUIContent("VertexDirt save meshes");
			#else
			window.title = "VertexDirt save meshes";
			#endif
			window.ShowUtility();
		}
	}

	void OnGUI() {
		GUILayout.Label("This tool collects the sharedMeshes of the Selection's Mesh Filter components, and saves them to .asset files.");
		GUILayout.Label("Then you can use the saved meshes with the generated colors without the need of VertexDirt's ColorHandler.");
		GUILayout.Label("");
		GUILayout.Label("Select single GameObject.");
		path = EditorGUILayout.TextField("Asset path for saving: ", path);
		if (GUILayout.Button("Save meshes of children.", GUILayout.Height(40))) {
			//Debug.Log (GetPathName(Selection.activeTransform, ""));
			Transform[]  gos = Selection.activeTransform.GetComponentsInChildren<Transform>();

			foreach (Transform t in gos) {

				if (t.gameObject.GetComponent<VDColorHandlerBase>() && t.gameObject.GetComponent<MeshFilter>()) {

					try {
						AssetDatabase.CreateAsset (t.gameObject.GetComponent<MeshFilter>().sharedMesh, "Assets/"+path+"/"+GetPathName(t, "") +".asset");
					}
					catch(UnityException e) {
						Debug.Log (e+"\nThis asset already saved. If you have multiple gameobjects at the same hierarchy and with the same name, please give them uniqe names.");
					}
					AssetDatabase.SaveAssets();
					t.gameObject.GetComponent<VDColorHandlerBase>().coloredMesh =
					t.gameObject.GetComponent<MeshFilter>().sharedMesh;

				}

			}
			AssetDatabase.Refresh();
		}
		Repaint();
	}

	public string GetPathName(Transform t, string s){
		s = t.name + s;
		if (t.parent != null) {
			s = "--" + s;
			s = GetPathName(t.parent, s);
		}
		return s;
	}
}
