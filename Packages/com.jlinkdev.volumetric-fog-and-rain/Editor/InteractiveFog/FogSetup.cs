using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.InteractiveFog.Editor
{
    public static class FogSetup
    {
        public const string PackageRoot = "Packages/com.jlinkdev.volumetric-fog-and-rain";
        public const string RuntimeRoot = PackageRoot + "/Runtime/InteractiveFog";

        [MenuItem("GameObject/Effects/Volumetric Fog and Rain/Interactive Fog/Ground Mist", false, 20)]
        public static void CreateVolume() => CreateVolume(FogDensityShape.GroundMist);
        [MenuItem("GameObject/Effects/Volumetric Fog and Rain/Interactive Fog/Cumulus", false, 21)]
        public static void CreateCumulus() => CreateVolume(FogDensityShape.Cumulus);
        [MenuItem("GameObject/Effects/Volumetric Fog and Rain/Interactive Fog/Dry Ice", false, 22)]
        public static void CreateDryIce() => CreateVolume(FogDensityShape.DryIce);

        public static FogStyleProfile Preset(FogDensityShape shape)
        {
            string name=shape==FogDensityShape.Cumulus?"Cumulus":shape==FogDensityShape.DryIce?"Dry Ice":"Sanctuary Ground Mist";
            return AssetDatabase.LoadAssetAtPath<FogStyleProfile>(RuntimeRoot+"/Styles/"+name+".asset");
        }
        public static FogVolume CreateVolume(FogDensityShape shape)
        {
            var preset=Preset(shape);
            var go = new GameObject(preset?preset.name:"Interactive Fog");
            Undo.RegisterCreatedObjectUndo(go, "Create Interactive Fog");
            var volume = go.AddComponent<FogVolume>();
            volume.simulation = AssetDatabase.LoadAssetAtPath<ComputeShader>(RuntimeRoot + "/Shaders/FogFluid.compute");
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
            so.FindProperty("shader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(RuntimeRoot + "/Shaders/InteractiveFog.shader");
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
        [MenuItem("Tools/jlinkdev/Volumetric Fog and Rain/Interactive Fog Setup")]
        public static void OpenSetup() => EditorWindow.GetWindow<FogSetupWindow>("Fog Setup");
        /// <summary>Resolve a camera override with URP's default fallback, without adding camera components.</summary>
        public static UniversalRendererData GetCameraRenderer(UniversalRenderPipelineAsset pipeline, Camera camera)
        {
            if(!pipeline) return null;
            int index=-1;
            var cameraData=camera?camera.GetComponent<UniversalAdditionalCameraData>():null;
            if(cameraData)
                using(var data=new SerializedObject(cameraData)) index=data.FindProperty("m_RendererIndex").intValue;
            using(var data=new SerializedObject(pipeline))
            {
                var list=data.FindProperty("m_RendererDataList");
                if(index<0 || index>=list.arraySize || !list.GetArrayElementAtIndex(index).objectReferenceValue)
                    index=data.FindProperty("m_DefaultRendererIndex").intValue;
                return index>=0 && index<list.arraySize
                    ? list.GetArrayElementAtIndex(index).objectReferenceValue as UniversalRendererData : null;
            }
        }
        public static int AssignRendererToCamera(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer, Camera camera)
        {
            if(!pipeline || !AssetDatabase.Contains(pipeline)) throw new System.ArgumentException("Select a saved URP pipeline asset.");
            if(!renderer || !AssetDatabase.Contains(renderer)) throw new System.ArgumentException("Select a saved Universal Renderer Data asset.");
            if(!camera || EditorUtility.IsPersistent(camera) || !camera.gameObject.scene.IsValid())
                throw new System.ArgumentException("Select a scene camera.");
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Assign the camera renderer outside Play Mode.");
            var cameraData=camera.GetComponent<UniversalAdditionalCameraData>();
            if(cameraData && cameraData.renderType==CameraRenderType.Overlay)
                throw new System.ArgumentException("Fog requires a base camera; overlay cameras do not render the effect.");
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
            renderer=FogSetup.GetCameraRenderer(pipeline,camera);
        }
        void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("1. Choose the camera's renderer",EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            pipeline=(UniversalRenderPipelineAsset)EditorGUILayout.ObjectField("Pipeline",pipeline,typeof(UniversalRenderPipelineAsset),false);
            camera=(Camera)EditorGUILayout.ObjectField("Camera",camera,typeof(Camera),true);
            if(EditorGUI.EndChangeCheck()) renderer=FogSetup.GetCameraRenderer(pipeline,camera);
            renderer=(UniversalRendererData)EditorGUILayout.ObjectField("Renderer",renderer,typeof(UniversalRendererData),false);
            var currentRenderer=FogSetup.GetCameraRenderer(pipeline,camera);
            var cameraData=camera?camera.GetComponent<UniversalAdditionalCameraData>():null;
            bool overlay=cameraData && cameraData.renderType==CameraRenderType.Overlay;
            if(overlay) EditorGUILayout.HelpBox("Overlay cameras do not render fog. Select the base camera in the stack.",MessageType.Warning);
            else if(camera && !currentRenderer) EditorGUILayout.HelpBox("This camera does not resolve to a Universal Renderer in the selected pipeline. Choose a Universal Renderer asset and register it below.",MessageType.Warning);
            else if(camera && currentRenderer!=renderer)
            {
                EditorGUILayout.HelpBox("The chosen renderer differs from the camera's current renderer. Register and assign it below to use it on this camera.",MessageType.Info);
                if(GUILayout.Button("Use Camera's Current Renderer")) renderer=currentRenderer;
            }
            EditorGUILayout.HelpBox("Choosing a camera or pipeline selects its current renderer, including camera overrides. Configure adds or repairs one fog feature on the selected asset; all cameras using that renderer share the change.",MessageType.Info);
            using(new EditorGUI.DisabledScope(!renderer || !AssetDatabase.Contains(renderer)))
                if(GUILayout.Button("Configure Selected Renderer")) FogSetup.ConfigureRenderer(renderer);
            EditorGUILayout.HelpBox("For a dedicated camera renderer, choose that renderer above, then register and assign it here. This appends or reuses its entry without changing the pipeline's default renderer or project pipeline references. Save the scene afterward.",MessageType.Info);
            if(pipeline && pipeline!=GraphicsSettings.currentRenderPipeline)
                EditorGUILayout.HelpBox("This pipeline is not currently active. The camera override takes effect only when this pipeline is active.",MessageType.Warning);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying || overlay || !pipeline || !AssetDatabase.Contains(pipeline) || !renderer || !AssetDatabase.Contains(renderer) || !camera || EditorUtility.IsPersistent(camera)))
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
