using System.IO;
using UnityEditor;
using UnityEngine;

namespace JLink.Frames.Editor
{
    [InitializeOnLoad, CustomEditor(typeof(ProceduralFrame))]
    public sealed partial class FrameInspector : UnityEditor.Editor
    {
        FrameDesign source;
        FrameEditMode mode;

        bool DrawModeButton(FrameEditMode tool,string label)
        {
            bool active=mode==tool;
            if (GUILayout.Toggle(active,label,"Button")==active) return active;
            mode=active ? FrameEditMode.Frame : tool;
            bandTool.Arm(0); cornerTool.Release(); edgeTool.Release();
            if (mode==FrameEditMode.Loops)
                bandTool.Select(((ProceduralFrame)target).recipe.bands.Count>0 ? 0 : -1);
            SceneView.RepaintAll();
            return mode==tool;
        }
        static FrameInspector() => FrameWorkshop.SaveMeshRequested += SaveMesh;

        [MenuItem("GameObject/JLink/Picture Frame",false,10)]
        static void Create(MenuCommand cmd)
        {
            var go = new GameObject("Picture frame");
            GameObjectUtility.SetParentAndAlign(go,cmd.context as GameObject);
            go.AddComponent<ProceduralFrame>().Rebuild();
            Undo.RegisterCreatedObjectUndo(go,"Create picture frame");
            Selection.activeGameObject = go;
        }

        public override void OnInspectorGUI()
        {
            var frame = (ProceduralFrame)target;
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("recipe"),true);
            if (serializedObject.ApplyModifiedProperties()) frame.Rebuild();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Moulding profile",EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Choose profile"))
                {
                    var menu = new GenericMenu();
                    foreach (Moulding profile in System.Enum.GetValues(typeof(Moulding)))
                        menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(profile.ToString())),false,() =>
                            Edit(frame,() => frame.recipe.profile = FrameRecipe.Profile(profile)));
                    menu.ShowAsContext();
                }
                if (GUILayout.Button("Save mesh asset")) SaveMesh(frame);
            }
            EditorGUILayout.HelpBox("Pull the four Scene view handles to resize the opening, rail and border. Edit Profile to sculpt the cross section.",MessageType.Info);
            var recipeProperty=serializedObject.FindProperty("recipe");
            var easel=recipeProperty.FindPropertyRelative("easelBack");
            EditorGUILayout.PropertyField(easel,new GUIContent("Easel back"));
            if (easel.boolValue)
                EditorGUILayout.PropertyField(recipeProperty.FindPropertyRelative("leanAngle"),new GUIContent("Tabletop lean (degrees)"));
            if (serializedObject.ApplyModifiedProperties()) frame.Rebuild();
            DrawBandInspector(frame);
            DrawCornerInspector(frame);
            DrawEdgeInspector(frame);
            source = (FrameDesign)EditorGUILayout.ObjectField("Saved design",source,typeof(FrameDesign),false);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!source))
                    if (GUILayout.Button("Apply design")) Edit(frame,() => frame.recipe = source.recipe?.Copy() ?? new FrameRecipe());
                if (GUILayout.Button("Save design asset"))
                {
                    var path = EditorUtility.SaveFilePanelInProject("Save editable frame design","Frame design","asset","Choose a project folder.");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var design = CreateInstance<FrameDesign>();
                        design.recipe = frame.recipe.Copy();
                        SaveAsset(design,path);
                        source = AssetDatabase.LoadAssetAtPath<FrameDesign>(path);
                    }
                }
            }
            if (GUILayout.Button("Export OBJ"))
            {
                var path = EditorUtility.SaveFilePanel("Export picture frame",Application.dataPath,"Picture frame","obj");
                if (!string.IsNullOrEmpty(path))
                {
                    frame.Rebuild();
                    FrameObj.Write(frame.GeneratedMesh,path);
                    AssetDatabase.Refresh();
                }
            }
        }

        internal static void SaveMesh(ProceduralFrame frame)
        {
            var path = EditorUtility.SaveFilePanelInProject("Save frame mesh","Picture frame","asset","Choose a project folder.");
            if (string.IsNullOrEmpty(path)) return;
            frame.Rebuild();
            Bake(frame.GeneratedMesh,path);
        }

        public static Mesh Bake(Mesh mesh,string assetPath)
        {
            var clone = Instantiate(mesh);
            clone.hideFlags = HideFlags.None;
            SaveAsset(clone,assetPath);
            return AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        }

        static void SaveAsset(Object value,string path)
        {
            value.name = Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing)
            {
                if (existing.GetType() != value.GetType())
                {
                    DestroyImmediate(value);
                    throw new System.InvalidOperationException("Choose an asset of the same type or a new filename.");
                }
                Undo.RecordObject(existing,"Update frame asset");
                EditorUtility.CopySerialized(value,existing);
                EditorUtility.SetDirty(existing);
                DestroyImmediate(value);
            }
            else AssetDatabase.CreateAsset(value,path);
            AssetDatabase.SaveAssets();
        }

        static void Edit(ProceduralFrame frame,System.Action change)
        {
            Undo.RecordObject(frame,"Shape picture frame");
            change();
            frame.Rebuild();
            PrefabUtility.RecordPrefabInstancePropertyModifications(frame);
            EditorUtility.SetDirty(frame);
        }

        void OnSceneGUI()
        {
            var frame = (ProceduralFrame)target;
            if (!frame.GeneratedMesh) return;
            if (mode==FrameEditMode.Corners || mode==FrameEditMode.Edges) { DrawPerimeterHandles(frame); return; }
            if (mode==FrameEditMode.Loops) { DrawSceneBands(frame); return; }
            using (new Handles.DrawingScope(new Color(.95f,.68f,.31f),frame.DesignToWorld))
            {
                foreach (FrameHandle h in System.Enum.GetValues(typeof(FrameHandle)))
                {
                    var p = FrameHandles.Position(frame.recipe,h);
                    var axis = FrameHandles.Axis(h);
                    EditorGUI.BeginChangeCheck();
                    var next = Handles.Slider(p,axis,HandleUtility.GetHandleSize(p)*.065f,Handles.SphereHandleCap,.001f);
                    if (EditorGUI.EndChangeCheck())
                        Edit(frame,() => FrameHandles.Pull(frame.recipe,h,Vector3.Dot(next-p,axis)));
                }
            }
        }
    }
}
