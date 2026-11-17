using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.InteractiveFog.Editor
{
    public static class FogSetup
    {
        public const string PackageRoot = "Packages/com.jlinkdev.interactive-fog";
        [MenuItem("GameObject/Effects/Interactive Fog/Ground Mist", false, 20)]
        public static void CreateVolume() => CreateVolume(FogDensityShape.GroundMist);
        [MenuItem("GameObject/Effects/Interactive Fog/Cumulus", false, 21)]
        public static void CreateCumulus() => CreateVolume(FogDensityShape.Cumulus);
        [MenuItem("GameObject/Effects/Interactive Fog/Dry Ice", false, 22)]
        public static void CreateDryIce() => CreateVolume(FogDensityShape.DryIce);

        public static FogStyleProfile Preset(FogDensityShape shape)
        {
            string name=shape==FogDensityShape.Cumulus?"Cumulus":shape==FogDensityShape.DryIce?"Dry Ice":"Sanctuary Ground Mist";
            return AssetDatabase.LoadAssetAtPath<FogStyleProfile>(PackageRoot+"/Runtime/Styles/"+name+".asset");
        }
        public static FogVolume CreateVolume(FogDensityShape shape)
        {
            var preset=Preset(shape);
            var go = new GameObject(preset?preset.name:"Interactive Fog");
            Undo.RegisterCreatedObjectUndo(go, "Create Interactive Fog");
            var volume = go.AddComponent<FogVolume>();
            volume.simulation = AssetDatabase.LoadAssetAtPath<ComputeShader>(PackageRoot + "/Runtime/Shaders/FogFluid.compute");
            volume.style=preset;
            volume.Initialize();
            Selection.activeGameObject = go;
            return volume;
        }
        public static FogStyleProfile SaveStyleCopy(FogVolume volume,string path)
        {
            if(!volume) throw new System.ArgumentNullException(nameof(volume));
            var asset=SaveProfile(volume.ResolvedStyle,path);
            Undo.RecordObject(volume,"Assign independent fog style");
            volume.style=asset;
            PrefabUtility.RecordPrefabInstancePropertyModifications(volume);
            EditorUtility.SetDirty(volume);
            return asset;
        }
        public static FogStyleProfile SaveProfile(FogStyleSettings settings,string path)
        {
            if(string.IsNullOrEmpty(path) || !path.StartsWith("Assets/") || !path.EndsWith(".asset"))
                throw new System.ArgumentException("Choose an .asset path inside Assets.");
            path=AssetDatabase.GenerateUniqueAssetPath(path);
            var asset=ScriptableObject.CreateInstance<FogStyleProfile>();
            asset.settings=settings;
            AssetDatabase.CreateAsset(asset,path);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }
        public static void ConfigureFeature(FogRendererFeature feature)
        {
            var so = new SerializedObject(feature);
            so.FindProperty("shader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(PackageRoot + "/Runtime/Shaders/InteractiveFog.shader");
            so.ApplyModifiedProperties(); feature.Create(); EditorUtility.SetDirty(feature);
        }
        public static FogRendererFeature ConfigureRenderer(UniversalRendererData renderer)
        {
            if(!renderer || !AssetDatabase.Contains(renderer)) throw new System.ArgumentException("Select a saved Universal Renderer Data asset.");
            FogRendererFeature feature=null;
            foreach(var existing in renderer.rendererFeatures) if(existing is FogRendererFeature fog) { feature=fog;break; }
            Undo.RecordObject(renderer,"Configure fog renderer");
            if(!feature)
            {
                feature=ScriptableObject.CreateInstance<FogRendererFeature>();feature.name="Interactive Fog";
                AssetDatabase.AddObjectToAsset(feature,renderer);Undo.RegisterCreatedObjectUndo(feature,"Add fog renderer feature");
                renderer.rendererFeatures.Add(feature);
            }
            Undo.RecordObject(feature,"Enable fog renderer feature");feature.SetActive(true);
            ConfigureFeature(feature);SynchronizeFeatureMap(renderer);
            EditorUtility.SetDirty(renderer);AssetDatabase.SaveAssetIfDirty(renderer);
            return feature;
        }
        [MenuItem("Tools/jlinkdev/Interactive Fog/Setup")]
        public static void OpenSetup() => EditorWindow.GetWindow<FogSetupWindow>("Fog Setup");
        public static int AssignRendererToCamera(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer, Camera camera)
        {
            if(!pipeline || !AssetDatabase.Contains(pipeline)) throw new System.ArgumentException("Select a saved URP pipeline asset.");
            if(!renderer || !AssetDatabase.Contains(renderer)) throw new System.ArgumentException("Select a saved Universal Renderer Data asset.");
            if(!camera || EditorUtility.IsPersistent(camera) || !camera.gameObject.scene.IsValid())
                throw new System.ArgumentException("Select a scene camera.");
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Assign the camera renderer outside Play Mode.");
            Undo.IncrementCurrentGroup();
            int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Assign fog camera renderer");
            int index=-1;
            using(var data=new SerializedObject(pipeline))
            {
                var list=data.FindProperty("m_RendererDataList");
                for(int i=0;i<list.arraySize;i++)
                    if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer) { index=i;break; }
                if(index<0)
                {
                    index=list.arraySize;list.arraySize++;
                    list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;
                    data.ApplyModifiedProperties();
                }
            }
            var cameraData=camera.GetComponent<UniversalAdditionalCameraData>();
            if(!cameraData) cameraData=Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
            Undo.RecordObject(cameraData,"Assign fog camera renderer");cameraData.SetRenderer(index);
            PrefabUtility.RecordPrefabInstancePropertyModifications(cameraData);
            EditorUtility.SetDirty(cameraData);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            AssetDatabase.SaveAssetIfDirty(pipeline);
            Undo.CollapseUndoOperations(group);
            return index;
        }
        public static void SynchronizeFeatureMap(UniversalRendererData renderer)
        {
            var so = new SerializedObject(renderer);
            var map = so.FindProperty("m_RendererFeatureMap"); map.arraySize = renderer.rendererFeatures.Count;
            for (int i = 0; i < map.arraySize; i++)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i], out string guid, out long id);
                map.GetArrayElementAtIndex(i).longValue = id;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    sealed class FogSetupWindow : EditorWindow
    {
        UniversalRenderPipelineAsset pipeline;
        UniversalRendererData renderer;
        Camera camera;
        Vector2 scroll;
        void OnEnable()
        {
            minSize=new Vector2(360,460);
            pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            camera=Selection.activeGameObject?Selection.activeGameObject.GetComponent<Camera>():null;
            if(!camera) camera=Camera.main;
            if(pipeline)
            {
                using(var data=new SerializedObject(pipeline))
                {
                    var list=data.FindProperty("m_RendererDataList");var index=data.FindProperty("m_DefaultRendererIndex");
                    if(list!=null && index!=null && index.intValue>=0 && index.intValue<list.arraySize)
                        renderer=list.GetArrayElementAtIndex(index.intValue).objectReferenceValue as UniversalRendererData;
                }
            }
        }
        void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("1. Choose the camera's renderer",EditorStyles.boldLabel);
            pipeline=(UniversalRenderPipelineAsset)EditorGUILayout.ObjectField("Pipeline",pipeline,typeof(UniversalRenderPipelineAsset),false);
            renderer=(UniversalRendererData)EditorGUILayout.ObjectField("Renderer",renderer,typeof(UniversalRendererData),false);
            EditorGUILayout.HelpBox("Select the renderer used by your camera. The initial selection is the active pipeline's default; cameras can override it. Configuration adds or repairs one fog feature without replacing other features.",MessageType.Info);
            using(new EditorGUI.DisabledScope(!renderer || !AssetDatabase.Contains(renderer)))
                if(GUILayout.Button("Configure Selected Renderer")) FogSetup.ConfigureRenderer(renderer);
            camera=(Camera)EditorGUILayout.ObjectField("Camera",camera,typeof(Camera),true);
            EditorGUILayout.HelpBox("For a dedicated camera renderer, choose that renderer above, then register and assign it here. This appends or reuses its entry without changing the pipeline's default renderer or project pipeline references. Save the scene afterward.",MessageType.Info);
            if(pipeline && pipeline!=GraphicsSettings.currentRenderPipeline)
                EditorGUILayout.HelpBox("This pipeline is not currently active. The camera override takes effect only when this pipeline is active.",MessageType.Warning);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying || !pipeline || !AssetDatabase.Contains(pipeline) || !renderer || !AssetDatabase.Contains(renderer) || !camera || EditorUtility.IsPersistent(camera)))
                if(GUILayout.Button("Register Renderer and Assign Camera")) FogSetup.AssignRendererToCamera(pipeline,renderer,camera);
            if(pipeline && !pipeline.supportsCameraDepthTexture && GUILayout.Button("Enable Depth Texture on Selected Pipeline"))
            { Undo.RecordObject(pipeline,"Enable fog depth texture");pipeline.supportsCameraDepthTexture=true;EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssetIfDirty(pipeline); }
            EditorGUILayout.HelpBox("URP Render Graph must be enabled. XR and compatibility mode are unsupported. Transparent objects render after fog.",MessageType.Info);
            if(GUILayout.Button("Open Graphics Settings")) SettingsService.OpenProjectSettings("Project/Graphics");
            EditorGUILayout.Space();EditorGUILayout.LabelField("2. Create a fog volume",EditorStyles.boldLabel);
            if(GUILayout.Button("Ground Mist")) FogSetup.CreateVolume();
            if(GUILayout.Button("Cumulus")) FogSetup.CreateCumulus();
            if(GUILayout.Button("Dry Ice")) FogSetup.CreateDryIce();
            EditorGUILayout.HelpBox("Place the volume at floor height, set its bounds and assign lights. Add Fog Interactor to moving characters and Fog Obstacle to solid architecture. New volumes use shared package presets; save an independent style copy before customizing another look.",MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
    }
    [CustomEditor(typeof(FogRendererFeature))]
    sealed class FogFeatureInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Assign Package Shader")) FogSetup.ConfigureFeature((FogRendererFeature)target);
            EditorGUILayout.HelpBox("Requires URP Render Graph and a depth texture. Add one feature to the renderer used by your base camera. XR and overlapping volumes are not supported.", MessageType.Info);
        }
    }
}
