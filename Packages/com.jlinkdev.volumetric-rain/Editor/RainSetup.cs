using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace jlinkdev.UnityUtilities.VolumetricRain.Editor
{
    public static class RainSetup
    {
        [MenuItem("Tools/jlinkdev/Volumetric Rain/Add Feature to Selected Renderer")]
        public static void AddToSelectedRenderer()
        {
            var renderer = Selection.activeObject as UniversalRendererData;
            if (renderer == null)
            {
                Debug.LogWarning("Select the Universal Renderer Data asset used by your camera, then run Add Feature to Selected Renderer.");
                return;
            }
            AddFeature(renderer);
            AssetDatabase.SaveAssets();
        }

        public static RainRendererFeature AddFeature(UniversalRendererData renderer)
        {
            var existing = renderer.rendererFeatures.OfType<RainRendererFeature>().FirstOrDefault();
            if (existing != null) return existing;
            var feature = ScriptableObject.CreateInstance<RainRendererFeature>();
            feature.name = "Volumetric Rain";
            Undo.RegisterCreatedObjectUndo(feature, "Add Volumetric Rain");
            Undo.RecordObject(renderer, "Add Volumetric Rain");
            AssetDatabase.AddObjectToAsset(feature, renderer);
            renderer.rendererFeatures.Add(feature);
            feature.Create();
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(feature);
            return feature;
        }

        [MenuItem("Tools/jlinkdev/Volumetric Rain/Create Rain Laboratory")]
        public static void CreateLaboratory() => CreateLaboratory(false);

        [MenuItem("Tools/jlinkdev/Volumetric Rain/Create Volume Laboratory")]
        public static void CreateVolumeLaboratory() => CreateLaboratory(true);

        [MenuItem("Tools/jlinkdev/Volumetric Rain/Create Fog Laboratory")]
        public static void CreateFogLaboratory() => CreateLaboratory(true, true);

        private static void CreateLaboratory(bool withVolumes, bool fogOnly = false)
        {
            // Follow the standard editor save flow before opening a new scene.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string root = "Assets/VolumetricRain";
            if (!AssetDatabase.IsValidFolder(root)) AssetDatabase.CreateFolder("Assets", "VolumetricRain");
            string title = fogOnly ? "Fog Laboratory" : withVolumes ? "Volume Laboratory" : "Rain Laboratory";
            string folder = AssetDatabase.GenerateUniqueAssetPath(root + "/" + title);
            AssetDatabase.CreateFolder(root, System.IO.Path.GetFileName(folder));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var profile = ScriptableObject.CreateInstance<RainProfile>();
            if (fogOnly)
            {
                profile.renderMode = RainRenderMode.FogOnly;
                profile.fogDensity = 1;
                profile.hazeExtinction = .07f;
                profile.fogColor = new Color(.55f,.68f,.8f);
                profile.fogBrightness = 1;
                profile.fogScattering = .8f;
            }
            AssetDatabase.CreateAsset(profile, folder + "/Rain Profile.asset");
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, folder + "/Rain Renderer.asset");
            var feature = AddFeature(renderer);
            feature.sceneViewProfile = profile;
            EditorUtility.SetDirty(feature);
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.name = title + " Pipeline";
            pipeline.supportsCameraDepthTexture = true;
            pipeline.msaaSampleCount = 1;
            AssetDatabase.CreateAsset(pipeline, folder + "/Rain Pipeline.asset");

            var camera = new GameObject("Rain Camera", typeof(Camera), typeof(VolumetricRainCamera)).GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 2.5f, -9);
            camera.transform.rotation = Quaternion.Euler(5, 0, 0);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f);
            camera.farClipPlane = 150;
            camera.tag = "MainCamera";
            camera.gameObject.AddComponent<Demo.RainDemoPipeline>().Pipeline = pipeline;
            camera.GetComponent<VolumetricRainCamera>().profile = profile;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var light = new GameObject("Main Light", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.18f, 0.22f, 0.26f);
            AssetDatabase.CreateAsset(material, folder + "/Greybox.mat");
            MakeBox("Ground", new Vector3(0, -0.25f, 30), new Vector3(60, 0.5f, 100), material);
            MakeBox("Near occluder", new Vector3(-2, 2, 1), new Vector3(2, 4, 1), material);
            MakeBox("Mid occluder", new Vector3(3, 3, 14), new Vector3(3, 6, 2), material);
            MakeBox("Far wall", new Vector3(0, 5, 55), new Vector3(32, 10, 1), material);
            for (int i = 0; i < 8; i++)
                MakeBox("Depth marker " + i, new Vector3(-7, 1.5f, i * 6), new Vector3(0.6f, 3, 0.6f), material);
            if (withVolumes)
            {
                profile.extent = RainExtent.VolumesOnly;
                EditorUtility.SetDirty(profile);
                var wet = new GameObject(fogOnly ? "Fog Volume - move or resize this box" : "Rain Volume - move or resize this box").AddComponent<RainVolume>();
                wet.transform.position = new Vector3(0, 12, 25);
                wet.size = new Vector3(50, 30, 90);
                // The camera starts under a canopy. Rain remains visible beyond its open front.
                var dry = new GameObject(fogOnly ? "Fog Exclusion - canopy interior" : "Dry Volume - canopy interior").AddComponent<RainExclusionVolume>();
                dry.transform.position = new Vector3(0, 2.5f, -9);
                dry.size = new Vector3(10, 5, 10);
                MakeBox("Canopy roof", new Vector3(0, 5.2f, -9), new Vector3(10.4f, .4f, 10.4f), material);
                MakeBox("Canopy left wall", new Vector3(-5.1f, 2.5f, -9), new Vector3(.2f, 5, 10), material);
                MakeBox("Canopy right wall", new Vector3(5.1f, 2.5f, -9), new Vector3(.2f, 5, 10), material);
                MakeBox("Canopy back wall", new Vector3(0, 2.5f, -14.1f), new Vector3(10, 5, .2f), material);
            }
            EditorSceneManager.SaveScene(scene, folder + "/" + title + ".unity");
            AssetDatabase.SaveAssets();
            Selection.activeObject = pipeline;
            Debug.Log(title + " created at " + folder + ". Press Play to activate its pipeline automatically. Previous pipeline settings are restored when the demo stops.");
        }

        private static void MakeBox(string name, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetPositionAndRotation(position, Quaternion.identity);
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
