using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;

[CanEditMultipleObjects, CustomEditor(typeof(POLMObject))]
public class POLMObjectEditor : Editor
{
    SerializedObject obj;
    SerializedProperty oMesh;
    SerializedProperty bMesh;
    SerializedProperty bMeshUsed;
    MonoScript script;

    public void OnEnable()
    {
        script = MonoScript.FromMonoBehaviour((POLMObject)target);
        obj = new SerializedObject(target);
        oMesh = obj.FindProperty("o_Mesh");
        bMesh = obj.FindProperty("b_Mesh");
        bMeshUsed = obj.FindProperty("b_MeshUsed");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnInspectorGUI()
    {
        obj.Update();

        EditorGUI.BeginDisabledGroup(true);
        script = EditorGUILayout.ObjectField("Script:", script, typeof(MonoScript), false) as MonoScript;
        EditorGUI.EndDisabledGroup();

        GUI.backgroundColor = POLMColors.ye1;
        EditorGUILayout.HelpBox("This script holds a reference to the object's saved mesh. The original mesh is replaced with the baked one at level start.", MessageType.Info);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(oMesh, new GUIContent("Original Mesh"));
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.PropertyField(bMesh, new GUIContent("Baked Mesh"));

        int objectsSelected = Selection.transforms.Length;

        GUILayout.Space(5f);

        GUI.backgroundColor = POLMColors.ye1;
        EditorGUILayout.HelpBox(Application.isPlaying ? "Not available in Play Mode" : "Toggle baked meshes for preview.", MessageType.Info);

        if (!Application.isPlaying)
        {
            if (GUILayout.Button(objectsSelected > 1 ?
                            (bMeshUsed.boolValue == true ? "Revert to original meshes" : "Preview Baked Meshes") :
                            (bMeshUsed.boolValue == true ? "Revert to original mesh" : "Preview Baked Mesh")))
            {
                Transform[] transforms = Selection.GetTransforms(SelectionMode.ExcludePrefab);
                foreach (Transform t in transforms)
                {
                    if (t.GetComponent<POLMObject>())
                    {
                        t.GetComponent<POLMObject>().ToggleBakedMeshPreview();
                    }
                }
            }
        }

        GUILayout.Space(5f);

        GUI.backgroundColor = POLMColors.ye1;
        EditorGUILayout.HelpBox(Application.isPlaying ? "Not available in Play Mode" : "This will revert to original mesh remove the component.", MessageType.Info);

        if (!Application.isPlaying)
        {
            if (GUILayout.Button(objectsSelected > 1 ? "Remove Components" : "Remove Component"))
            {
                Transform[] transforms = Selection.GetTransforms(SelectionMode.ExcludePrefab);
                foreach (Transform t in transforms)
                {
                    if (t.GetComponent<POLMObject>())
                    {
                        t.GetComponent<POLMObject>().RemoveComponent();
                        DestroyImmediate(t.GetComponent<POLMObject>());
                    }
                }
            }   
        }

        GUILayout.Space(5f);

        GUI.backgroundColor = POLMColors.lightRed;
        string _message = "This will apply the mesh to the object and will remove the component.\n" + 
            "IMPORTANT: it is recommended to use this option only if you are sure you will not bake this mesh again, just because the reference to the real mesh is lost after this component is removed. If you want to bake the object's mesh again, you will have to load the original mehs into the MeshFilter component.";
        EditorGUILayout.HelpBox(Application.isPlaying ? "Not available in Play Mode" : _message, MessageType.Info);
        if (!Application.isPlaying)
        {
            if (GUILayout.Button(objectsSelected > 1 ? "Apply mesh and remove Components" : "Apply mesh and remove Component"))
            {
                Transform[] transforms = Selection.GetTransforms(SelectionMode.ExcludePrefab);
                foreach (Transform t in transforms)
                {
                    if (t.GetComponent<POLMObject>())
                    {
                        t.GetComponent<POLMObject>().ApplyMeshAndRemoveComponent();
                        DestroyImmediate(t.GetComponent<POLMObject>());
                    }
                }
            }
        }
        
        if (GUI.changed)
        {
            if (obj.targetObject != null)
            {
                Undo.RecordObject((target as POLMObject), "make changes to object");
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                obj.ApplyModifiedProperties();
            }
        }
    }
}
