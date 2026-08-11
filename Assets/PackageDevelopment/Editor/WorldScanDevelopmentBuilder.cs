using System.IO;
using jlinkdev.UnityUtilities.WorldScanning;
using jlinkdev.UnityUtilities.WorldScanning.Rendering;
using jlinkdev.UnityUtilities.WorldScanning.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace jlinkdev.UnityUtilities.WorldScanning.Development
{
    public static class WorldScanDevelopmentBuilder
    {
        private const string RenderingRoot = "Assets/PackageDevelopment/WorldScanning/Rendering";
        private const string AuthoringRoot = "Assets/PackageDevelopment/WorldScanning/SampleAuthoring/World Scan Demo";
        private const string PackageSampleRoot = "Packages/com.jlinkdev.world-scanning/Samples~/World Scan Demo";

        [MenuItem("Tools/jlinkdev/World Scanning/Rebuild Development Content")]
        public static void BuildAll()
        {
            UniversalRenderPipelineAsset pipeline = ConfigureUrp();
            BuildDemo(pipeline);
            PublishSample();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[jlinkdev World Scanning] URP configuration and World Scan Demo rebuilt.");
        }

        private static UniversalRenderPipelineAsset ConfigureUrp()
        {
            EnsureFolder(RenderingRoot);
            string rendererPath = $"{RenderingRoot}/World Scan Universal Renderer.asset";
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "World Scan Universal Renderer";
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            bool hasFeature = false;
            foreach (ScriptableRendererFeature rendererFeature in rendererData.rendererFeatures)
            {
                if (rendererFeature is ScanRendererFeature)
                {
                    hasFeature = true;
                    break;
                }
            }
            if (!hasFeature)
            {
                ScanRendererFeature feature = ScriptableObject.CreateInstance<ScanRendererFeature>();
                feature.name = "World Scan Renderer Feature";
                AssetDatabase.AddObjectToAsset(feature, rendererData);
                rendererData.rendererFeatures.Add(feature);
                feature.Create();
                EditorUtility.SetDirty(feature);
            }

            string pipelinePath = $"{RenderingRoot}/World Scan Universal Render Pipeline.asset";
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                pipeline.name = "World Scan Universal Render Pipeline";
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            pipeline.renderScale = 1f;
            pipeline.msaaSampleCount = 1;
            pipeline.supportsHDR = true;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        private static void BuildDemo(UniversalRenderPipelineAsset pipeline)
        {
            EnsureFolder($"{AuthoringRoot}/Scenes");
            EnsureFolder($"{AuthoringRoot}/Materials");
            EnsureFolder($"{AuthoringRoot}/Profiles");
            EnsureFolder($"{AuthoringRoot}/Settings");

            Material dark = Material("Facility Dark", "Universal Render Pipeline/Lit", new Color(0.045f, 0.055f, 0.07f), 0.05f, 0.42f);
            Material mid = Material("Facility Mid", "Universal Render Pipeline/Lit", new Color(0.11f, 0.13f, 0.16f), 0.18f, 0.5f);
            Material light = Material("Facility Light", "Universal Render Pipeline/Lit", new Color(0.25f, 0.29f, 0.33f), 0.35f, 0.58f);
            Material accent = Material("Scan Accent", "Universal Render Pipeline/Lit", new Color(0.025f, 0.15f, 0.18f), 0.2f, 0.72f, new Color(0.02f, 0.55f, 0.7f) * 1.8f);
            Material reveal = Material("Reveal Markers", "jlinkdev/World Scanning/Scan Reveal Lit", new Color(0.012f, 0.016f, 0.022f), 0.15f, 0.66f);
            ScanProfile survey = Profile("Survey Cyan", ScanShape.Sphere, new Color(0.05f, 0.95f, 1f), 46f, 3.4f, 1.15f, 16f, 1.5f);
            ScanProfile tactical = Profile("Tactical Amber", ScanShape.Sphere, new Color(1f, 0.42f, 0.06f), 38f, 2.25f, 0.75f, 8f, 1f);
            ScanProfile cylinder = Profile("Vertical Systems Sweep", ScanShape.Cylinder, new Color(0.5f, 0.2f, 1f), 42f, 3.8f, 0.9f, 18f, 2f);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "World Scan Demo";
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.045f, 0.065f, 0.09f);
            RenderSettings.ambientEquatorColor = new Color(0.025f, 0.03f, 0.04f);
            RenderSettings.ambientGroundColor = new Color(0.008f, 0.01f, 0.014f);

            CreateLighting();
            CreateFacility(dark, mid, light, accent, reveal);
            GameObject focus = new GameObject("Camera Focus");
            focus.transform.position = new Vector3(0f, 1.5f, 4f);
            CreateCamera(focus.transform);
            CreateVolume();

            GameObject system = new GameObject("World Scan Demonstration");
            system.transform.position = new Vector3(0f, 1.05f, 0f);
            ScanEmitter emitter = system.AddComponent<ScanEmitter>();
            emitter.Profile = survey;
            WorldScanDemoController controller = system.AddComponent<WorldScanDemoController>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("emitter").objectReferenceValue = emitter;
            SerializedProperty profiles = serializedController.FindProperty("profiles");
            profiles.arraySize = 3;
            profiles.GetArrayElementAtIndex(0).objectReferenceValue = survey;
            profiles.GetArrayElementAtIndex(1).objectReferenceValue = tactical;
            profiles.GetArrayElementAtIndex(2).objectReferenceValue = cylinder;
            serializedController.FindProperty("automaticInterval").floatValue = 5f;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, $"{AuthoringRoot}/Scenes/World Scan Demo.unity");
        }

        private static void CreateLighting()
        {
            GameObject keyObject = new GameObject("Key Light");
            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.8f;
            key.color = new Color(0.55f, 0.68f, 0.85f);
            key.shadows = LightShadows.Soft;
            keyObject.transform.rotation = Quaternion.Euler(52f, -34f, 0f);

            GameObject rimObject = new GameObject("Facility Rim Light");
            Light rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Point;
            rim.range = 30f;
            rim.intensity = 1200f;
            rim.color = new Color(0.05f, 0.42f, 0.6f);
            rimObject.transform.position = new Vector3(0f, 8f, 8f);
        }

        private static void CreateFacility(Material dark, Material mid, Material light, Material accent, Material reveal)
        {
            GameObject environment = new GameObject("Greybox Scan Facility");
            CreateCube("Foundation", environment.transform, new Vector3(0f, -0.55f, 4f), new Vector3(44f, 1f, 40f), dark);

            for (int x = -4; x <= 4; x++)
            {
                for (int z = -3; z <= 4; z++)
                {
                    Vector3 position = new Vector3(x * 4f, 0f, z * 4f);
                    Material panelMaterial = (x + z) % 3 == 0 ? light : mid;
                    CreateCube($"Floor Panel {x + 5}-{z + 4}", environment.transform, position, new Vector3(3.82f, 0.12f, 3.82f), panelMaterial);
                }
            }

            CreateCylinder("Scanner Dais", environment.transform, new Vector3(0f, 0.45f, 0f), new Vector3(3.8f, 0.45f, 3.8f), dark);
            CreateCylinder("Scanner Core", environment.transform, new Vector3(0f, 0.95f, 0f), new Vector3(1.2f, 0.65f, 1.2f), accent);

            for (int z = -2; z <= 4; z += 2)
            {
                CreateArch(environment.transform, new Vector3(-10f, 0f, z * 3.5f), Quaternion.Euler(0f, 90f, 0f), dark, light);
                CreateArch(environment.transform, new Vector3(10f, 0f, z * 3.5f), Quaternion.Euler(0f, -90f, 0f), dark, light);
            }

            for (int i = 0; i < 10; i++)
            {
                float angle = i / 10f * Mathf.PI * 2f;
                float radius = i % 2 == 0 ? 13f : 16f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 1.2f + (i % 3) * 0.55f, 5f + Mathf.Sin(angle) * radius);
                Vector3 scale = new Vector3(1.2f + (i % 2), 2.5f + (i % 3) * 1.6f, 1.2f + ((i + 1) % 2));
                GameObject block = CreateCube($"Analysis Mass {i + 1:00}", environment.transform, position, scale, i % 3 == 0 ? light : mid);
                block.transform.rotation = Quaternion.Euler(0f, i * 23f, i % 2 == 0 ? 0f : 7f);
            }

            for (int i = 0; i < 7; i++)
            {
                float x = -15f + i * 5f;
                GameObject marker = CreateCube($"Reveal Data Node {i + 1:00}", environment.transform, new Vector3(x, 1.3f + (i % 2), 12f), new Vector3(1.4f, 2f + (i % 2) * 1.4f, 0.35f), reveal);
                marker.transform.rotation = Quaternion.Euler(0f, i * 7f, 0f);
                CreateBeacon(marker.transform, accent, i % 2 == 0 ? new Color(0.05f, 0.9f, 1f) : new Color(1f, 0.32f, 0.05f));
            }

            CreateCube("Rear Observation Wall", environment.transform, new Vector3(0f, 4f, 20f), new Vector3(40f, 8f, 0.8f), dark);
            CreateCube("Left Boundary", environment.transform, new Vector3(-21f, 2f, 4f), new Vector3(0.6f, 4f, 32f), dark);
            CreateCube("Right Boundary", environment.transform, new Vector3(21f, 2f, 4f), new Vector3(0.6f, 4f, 32f), dark);
        }

        private static void CreateArch(Transform parent, Vector3 position, Quaternion rotation, Material frame, Material inset)
        {
            GameObject root = new GameObject("Structural Scan Arch");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);
            CreateCube("Left Support", root.transform, new Vector3(-2.5f, 2.4f, 0f), new Vector3(0.45f, 4.8f, 0.8f), frame, true);
            CreateCube("Right Support", root.transform, new Vector3(2.5f, 2.4f, 0f), new Vector3(0.45f, 4.8f, 0.8f), frame, true);
            CreateCube("Top Beam", root.transform, new Vector3(0f, 4.6f, 0f), new Vector3(5.45f, 0.45f, 0.8f), inset, true);
        }

        private static void CreateBeacon(Transform parent, Material material, Color color)
        {
            GameObject beacon = CreateSphere("Scan Receiver Beacon", parent, new Vector3(0f, 1.5f, -0.5f), Vector3.one * 0.28f, material, true);
            ScanReceiver receiver = beacon.AddComponent<ScanReceiver>();
            WorldScanDemoBeacon behaviour = beacon.AddComponent<WorldScanDemoBeacon>();
            GameObject lightObject = new GameObject("Beacon Light");
            lightObject.transform.SetParent(beacon.transform, false);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 7f;
            light.intensity = 0f;
            light.color = color;
            SerializedObject serialized = new SerializedObject(behaviour);
            serialized.FindProperty("beaconLight").objectReferenceValue = light;
            serialized.FindProperty("beaconRenderer").objectReferenceValue = beacon.GetComponent<Renderer>();
            serialized.FindProperty("activeColor").colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(receiver);
        }

        private static void CreateCamera(Transform focus)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(17f, 11f, -17f);
            cameraObject.transform.LookAt(focus.position + Vector3.up * 1.5f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 250f;
            camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.006f, 0.009f, 0.015f);
            cameraObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            WorldScanDemoCamera orbit = cameraObject.AddComponent<WorldScanDemoCamera>();
            SerializedObject serialized = new SerializedObject(orbit);
            serialized.FindProperty("focus").objectReferenceValue = focus;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateVolume()
        {
            string path = $"{AuthoringRoot}/Settings/World Scan Demo Volume Profile.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            profile.components.Clear();
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.4f);
            bloom.threshold.Override(1.1f);
            bloom.scatter.Override(0.68f);
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(-0.25f);
            color.contrast.Override(12f);
            color.saturation.Override(-12f);
            EditorUtility.SetDirty(profile);

            GameObject volumeObject = new GameObject("World Scan Post Processing");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
        }

        private static ScanProfile Profile(string name, ScanShape shape, Color color, float range, float duration, float bandWidth, float trailLength, float gridSize)
        {
            string path = $"{AuthoringRoot}/Profiles/{name}.asset";
            ScanProfile profile = AssetDatabase.LoadAssetAtPath<ScanProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ScanProfile>();
                profile.name = name;
                AssetDatabase.CreateAsset(profile, path);
            }
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("shape").enumValueIndex = (int)shape;
            serialized.FindProperty("range").floatValue = range;
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("bandWidth").floatValue = bandWidth;
            serialized.FindProperty("bandSoftness").floatValue = bandWidth * 0.4f;
            serialized.FindProperty("trailLength").floatValue = trailLength;
            serialized.FindProperty("gridCellSize").floatValue = gridSize;
            serialized.FindProperty("trailIntensity").floatValue = 0.12f;
            serialized.FindProperty("emissionIntensity").floatValue = 1.5f;
            serialized.FindProperty("gridIntensity").floatValue = 0.4f;
            serialized.FindProperty("gridMajorIntensity").floatValue = 0.7f;
            serialized.FindProperty("edgeIntensity").floatValue = 0.9f;
            serialized.FindProperty("radiusOverLifetime").animationCurveValue = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            serialized.FindProperty("intensityOverLifetime").animationCurveValue = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.08f, 1f), new Keyframe(0.85f, 0.9f), new Keyframe(1f, 0f));
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.Lerp(color, Color.white, 0.25f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 1f) });
            serialized.FindProperty("colorOverLifetime").gradientValue = gradient;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static Material Material(string name, string shaderName, Color color, float metallic, float smoothness, Color? emission = null)
        {
            string path = $"{AuthoringRoot}/Materials/{name}.mat";
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new System.InvalidOperationException($"Required shader not found: {shaderName}");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool local = false)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            if (local) gameObject.transform.localPosition = position; else gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
        }

        private static GameObject CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool local)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            if (local) gameObject.transform.localPosition = position; else gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
        }

        private static void PublishSample()
        {
            FileUtil.DeleteFileOrDirectory(PackageSampleRoot);
            FileUtil.DeleteFileOrDirectory($"{PackageSampleRoot}.meta");
            Directory.CreateDirectory(Path.GetDirectoryName(PackageSampleRoot));
            FileUtil.CopyFileOrDirectory(AuthoringRoot, PackageSampleRoot);
            FileUtil.CopyFileOrDirectory($"{AuthoringRoot}.meta", $"{PackageSampleRoot}.meta");
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
