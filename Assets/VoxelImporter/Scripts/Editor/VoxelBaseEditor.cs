using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace VoxelImporter
{
    public abstract class VoxelBaseEditor : EditorCommon
    {
        public VoxelBase baseTarget { get; protected set; }
        public VoxelBaseCore baseCore { get; protected set; }

        protected ReorderableList materialList;

        protected VoxelEditorCommon editorCommon;

        protected bool drawEditorMesh = true;
        protected FlagTable3 editVoxelList = new FlagTable3();

        protected VoxelData voxleDataBefore = null;

        protected static Rect editorMaterialEditorWindowRect = new Rect(8, 17 + 8, 0, 0);

        #region GUIStyle
        protected GUIStyle guiStyleMagentaBold;
        protected GUIStyle guiStyleRedBold;
        protected GUIStyle guiStyleFoldoutBold;
        protected GUIStyle guiStyleBoldActiveButton;
        protected GUIStyle guiStyleDropDown;
        protected GUIStyle guiStyleLabelMiddleLeftItalic;
        protected GUIStyle guiStyleTextFieldMiddleLeft;
        protected GUIStyle guiStyleEditorWindow;
        #endregion

        #region strings
        public static readonly string[] Edit_MaterialModeString =
        {
            VoxelBase.Edit_MaterialMode.Add.ToString(),
            VoxelBase.Edit_MaterialMode.Remove.ToString(),
        };
        public static readonly string[] Edit_MaterialTypeModeString =
        {
            VoxelBase.Edit_MaterialTypeMode.Voxel.ToString(),
            VoxelBase.Edit_MaterialTypeMode.Fill.ToString(),
            VoxelBase.Edit_MaterialTypeMode.Rect.ToString(),
        };
        public static readonly string[] Edit_AdvancedModeStrings =
        {
            "Simple",
            "Advanced",
        };
        #endregion

        #region Prefab
        protected PrefabType prefabType { get { return PrefabUtility.GetPrefabType(baseTarget.gameObject); } }
        protected bool prefabEnable { get { var type = prefabType; return type == PrefabType.Prefab || type == PrefabType.PrefabInstance || type == PrefabType.DisconnectedPrefabInstance; } }
        #endregion

        protected virtual void OnEnable()
        {
            baseTarget = target as VoxelBase;
            if (baseTarget == null) return;

            PrefabCreateMonitoringAssetModificationProcessor.Clear();

            PrefabUtility.prefabInstanceUpdated -= EditorPrefabInstanceUpdated;
            PrefabUtility.prefabInstanceUpdated += EditorPrefabInstanceUpdated;
            Undo.undoRedoPerformed -= EditorUndoRedoPerformed;
            Undo.undoRedoPerformed += EditorUndoRedoPerformed;
        }
        protected virtual void OnDisable()
        {
            if (baseTarget == null) return;

            AfterRefresh();

            EditEnableMeshDestroy();

            baseCore.SetSelectedWireframeHidden(false);

            if (PrefabUtility.GetPrefabType(baseTarget.gameObject) == PrefabType.Prefab)
            {
                baseTarget.voxelData = null;
            }
            PrefabCreateMonitoringAssetModificationProcessor.Clear();

            PrefabUtility.prefabInstanceUpdated -= EditorPrefabInstanceUpdated;
            Undo.undoRedoPerformed -= EditorUndoRedoPerformed;
        }
        protected virtual void OnDestroy()
        {
            OnDisable();
        }

        protected virtual void OnEnableInitializeSet()
        {
            baseCore.Initialize();

            editorCommon = new VoxelEditorCommon(baseTarget, baseCore);

            UpdateMaterialList();
            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
                UpdateMaterialEnableMesh();

            #region PrefabCreated
            if (PrefabUtility.GetPrefabType(baseTarget.gameObject) == PrefabType.Prefab)
            {
                var go = VoxelBaseCore.PrefabCreatedGameObjectContains(baseTarget.gameObject);
                if (go != null)
                {
                    VoxelBaseCore.PrefabCreatedGameObjectRemove(baseTarget.gameObject);
                    Refresh();
                    PrefabUtility.RevertPrefabInstance(go);
                }
            }
            #endregion
        }

        protected virtual void GUIStyleReady()
        {
            //Styles
            if (guiStyleMagentaBold == null)
                guiStyleMagentaBold = new GUIStyle(EditorStyles.boldLabel);
            guiStyleMagentaBold.normal.textColor = Color.magenta;
            if (guiStyleRedBold == null)
                guiStyleRedBold = new GUIStyle(EditorStyles.boldLabel);
            guiStyleRedBold.normal.textColor = Color.red;
            if (guiStyleFoldoutBold == null)
                guiStyleFoldoutBold = new GUIStyle(EditorStyles.foldout);
            guiStyleFoldoutBold.fontStyle = FontStyle.Bold;
            if (guiStyleBoldActiveButton == null)
                guiStyleBoldActiveButton = new GUIStyle(GUI.skin.button);
            guiStyleBoldActiveButton.normal = guiStyleBoldActiveButton.active;
            if (guiStyleDropDown == null)
                guiStyleDropDown = new GUIStyle("DropDown");
            guiStyleDropDown.alignment = TextAnchor.MiddleCenter;
            if (guiStyleLabelMiddleLeftItalic == null)
                guiStyleLabelMiddleLeftItalic = new GUIStyle(EditorStyles.label);
            guiStyleLabelMiddleLeftItalic.alignment = TextAnchor.MiddleLeft;
            guiStyleLabelMiddleLeftItalic.fontStyle = FontStyle.Italic;
            if (guiStyleTextFieldMiddleLeft == null)
                guiStyleTextFieldMiddleLeft = new GUIStyle(EditorStyles.textField);
            guiStyleTextFieldMiddleLeft.alignment = TextAnchor.MiddleLeft;
            if (guiStyleEditorWindow == null)
            {
                guiStyleEditorWindow = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).window);
            }
        }

        public override void OnInspectorGUI()
        {
            if (baseTarget == null || editorCommon == null)
            {
                DrawDefaultInspector();
                return;
            }

            baseCore.AutoSetSelectedWireframeHidden();

            serializedObject.Update();
            
            InspectorGUI();
            
            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void InspectorGUI()
        {
            GUIStyleReady();
            editorCommon.GUIStyleReady();
            if(baseTarget.voxelData != voxleDataBefore)
            {
                UpdateMaterialList();
                voxleDataBefore = baseTarget.voxelData;
            }

            #region Simple
            {
                EditorGUI.BeginChangeCheck();
                var mode = GUILayout.Toolbar(baseTarget.advancedMode ? 1 : 0, Edit_AdvancedModeStrings);
                if (EditorGUI.EndChangeCheck())
                {
                    baseTarget.advancedMode = mode != 0 ? true : false;
                }
            }
            #endregion
        }

        protected void InspectorGUI_Import()
        {
            Event e = Event.current;

            baseTarget.edit_importFoldout = EditorGUILayout.Foldout(baseTarget.edit_importFoldout, "Import", guiStyleFoldoutBold);
            if (baseTarget.edit_importFoldout)
            {
                EditorGUI.BeginDisabledGroup(PrefabUtility.GetPrefabType(baseTarget) == PrefabType.Prefab);

                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                EditorGUILayout.BeginVertical();
                #region Voxel File
                {
                    bool fileExists = baseCore.IsVoxelFileExists();
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (string.IsNullOrEmpty(baseTarget.voxelFilePath))
                            EditorGUILayout.LabelField("Voxel File", guiStyleMagentaBold);
                        else if (!fileExists)
                            EditorGUILayout.LabelField("Voxel File", guiStyleRedBold);
                        else
                            EditorGUILayout.LabelField("Voxel File", EditorStyles.boldLabel);

                        Action<string, UnityEngine.Object> OpenFile = (path, obj) =>
                        {
                            if (!baseCore.IsEnableFile(path))
                                return;
                            if(obj == null && path.Contains(Application.dataPath))
                            {
                                var assetPath = path.Replace(Application.dataPath, "Assets");
                                obj = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                            }
                            UndoRecordObject("Open Voxel File", true);
                            baseCore.Reset(path, obj);
                            baseCore.Create(path, obj);
                            UpdateMaterialList();
                        };

                        var rect = GUILayoutUtility.GetRect(new GUIContent("Open"), guiStyleDropDown, GUILayout.Width(64));
                        if (GUI.Button(rect, "Open", guiStyleDropDown))
                        {
                            InspectorGUI_ImportOpenBefore();
                            GenericMenu menu = new GenericMenu();
                            #region vox
                            menu.AddItem(new GUIContent("MagicaVoxel (*.vox)"), false, () =>
                            {
                                var path = EditorUtility.OpenFilePanel("Open MagicaVoxel File", !string.IsNullOrEmpty(baseTarget.voxelFilePath) ? Path.GetDirectoryName(baseTarget.voxelFilePath) : "", "vox");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    OpenFile(path, null);
                                }
                            });
                            #endregion
                            #region qb
                            menu.AddItem(new GUIContent("Qubicle Binary (*.qb)"), false, () =>
                            {
                                var path = EditorUtility.OpenFilePanel("Open Qubicle Binary File", !string.IsNullOrEmpty(baseTarget.voxelFilePath) ? Path.GetDirectoryName(baseTarget.voxelFilePath) : "", "qb");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    OpenFile(path, null);
                                }
                            });
                            #endregion
                            #region png
                            menu.AddItem(new GUIContent("Pixel Art (*.png)"), false, () =>
                            {
                                var path = EditorUtility.OpenFilePanel("Open Pixel Art File", !string.IsNullOrEmpty(baseTarget.voxelFilePath) ? Path.GetDirectoryName(baseTarget.voxelFilePath) : "", "png");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    OpenFile(path, null);
                                }
                            });
                            #endregion
                            menu.ShowAsContext();
                        }
                        #region Drag&Drop
                        {
                            switch (e.type)
                            {
                            case EventType.DragUpdated:
                            case EventType.DragPerform:
                                if (!rect.Contains(e.mousePosition)) break;
                                if (DragAndDrop.paths.Length != 1) break;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                                if (e.type == EventType.DragPerform)
                                {
                                    string path = DragAndDrop.paths[0];
                                    if (Path.GetPathRoot(path) == "")
                                        path = Application.dataPath + DragAndDrop.paths[0].Remove(0, "Assets".Length);
                                    OpenFile(path, DragAndDrop.objectReferences.Length > 0 ? DragAndDrop.objectReferences[0] : null);
                                    e.Use();
                                }
                                break;
                            }
                        }
                        #endregion
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel++;
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (fileExists)
                        {
                            if (baseTarget.voxelFileObject == null)
                            {
                                EditorGUILayout.LabelField(Path.GetFileName(baseTarget.voxelFilePath));
                            }
                            else
                            {
                                EditorGUI.BeginChangeCheck();
                                var obj = EditorGUILayout.ObjectField(baseTarget.voxelFileObject, typeof(UnityEngine.Object), false);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    var path = Application.dataPath + AssetDatabase.GetAssetPath(obj).Remove(0, "Assets".Length);
                                    if (baseCore.IsEnableFile(path))
                                    {
                                        UndoRecordObject("Open Voxel File", true);
                                        baseCore.Reset(path, obj);
                                        baseCore.Create(path, obj);
                                        UpdateMaterialList();
                                    }
                                }
                            }
                            if (baseTarget.advancedMode)
                            {
                                EditorGUILayout.LabelField(baseTarget.voxelData != null ? "Loaded" : "Unloaded", GUILayout.Width(80));
                                if (baseTarget.voxelData == null)
                                {
                                    if (GUILayout.Button("Load", GUILayout.Width(54), GUILayout.Height(16)))
                                    {
                                        baseCore.ReadyVoxelData();
                                    }
                                }
                                else
                                {
                                    if (GUILayout.Button("UnLoad", GUILayout.Width(54), GUILayout.Height(16)))
                                    {
                                        baseTarget.voxelData = null;
                                    }
                                }
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Voxel file not found. Please open file.", MessageType.Error);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
                #endregion
                #region Settings
                {
                    EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    {
                        if (baseTarget.advancedMode)
                        {
                            #region LegacyVoxImport
                            if (baseTarget.fileType == VoxelBase.FileType.vox)
                            {
                                EditorGUI.BeginChangeCheck();
                                var legacyVoxImport = EditorGUILayout.Toggle(new GUIContent("Legacy Vox Import", "Import with legacy behavior up to Version 1.1.2.\nMultiple objects do not correspond.\nIt is deprecated for future use.\nThis is left for compatibility."), baseTarget.legacyVoxImport);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    UndoRecordObject("Inspector", true);
                                    baseTarget.legacyVoxImport = legacyVoxImport;
                                    baseCore.ReadyVoxelData(true);
                                    Refresh();
                                }
                            }
                            #endregion

                            #region Import Mode
                            {
                                EditorGUI.BeginChangeCheck();
                                var importMode = (VoxelObject.ImportMode)EditorGUILayout.EnumPopup("Import Mode", baseTarget.importMode);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    UndoRecordObject("Inspector");
                                    baseTarget.importMode = importMode;
                                    Refresh();
                                }
                            }
                            #endregion
                            #region Import Flag
                            {
                                EditorGUI.BeginChangeCheck();
#if UNITY_2017_3_OR_NEWER
                                var importFlags = (VoxelObject.ImportFlag)EditorGUILayout.EnumFlagsField("Import Flag", baseTarget.importFlags);
#else
                                var importFlags = (VoxelObject.ImportFlag)EditorGUILayout.EnumMaskField("Import Flag", baseTarget.importFlags);
#endif
                                if (EditorGUI.EndChangeCheck())
                                {
                                    UndoRecordObject("Inspector", true);
                                    baseTarget.importFlags = importFlags;
                                    baseCore.ReadyVoxelData(true);
                                    Refresh();
                                }
                            }
                            #endregion
                        }
                        #region Import Scale
                        {
                            InspectorGUI_ImportSettingsImportScale();
                        }
                        #endregion
                        #region Import Offset
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                InspectorGUI_ImportSettingsImportOffset();
                            }
                            {
                                if(GUILayout.Button("Set", guiStyleDropDown, GUILayout.Width(40), GUILayout.Height(14)))
                                {
                                    GenericMenu menu = new GenericMenu();
                                    #region Reset
                                    menu.AddItem(new GUIContent("Reset"), false, () =>
                                    {
                                        UndoRecordObject("Inspector", true);
                                        baseTarget.importOffset = Vector3.zero;
                                        Refresh();
                                    });
                                    #endregion
                                    #region Center
                                    menu.AddItem(new GUIContent("Center"), false, () =>
                                    {
                                        UndoRecordObject("Inspector", true);
                                        baseTarget.importOffset = Vector3.zero;
                                        baseTarget.importOffset = -baseCore.GetVoxelsCenter();
                                        Refresh();
                                    });
                                    #endregion
                                    InspectorGUI_ImportOffsetSetExtra(menu);
                                    menu.ShowAsContext();
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        #endregion
                        if (baseTarget.advancedMode)
                        {
                            #region Enable Face
                            {
                                EditorGUI.BeginChangeCheck();
#if UNITY_2017_3_OR_NEWER
                                var enableFaceFlags = (VoxelBase.Face)EditorGUILayout.EnumFlagsField("Enable Face", baseTarget.enableFaceFlags);
#else
                                var enableFaceFlags = (VoxelBase.Face)EditorGUILayout.EnumMaskField("Enable Face", baseTarget.enableFaceFlags);
#endif
                                if (EditorGUI.EndChangeCheck())
                                {
                                    UndoRecordObject("Inspector");
                                    baseTarget.enableFaceFlags = enableFaceFlags;
                                    Refresh();
                                }
                            }
                            #endregion
                        }
                        InspectorGUI_ImportSettingsExtra();
                    }
                    EditorGUI.indentLevel--;
                }
                #endregion
                #region Optimize
                if (baseTarget.advancedMode)
                {
                    EditorGUILayout.LabelField("Optimize", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    {
                        #region combineFaces
                        {
                            EditorGUI.BeginChangeCheck();
                            var combineFaces = EditorGUILayout.Toggle("Combine Voxel Faces", baseTarget.combineFaces);
                            if (EditorGUI.EndChangeCheck())
                            {
                                UndoRecordObject("Inspector");
                                baseTarget.combineFaces = combineFaces;
                                Refresh();
                            }
                        }
                        #endregion
                        #region Ignore the cavity
                        {
                            EditorGUI.BeginChangeCheck();
                            var ignoreCavity = EditorGUILayout.Toggle("Ignore Cavity", baseTarget.ignoreCavity);
                            if (EditorGUI.EndChangeCheck())
                            {
                                UndoRecordObject("Inspector");
                                baseTarget.ignoreCavity = ignoreCavity;
                                Refresh();
                            }
                        }
                        #endregion
                    }
                    EditorGUI.indentLevel--;
                }
                #endregion
                #region Output
                if (baseTarget.advancedMode)
                {
                    EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(new GUIContent("Voxel Structure", "Save the structure information."), baseTarget.voxelStructure, typeof(VoxelStructure), false);
                        EditorGUI.EndDisabledGroup();
                        if (baseTarget.voxelStructure == null)
                        {
                            if (GUILayout.Button("Save", GUILayout.Width(48), GUILayout.Height(16)))
                            {
                                #region Save
                                UndoRecordObject("Inspector");
                                string path = EditorUtility.SaveFilePanel("Save as", baseCore.GetDefaultPath(), string.Format("{0}.asset", Path.GetFileNameWithoutExtension(baseTarget.voxelFilePath)), "asset");
                                if (!string.IsNullOrEmpty(path))
                                {
                                    if (path.IndexOf(Application.dataPath) < 0)
                                    {
                                        SaveInsideAssetsFolderDisplayDialog();
                                    }
                                    else
                                    {
                                        path = path.Replace(Application.dataPath, "Assets");
                                        baseTarget.voxelStructure = ScriptableObject.CreateInstance<VoxelStructure>();
                                        baseTarget.voxelStructure.Set(baseCore.voxelData);
                                        AssetDatabase.CreateAsset(baseTarget.voxelStructure, path);
                                    }
                                }
                                #endregion
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Reset", GUILayout.Width(48), GUILayout.Height(16)))
                            {
                                #region Reset
                                UndoRecordObject("Inspector");
                                baseTarget.voxelStructure = null;
                                #endregion
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
                #endregion
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();
            }
        }
        protected virtual void UndoRecordObject(string text, bool reset = false)
        {
            if (baseTarget != null)
                Undo.RecordObject(baseTarget, text);
        }
        protected virtual void InspectorGUI_ImportOpenBefore() { }
        protected virtual void InspectorGUI_ImportSettingsImportScale()
        {
            EditorGUI.BeginChangeCheck();
            var importScale = EditorGUILayout.Vector3Field("Import Scale", baseTarget.importScale);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject("Inspector", true);
                baseTarget.importScale = importScale;
                Refresh();
            }
        }
        protected virtual void InspectorGUI_ImportSettingsImportOffset()
        {
            EditorGUI.BeginChangeCheck();
            var importOffset = EditorGUILayout.Vector3Field("Import Offset", baseTarget.importOffset);
            if (EditorGUI.EndChangeCheck())
            {
                UndoRecordObject("Inspector", true);
                baseTarget.importOffset = importOffset;
                Refresh();
            }
        }
        protected virtual void InspectorGUI_ImportSettingsExtra() { }
        protected virtual void InspectorGUI_ImportOffsetSetExtra(GenericMenu menu) { }
        protected virtual void InspectorGUI_Refresh()
        {
            if (GUILayout.Button("Refresh"))
            {
                UndoRecordObject("Inspector");
                Refresh();
            }
        }

        protected virtual void OnSceneGUI()
        {
            if (baseTarget == null || editorCommon == null) return;

            GUIStyleReady();
            editorCommon.GUIStyleReady();

            Event e = Event.current;
            bool repaint = false;
            
            #region Configure Material
            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
            {
                if (baseTarget.materialData != null && materialList != null &&
                    baseTarget.edit_configureMaterialIndex >= 0 && baseTarget.edit_configureMaterialIndex < baseTarget.materialData.Count)
                {
                    if (SceneView.currentDrawingSceneView == SceneView.lastActiveSceneView)
                    {
                        #region Material Editor
                        if (baseTarget.edit_configureMaterialIndex > 0)
                        {
                            editorMaterialEditorWindowRect = GUILayout.Window(EditorGUIUtility.GetControlID(FocusType.Passive, editorMaterialEditorWindowRect), editorMaterialEditorWindowRect, (id) =>
                            {
                                #region MaterialMode
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var edit_materialMode = (VoxelBase.Edit_MaterialMode)GUILayout.Toolbar((int)baseTarget.edit_materialMode, Edit_MaterialModeString);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(baseTarget, "Material Mode");
                                        baseTarget.edit_materialMode = edit_materialMode;
                                        ShowNotification();
                                    }
                                }
                                #endregion
                                #region MaterialTypeMode
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var edit_materialTypeMode = (VoxelBase.Edit_MaterialTypeMode)GUILayout.Toolbar((int)baseTarget.edit_materialTypeMode, Edit_MaterialTypeModeString);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(baseTarget, "Material Type Mode");
                                        baseTarget.edit_materialTypeMode = edit_materialTypeMode;
                                        ShowNotification();
                                    }
                                }
                                #endregion
                                #region Transparent
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var transparent = EditorGUILayout.Toggle("Transparent", baseTarget.materialData[baseTarget.edit_configureMaterialIndex].transparent);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(baseTarget, "Transparent");
                                        baseTarget.materialData[baseTarget.edit_configureMaterialIndex].transparent = transparent;
                                        baseTarget.edit_afterRefresh = true;
                                    }
                                }
                                #endregion
                                #region MaterialPreviewMode
                                {
                                    EditorGUI.BeginChangeCheck();
                                    var edit_MaterialPreviewMode = (VoxelBase.Edit_MaterialPreviewMode)EditorGUILayout.EnumPopup("Preview", baseTarget.edit_MaterialPreviewMode);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(baseTarget, "Material Preview Mode");
                                        baseTarget.edit_MaterialPreviewMode = edit_MaterialPreviewMode;
                                    }
                                }
                                #endregion
                                #region WeightClear
                                {
                                    if (GUILayout.Button("Clear"))
                                    {
                                        Undo.RecordObject(baseTarget, "Clear");
                                        baseTarget.materialData[baseTarget.edit_configureMaterialIndex].ClearMaterial();
                                        UpdateMaterialEnableMesh();
                                        baseTarget.edit_afterRefresh = true;
                                    }
                                }
                                #endregion
                                #region Help
                                if (!baseTarget.edit_helpEnable)
                                {
                                    #region "?"
                                    {
                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUILayout.Space();
                                        if (GUILayout.Button("?", baseTarget.edit_helpEnable ? editorCommon.guiStyleActiveButton : GUI.skin.button, GUILayout.Width(16)))
                                        {
                                            Undo.RecordObject(baseTarget, "Help Enable");
                                            baseTarget.edit_helpEnable = !baseTarget.edit_helpEnable;
                                        }
                                        EditorGUILayout.EndHorizontal();
                                    }
                                    #endregion
                                }
                                else
                                {
                                    EditorGUI.BeginChangeCheck();
                                    baseTarget.edit_helpEnable = EditorGUILayout.Foldout(baseTarget.edit_helpEnable, "Help");
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        editorMaterialEditorWindowRect.width = editorMaterialEditorWindowRect.height = 0;
                                    }
                                    EditorGUILayout.BeginVertical(GUI.skin.box);
                                    EditorGUILayout.LabelField("F5 Key - Refresh");
                                    EditorGUILayout.LabelField("Press Space Key - Hide Preview");
                                    EditorGUILayout.EndVertical();
                                }
                                #endregion

                                GUI.DragWindow();

                            }, "Material Editor", guiStyleEditorWindow);
                            editorMaterialEditorWindowRect = editorCommon.ResizeSceneViewRect(editorMaterialEditorWindowRect);
                        }
                        #endregion
                    }

                    #region Event
                    {
                        Tools.current = Tool.None;
                        switch (e.type)
                        {
                        case EventType.MouseMove:
                            editVoxelList.Clear();
                            editorCommon.selectionRect.Reset();
                            editorCommon.ClearPreviewMesh();
                            UpdateCursorMesh();
                            break;
                        case EventType.MouseDown:
                            if (editorCommon.CheckMousePositionEditorRects())
                            {
                                if (!e.alt && e.button == 0)
                                {
                                    editorCommon.ClearCursorMesh();
                                    EventMouseDrag(true);
                                }
                                else if (!e.alt && e.button == 1)
                                {
                                    ClearMakeAddData();
                                }
                            }
                            break;
                        case EventType.MouseDrag:
                            {
                                if (!e.alt && e.button == 0)
                                {
                                    EventMouseDrag(false);
                                }
                            }
                            break;
                        case EventType.MouseUp:
                            if (!e.alt && e.button == 0)
                            {
                                EventMouseApply();
                            }
                            ClearMakeAddData();
                            UpdateCursorMesh();
                            repaint = true;
                            break;
                        case EventType.Layout:
                            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                            break;
                        }
                        switch (e.type)
                        {
                        case EventType.KeyDown:
                            if (!e.alt)
                            {
                                if (e.keyCode == KeyCode.F5)
                                {
                                    Refresh();
                                }
                                else if (e.keyCode == KeyCode.Space)
                                {
                                    drawEditorMesh = false;
                                }
                            }
                            break;
                        case EventType.KeyUp:
                            {
                                if (e.keyCode == KeyCode.Space)
                                {
                                    drawEditorMesh = true;
                                }
                            }
                            break;
                        }
                    }
                    #endregion

                    if (drawEditorMesh)
                    {
                        DrawBaseMesh();

                        #region MaterialMesh
                        if (baseTarget.edit_enableMesh != null)
                        {
                            for (int i = 0; i < baseTarget.edit_enableMesh.Length; i++)
                            {
                                if (baseTarget.edit_enableMesh[i] == null) continue;
                                if (baseTarget.edit_MaterialPreviewMode == VoxelBase.Edit_MaterialPreviewMode.Transparent)
                                {
                                    editorCommon.vertexColorTransparentMaterial.color = new Color(1, 0, 0, 0.75f);
                                    editorCommon.vertexColorTransparentMaterial.SetPass(0);
                                }
                                else
                                {
                                    editorCommon.vertexColorMaterial.color = new Color(1, 0, 0, 1);
                                    editorCommon.vertexColorMaterial.SetPass(0);
                                }
                                Graphics.DrawMeshNow(baseTarget.edit_enableMesh[i], baseTarget.transform.localToWorldMatrix);
                            }
                        }
                        #endregion
                    }

                    if (SceneView.currentDrawingSceneView == SceneView.lastActiveSceneView)
                    {

                        #region Preview Mesh
                        if (editorCommon.previewMesh != null)
                        {
                            Color color = Color.white;
                            if (baseTarget.edit_materialMode == VoxelBase.Edit_MaterialMode.Add)
                            {
                                color = new Color(1, 0, 0, 1);
                            }
                            else if (baseTarget.edit_materialMode == VoxelBase.Edit_MaterialMode.Remove)
                            {
                                color = new Color(0, 0, 1, 1);
                            }
                            color.a = 0.5f + 0.5f * (1f - editorCommon.AnimationPower);
                            for (int i = 0; i < editorCommon.previewMesh.Length; i++)
                            {
                                if (editorCommon.previewMesh[i] == null) continue;
                                editorCommon.vertexColorTransparentMaterial.color = color;
                                editorCommon.vertexColorTransparentMaterial.SetPass(0);
                                Graphics.DrawMeshNow(editorCommon.previewMesh[i], baseTarget.transform.localToWorldMatrix);
                            }
                            repaint = true;
                        }
                        #endregion

                        #region Cursor Mesh
                        {
                            float color = 0.2f + 0.4f * (1f - editorCommon.AnimationPower);
                            if (editorCommon.cursorMesh != null)
                            {
                                for (int i = 0; i < editorCommon.cursorMesh.Length; i++)
                                {
                                    if (editorCommon.cursorMesh[i] == null) continue;
                                    editorCommon.vertexColorTransparentMaterial.color = new Color(1, 1, 1, color);
                                    editorCommon.vertexColorTransparentMaterial.SetPass(0);
                                    Graphics.DrawMeshNow(editorCommon.cursorMesh[i], baseTarget.transform.localToWorldMatrix);
                                }
                            }
                            repaint = true;
                        }
                        #endregion

                        #region Selection Rect
                        if (baseTarget.edit_materialTypeMode == VoxelBase.Edit_MaterialTypeMode.Rect)
                        {
                            if (editorCommon.selectionRect.Enable)
                            {
                                Handles.BeginGUI();
                                GUI.Box(editorCommon.selectionRect.rect, "", "SelectionRect");
                                Handles.EndGUI();
                                repaint = true;
                            }
                        }
                        #endregion
                    }
                }
            }
            #endregion

            if (repaint)
            {
                SceneView.currentDrawingSceneView.Repaint();
            }
        }

        protected abstract void DrawBaseMesh();

        protected void UpdatePreviewMesh()
        {
            editorCommon.ClearPreviewMesh();

            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material &&
                baseTarget.edit_configureMaterialIndex > 0 && baseTarget.edit_configureMaterialIndex < baseTarget.materialData.Count)
            {
                List<VoxelData.Voxel> voxels = new List<VoxelData.Voxel>();
                editVoxelList.AllAction((x, y, z) =>
                {
                    var index = baseCore.voxelData.VoxelTableContains(x, y, z);
                    if (index < 0) return;
                    var voxel = baseCore.voxelData.voxels[index];
                    voxel.palette = -1;
                    voxels.Add(voxel);
                });
                if (voxels.Count > 0)
                {
                    editorCommon.previewMesh = baseCore.Edit_CreateMesh(voxels, null, false);
                }
            }
        }
        protected void UpdateCursorMesh()
        {
            editorCommon.ClearCursorMesh();

            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material &&
                baseTarget.edit_configureMaterialIndex > 0 && baseTarget.edit_configureMaterialIndex < baseTarget.materialData.Count)
            {
                switch (baseTarget.edit_materialTypeMode)
                {
                case VoxelBase.Edit_MaterialTypeMode.Voxel:
                    {
                        var result = editorCommon.GetMousePositionVoxel();
                        if (result.HasValue)
                        {
                            editorCommon.cursorMesh = baseCore.Edit_CreateMesh(new List<VoxelData.Voxel>() { new VoxelData.Voxel() { position = result.Value, palette = -1 } });
                        }
                    }
                    break;
                case VoxelBase.Edit_MaterialTypeMode.Fill:
                    {
                        var pos = editorCommon.GetMousePositionVoxel();
                        if (pos.HasValue)
                        {
                            var faceAreaTable = editorCommon.GetFillVoxelFaceAreaTable(pos.Value);
                            if (faceAreaTable != null)
                                editorCommon.cursorMesh = new Mesh[1] { baseCore.Edit_CreateMeshOnly_Mesh(faceAreaTable, null, null) };
                        }
                    }
                    break;
                }
            }
        }

        protected void ClearMakeAddData()
        {
            editVoxelList.Clear();
            editorCommon.selectionRect.Reset();
            editorCommon.ClearPreviewMesh();
            editorCommon.ClearCursorMesh();
        }

        private void EventMouseDrag(bool first)
        {
            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
            {
                UpdateCursorMesh();
                switch (baseTarget.edit_materialTypeMode)
                {
                case VoxelBase.Edit_MaterialTypeMode.Voxel:
                    {
                        var result = editorCommon.GetMousePositionVoxel();
                        if (result.HasValue)
                        {
                            editVoxelList.Set(result.Value, true);
                            UpdatePreviewMesh();
                        }
                    }
                    break;
                case VoxelBase.Edit_MaterialTypeMode.Fill:
                    {
                        var pos = editorCommon.GetMousePositionVoxel();
                        if (pos.HasValue)
                        {
                            var result = editorCommon.GetFillVoxel(pos.Value);
                            if (result != null)
                            {
                                for (int i = 0; i < result.Count; i++)
                                    editVoxelList.Set(result[i], true);
                                UpdatePreviewMesh();
                            }
                        }
                    }
                    break;
                case VoxelBase.Edit_MaterialTypeMode.Rect:
                    {
                        var pos = new IntVector2((int)Event.current.mousePosition.x, (int)Event.current.mousePosition.y);
                        if (first) { editorCommon.selectionRect.Reset(); editorCommon.selectionRect.SetStart(pos); }
                        else editorCommon.selectionRect.SetEnd(pos);
                        //
                        editVoxelList.Clear();
                        {
                            var list = editorCommon.GetSelectionRectVoxel();
                            for (int i = 0; i < list.Count; i++)
                                editVoxelList.Set(list[i], true);
                        }
                        UpdatePreviewMesh();
                    }
                    break;
                }
            }
        }
        private void EventMouseApply()
        {
            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
            {
                Undo.RecordObject(baseTarget, "Material");

                bool update = false;
                if (baseTarget.edit_materialMode == VoxelBase.Edit_MaterialMode.Add)
                {
                    editVoxelList.AllAction((x, y, z) =>
                    {
                        if (!update)
                            DisconnectPrefabInstance();

                        for (int i = 0; i < baseTarget.materialData.Count; i++)
                        {
                            if (i == baseTarget.edit_configureMaterialIndex) continue;
                            if (baseTarget.materialData[i].GetMaterial(new IntVector3(x, y, z)))
                            {
                                baseTarget.materialData[i].RemoveMaterial(new IntVector3(x, y, z));
                            }
                        }
                        baseTarget.materialData[baseTarget.edit_configureMaterialIndex].SetMaterial(new IntVector3(x, y, z));
                        update = true;
                    });
                }
                else if (baseTarget.edit_materialMode == VoxelBase.Edit_MaterialMode.Remove)
                {
                    editVoxelList.AllAction((x, y, z) =>
                    {
                        if (baseTarget.materialData[baseTarget.edit_configureMaterialIndex].GetMaterial(new IntVector3(x, y, z)))
                        {
                            if (!update)
                                DisconnectPrefabInstance();

                            baseTarget.materialData[baseTarget.edit_configureMaterialIndex].RemoveMaterial(new IntVector3(x, y, z));
                            update = true;
                        }
                    });
                }
                else
                {
                    Assert.IsTrue(false);
                }
                if (update)
                {
                    UpdateMaterialEnableMesh();
                    baseTarget.edit_afterRefresh = true;
                }
                editVoxelList.Clear();
            }
        }

        private void ShowNotification()
        {
            SceneView.currentDrawingSceneView.ShowNotification(new GUIContent(string.Format("{0} - {1}", baseTarget.edit_materialMode, baseTarget.edit_materialTypeMode)));
        }

        protected abstract List<Material> GetMaterialListMaterials();
        protected virtual void AddMaterialData(string name)
        {
            baseTarget.materialData.Add(new MaterialData() { name = name });
        }
        protected virtual void RemoveMaterialData(int index)
        {
            baseTarget.materialData.RemoveAt(index);
        }
        protected void UpdateMaterialList()
        {
            materialList = new ReorderableList(
                serializedObject,
                serializedObject.FindProperty("materialData"),
                false, true,
                !baseTarget.loadFromVoxelFile,
                !baseTarget.loadFromVoxelFile
            );
            materialList.elementHeight = 20;
            materialList.drawHeaderCallback = (rect) =>
            {
                Rect r = rect;
                EditorGUI.LabelField(r, "Name", EditorStyles.boldLabel);
                r.x = 182;
                var materials = GetMaterialListMaterials();
                if (materials != null)
                    EditorGUI.LabelField(r, "Material", EditorStyles.boldLabel);
                #region LoadFormVoxelFile
                r.x = r.width - 128;
                {
                    EditorGUI.BeginDisabledGroup(baseTarget.voxelData != null && baseTarget.voxelData.materials == null);
                    EditorGUI.BeginChangeCheck();
                    string tooltip = null;
                    if (baseTarget.voxelData == null)
                        tooltip = "Voxel data is not loaded.";
                    else if(baseTarget.voxelData.materials == null)
                        tooltip = "Material is not included in the voxel data.";
                    var loadFromVoxelFile = EditorGUI.ToggleLeft(r, new GUIContent("Load From Voxel File", tooltip), baseTarget.loadFromVoxelFile);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UndoRecordObject("Inspector");
                        baseTarget.loadFromVoxelFile = loadFromVoxelFile;
                        EditorApplication.delayCall += () =>
                        {
                            if (baseTarget.loadFromVoxelFile)
                            {
                                baseTarget.voxelData = null;
                                baseTarget.voxelDataCreatedVoxelFileTimeTicks = 0;
                                Refresh();
                            }
                            else
                            {
                                UpdateMaterialList();
                            }
                        };
                    }
                    EditorGUI.EndDisabledGroup();
                }
                #endregion
            };
            materialList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.yMin += 2;
                rect.yMax -= 2;
                if (index < baseTarget.materialData.Count)
                {
                    #region Name
                    {
                        Rect r = rect;
                        r.width = 144;
                        if (index == 0)
                        {
                            EditorGUI.LabelField(r, "default", guiStyleLabelMiddleLeftItalic);
                        }
                        else if(!baseTarget.loadFromVoxelFile)
                        {
                            EditorGUI.BeginChangeCheck();
                            string name = EditorGUI.TextField(r, baseTarget.materialData[index].name, guiStyleTextFieldMiddleLeft);
                            if (EditorGUI.EndChangeCheck())
                            {
                                UndoRecordObject("Inspector");
                                baseTarget.materialData[index].name = name;
                            }
                        }
                        else
                        {
                            EditorGUI.LabelField(r, baseTarget.materialData[index].name, guiStyleLabelMiddleLeftItalic);
                        }
                    }
                    #endregion
                    #region Material
                    var materials = GetMaterialListMaterials();
                    if (materials != null && index < materials.Count)
                    {
                        {
                            Rect r = rect;
                            r.xMin = 182;
                            r.width = rect.width - r.xMin;
                            if (baseTarget.advancedMode)
                                r.width -= 64;
                            if (baseTarget.advancedMode && materials[index] != null && !IsMainAsset(materials[index]))
                                r.width -= 48;
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUI.ObjectField(r, materials[index], typeof(Material), false);
                            EditorGUI.EndDisabledGroup();
                        }
                        if (baseTarget.advancedMode && materials[index] != null)
                        {
                            Rect r = rect;
                            r.xMin += rect.width - 46;
                            r.width = 48;
                            {
                                if (GUI.Button(r, "Reset"))
                                {
                                    #region Reset Material
                                    EditorApplication.delayCall += () =>
                                    {
                                        UndoRecordObject("Reset Material");
                                        if (!IsMainAsset(materials[index]))
                                            materials[index] = null;
                                        else
                                            materials[index] = Instantiate<Material>(materials[index]);
                                        Refresh();
                                    };
                                    #endregion
                                }
                            }
                            if (!IsMainAsset(materials[index]))
                            {
                                r.xMin -= 52;
                                r.width = 48;
                                if (GUI.Button(r, "Save"))
                                {
                                    #region Create Material
                                    string path = EditorUtility.SaveFilePanel("Save material", baseCore.GetDefaultPath(), string.Format("{0}_mat{1}.mat", baseTarget.gameObject.name, index), "mat");
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        if (path.IndexOf(Application.dataPath) < 0)
                                        {
                                            SaveInsideAssetsFolderDisplayDialog();
                                        }
                                        else
                                        {
                                            EditorApplication.delayCall += () =>
                                            {
                                                UndoRecordObject("Save Material");
                                                path = path.Replace(Application.dataPath, "Assets");
                                                AssetDatabase.CreateAsset(Material.Instantiate(materials[index]), path);
                                                materials[index] = AssetDatabase.LoadAssetAtPath<Material>(path);
                                                Refresh();
                                            };
                                        }
                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                    #endregion
                }
            };
            materialList.onSelectCallback = (list) =>
            {
                UndoRecordObject("Inspector");
                baseTarget.edit_configureMaterialIndex = list.index;
                if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
                    UpdateMaterialEnableMesh();
                InternalEditorUtility.RepaintAllViews();
            };
            materialList.onAddCallback = (list) =>
            {
                UndoRecordObject("Inspector");
                AddMaterialData(baseTarget.materialData.Count.ToString());
                var materials = GetMaterialListMaterials();
                if (materials != null)
                    materials.Add(null);
                Refresh();
                baseTarget.edit_configureMaterialIndex = list.count;
                list.index = baseTarget.edit_configureMaterialIndex;
                InternalEditorUtility.RepaintAllViews();
            };
            materialList.onRemoveCallback = (list) =>
            {
                if (list.index > 0 && list.index < baseTarget.materialData.Count)
                {
                    UndoRecordObject("Inspector");
                    RemoveMaterialData(list.index);
                    var materials = GetMaterialListMaterials();
                    if (materials != null)
                        materials.RemoveAt(list.index);
                    Refresh();
                    baseTarget.edit_configureMaterialIndex = -1;
                    if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
                        UpdateMaterialEnableMesh();
                    InternalEditorUtility.RepaintAllViews();
                }
            };
            if (baseTarget.edit_configureMaterialIndex >= 0 && baseTarget.materialData != null && baseTarget.edit_configureMaterialIndex < baseTarget.materialData.Count)
                materialList.index = baseTarget.edit_configureMaterialIndex;
            else
                baseTarget.edit_configureMaterialIndex = 0;
        }

        protected void UpdateMaterialEnableMesh()
        {
            if (baseTarget.materialData == null || baseCore.voxelData == null)
            {
                EditEnableMeshDestroy();
                return;
            }

            UndoRecordObject("Inspector");

            List<VoxelData.Voxel> voxels = new List<VoxelData.Voxel>(baseCore.voxelData.voxels.Length);
            if (baseTarget.edit_configureMaterialIndex == 0)
            {
                for (int i = 0; i < baseCore.voxelData.voxels.Length; i++)
                {
                    {
                        bool enable = true;
                        for (int j = 0; j < baseTarget.materialData.Count; j++)
                        {
                            if (baseTarget.materialData[j].GetMaterial(baseCore.voxelData.voxels[i].position))
                            {
                                enable = false;
                                break;
                            }
                        }
                        if (!enable) continue;
                    }
                    var voxel = baseCore.voxelData.voxels[i];
                    voxel.palette = -1;
                    voxels.Add(voxel);
                }
            }
            else if (baseTarget.edit_configureMaterialIndex >= 0 && baseTarget.edit_configureMaterialIndex < baseTarget.materialData.Count)
            {
                baseTarget.materialData[baseTarget.edit_configureMaterialIndex].AllAction((pos) =>
                {
                    var index = baseCore.voxelData.VoxelTableContains(pos);
                    if (index < 0) return;

                    var voxel = baseCore.voxelData.voxels[index];
                    voxel.palette = -1;
                    voxels.Add(voxel);
                });
            }
            baseTarget.edit_enableMesh = baseCore.Edit_CreateMesh(voxels);
        }

        public void EditEnableMeshDestroy()
        {
            if (baseTarget.edit_enableMesh != null)
            {
                UndoRecordObject("Inspector");

                for (int i = 0; i < baseTarget.edit_enableMesh.Length; i++)
                {
                    MonoBehaviour.DestroyImmediate(baseTarget.edit_enableMesh[i]);
                }
                baseTarget.edit_enableMesh = null;
            }
        }

        protected void AfterRefresh()
        {
            if (AnimationMode.InAnimationMode())
            {
                return;
            }

            if (baseTarget.edit_afterRefresh)
                Refresh();
        }
        protected virtual void Refresh()
        {
            baseCore.ReCreate();

            UpdateMaterialList();

            if (baseTarget.edit_configureMode == VoxelBase.Edit_configureMode.Material)
                UpdateMaterialEnableMesh();
        }

        protected void DisconnectPrefabInstance()
        {
            if (PrefabUtility.GetPrefabType(baseTarget) == PrefabType.PrefabInstance)
            {
                PrefabUtility.DisconnectPrefabInstance(baseTarget);
            }
        }

        protected class PrefabCreateMonitoringAssetModificationProcessor : UnityEditor.AssetModificationProcessor
        {
            public static bool IsContains(string path)
            {
                return paths.Contains(path);
            }
            public static void Remove(string path)
            {
                paths.Remove(path);
            }
            public static void Clear()
            {
                paths.Clear();
            }

            protected static List<string> paths = new List<string>();

            protected static void OnWillCreateAsset(string path)
            {
                paths.Add(path);
            }
        }

        protected virtual void EditorPrefabInstanceUpdated(GameObject go)
        {
            var prefabType = PrefabUtility.GetPrefabType(go);
            if (prefabType != PrefabType.PrefabInstance) return;
            var path = AssetDatabase.GetAssetPath(PrefabUtility.GetPrefabParent(go));
            if (!PrefabCreateMonitoringAssetModificationProcessor.IsContains(path)) return;
            PrefabCreateMonitoringAssetModificationProcessor.Remove(path);
            VoxelBaseCore.PrefabCreatedGameObjectAdd(go);
        }
        protected virtual void EditorUndoRedoPerformed()
        {
            if (AnimationMode.InAnimationMode())
            {
                baseTarget.edit_afterRefresh = true;
                return;
            }

            if (baseTarget != null && baseCore != null)
            {
                if (baseCore.RefreshCheckerCheck())
                {
                    if (baseTarget.importFlags != baseTarget.refreshChecker.importFlags)
                        baseCore.ReadyVoxelData(true);
                    Refresh();
                }
                else
                {
                    baseCore.SetRendererCompornent();
                }
            }
            Repaint();
        }
    }
}
