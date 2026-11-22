using System.IO;
using jlinkdev.UnityUtilities.WaterSystem;
using jlinkdev.UnityUtilities.WaterSystem.Editor;
using jlinkdev.UnityUtilities.WaterSystem.EditableSamples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.WaterSystem.Development
{
    /// <summary>Host-only authoring utility. Never changes Graphics or Quality project settings.</summary>
    public static class WaterSystemDevelopmentBuilder
    {
        public const string Root = "Assets/PackageSamples/com.jlinkdev.water-system/Water Laboratory";

        [MenuItem("Tools/jlinkdev/Water System/Rebuild Water Laboratory (Development)")]
        public static void BuildAll()
        {
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Profiles");
            EnsureFolder(Root + "/Settings");
            EnsureFolder(Root + "/Scenes");
            var renderer = LoadOrCreate<UniversalRendererData>(Root + "/Settings/Water Renderer.asset");
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            bool found = false;
            foreach (var item in renderer.rendererFeatures)
                if (item is WaterRendererFeature) found = true;
            if (!found)
            {
                var feature = ScriptableObject.CreateInstance<WaterRendererFeature>();
                feature.name = "Water Renderer Feature";
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
                feature.Create();
                EditorUtility.SetDirty(feature);
            }
            WaterSystemSetup.SynchronizeFeatureMap(renderer);
            // Forward rendering and an intermediate color target are the baseline sample configuration.
            var serializedRenderer = new SerializedObject(renderer);
            var intermediate = serializedRenderer.FindProperty("m_IntermediateTextureMode");
            if (intermediate != null) intermediate.intValue = 1;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            string pipelinePath = Root + "/Settings/Water Pipeline.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "Water Pipeline";
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            pipeline.supportsCameraDepthTexture = true;
            pipeline.supportsCameraOpaqueTexture = true;
            pipeline.msaaSampleCount = 1;
            pipeline.supportsHDR = true;
            pipeline.colorGradingMode = ColorGradingMode.HighDynamicRange;
            pipeline.renderScale = 1f;
            EditorUtility.SetDirty(pipeline);
            var profiles = new WaterProfile[3];
            for (int i = 0; i < profiles.Length; i++)
            {
                string name = ((WaterSystemSetup.WaterPreset)i).ToString();
                profiles[i] = LoadOrCreate<WaterProfile>(Root + "/Profiles/" + name + " Water.asset");
                WaterSystemSetup.ApplyPreset(profiles[i], (WaterSystemSetup.WaterPreset)i);
                EditorUtility.SetDirty(profiles[i]);
            }

            var sand = Material("Pale Sandstone", new Color(0.55f, 0.51f, 0.39f), 0.2f);
            var slate = Material("Deep Slate", new Color(0.085f, 0.14f, 0.17f), 0.3f);
            var tile = Material("Pool Ceramic", new Color(0.32f, 0.53f, 0.53f), 0.45f);
            var gold = Material("Floating Amber", new Color(0.83f, 0.35f, 0.065f), 0.3f);
            var white = Material("Reference White", new Color(0.85f, 0.9f, 0.86f), 0.3f);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Water Laboratory";
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.72f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.4f, 0.44f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.2f, 0.23f);
            RenderSettings.fog = false;
            var volumeProfile = LoadOrCreate<VolumeProfile>(Root + "/Settings/Laboratory Grading.asset");
            var toneMapping = VolumeComponent<Tonemapping>(volumeProfile);
            toneMapping.mode.Override(TonemappingMode.ACES);
            var colorAdjustments = VolumeComponent<ColorAdjustments>(volumeProfile);
            colorAdjustments.postExposure.Override(-0.2f);
            EditorUtility.SetDirty(toneMapping);
            EditorUtility.SetDirty(colorAdjustments);
            EditorUtility.SetDirty(volumeProfile);
            var volume = new GameObject("Laboratory Color Grading").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = volumeProfile;
            var sunlight = new GameObject("Sunlight").AddComponent<Light>();
            sunlight.type = LightType.Directional;
            sunlight.intensity = 2.4f;
            sunlight.color = new Color(1f, 0.94f, 0.82f);
            sunlight.shadows = LightShadows.Soft;
            sunlight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var environment = new GameObject("Pool Architecture").transform;
            Cube("Seabed", environment, new Vector3(0f, -4.15f, 0f), new Vector3(30f, 0.3f, 30f), sand);
            Cube("Far Wall", environment, new Vector3(0f, -1.25f, 12.3f), new Vector3(27f, 5.5f, 0.7f), slate);
            Cube("Left Wall", environment, new Vector3(-12.3f, -1.25f, 0f), new Vector3(0.7f, 5.5f, 25f), slate);
            Cube("Right Wall", environment, new Vector3(12.3f, -1.25f, 0f), new Vector3(0.7f, 5.5f, 25f), slate);
            Cube("Near Wall", environment, new Vector3(0f, -1.25f, -12.3f), new Vector3(27f, 5.5f, 0.7f), slate);
            for (int i = 0; i < 6; i++)
                Cube("Submerged Step " + (i + 1), environment, new Vector3(-6f, -3.75f + i * 0.58f, -1f + i * 1.2f), new Vector3(3.2f, 0.5f + i * 1.16f, 1.15f), tile);
            for (int x = -4; x <= 4; x++)
                Cube("Seabed Grid X " + x, environment, new Vector3(x * 2.5f, -3.988f, 0f), new Vector3(0.035f, 0.014f, 24f), tile);
            for (int z = -4; z <= 4; z++)
                Cube("Seabed Grid Z " + z, environment, new Vector3(0f, -3.987f, z * 2.5f), new Vector3(24f, 0.015f, 0.035f), tile);
            for (int i = 0; i < 5; i++)
            {
                float height = 1.8f + i * 0.7f;
                Cube("Depth Column " + (i + 1), environment, new Vector3(4.5f, -4f + height * 0.5f, -2f + i * 2.4f), new Vector3(1.1f, height, 1.1f), i % 2 == 0 ? white : tile);
            }
            for (int i = 0; i < 12; i++)
                Cube("Far Wall Marker " + i, environment, new Vector3(-11f + i * 2f, 0.12f, 11.9f), new Vector3(0.07f, 1.6f, 0.05f), white);

            var water = new GameObject("Water Body").AddComponent<WaterBody>();
            water.Profile = profiles[0];
            water.Size = new Vector2(24f, 24f);
            water.Depth = 5f;
            water.Resolution = 96;
            water.Rebuild();
            var floating = new Rigidbody[3];
            for (int i = 0; i < floating.Length; i++)
            {
                var prop = Cube("Floating Crate " + (i + 1), null, new Vector3(-2.8f + i * 2.9f, 0.3f + i * 0.1f, 0.2f + i * 2f), new Vector3(1.15f, 0.8f, 1.15f), i == 1 ? white : gold);
                prop.transform.rotation = Quaternion.Euler(3f * i, 18f + i * 23f, 4f * i);
                floating[i] = prop.AddComponent<Rigidbody>();
                floating[i].mass = 20f;
                floating[i].interpolation = RigidbodyInterpolation.Interpolate;
                var buoyancy = prop.AddComponent<WaterBuoyancy>();
                buoyancy.BodyOverride = water;
            }
            var camera = new GameObject("Laboratory Camera", typeof(AudioListener)).AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 1.6f, -8f);
            camera.transform.rotation = Quaternion.Euler(9.6f, 0f, 0f);
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 150f;
            camera.fieldOfView = 65f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.47f, 0.66f, 0.77f);
            camera.allowHDR = true;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.requiresDepthTexture = true;
            cameraData.requiresColorTexture = true;
            cameraData.renderPostProcessing = true;
            camera.gameObject.AddComponent<WaterCamera>().BodyOverride = water;
            var particles = new GameObject("Suspended Water Particles").AddComponent<WaterParticles>();
            particles.BodyOverride = water;
            particles.FollowTarget = camera.transform;
            var laboratory = new GameObject("Water Laboratory Controls").AddComponent<WaterLaboratoryController>();
            laboratory.Water = water;
            laboratory.ViewCamera = camera;
            camera.gameObject.AddComponent<WaterCameraPass>().feature =
                (WaterRendererFeature)renderer.rendererFeatures.Find(item => item is WaterRendererFeature);
            camera.allowMSAA = false;
            laboratory.Presets = profiles;
            laboratory.FloatingProps = floating;
            SampleScenePresentation.Apply(scene, Root);
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/Water Laboratory.unity");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            PackageSamplePublisher.Publish(Root);
            Debug.Log("[Water System] Water Laboratory built and published. Project Graphics and Quality settings were not changed.");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T VolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var component)) return component;
            component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static Material Material(string name, Color color, float smoothness)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.name = name;
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.position = position;
            item.transform.localScale = scale;
            item.GetComponent<MeshRenderer>().sharedMaterial = material;
            return item;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

    }
}
