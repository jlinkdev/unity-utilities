using System.IO;
using jlinkdev.UnityUtilities.Portals;
using jlinkdev.UnityUtilities.Portals.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace jlinkdev.UnityUtilities.Portals.Development
{
    public static class PortalDevelopmentBuilder
    {
        private const string RenderingRoot = "Assets/PackageDevelopment/Rendering";
        private const string AuthoringRoot = "Assets/PackageDevelopment/Portals/SampleAuthoring/Portal Playground";
        private const string PackageSampleRoot = "Packages/com.jlinkdev.portals/Samples~/Portal Playground";

        [MenuItem("Tools/jlinkdev/Portals/Rebuild Development Content")]
        public static void BuildAll()
        {
            ConfigureUrp();
            BuildPlayground();
            PublishSample();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[jlinkdev Portals] URP host configuration and Portal Playground sample rebuilt.");
        }

        private static void ConfigureUrp()
        {
            EnsureFolder(RenderingRoot);
            string rendererPath = $"{RenderingRoot}/Portal Universal Renderer.asset";
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            string pipelinePath = $"{RenderingRoot}/Portal Universal Render Pipeline.asset";
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                pipeline.name = "Portal Universal Render Pipeline";
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            pipeline.renderScale = 1f;
            pipeline.msaaSampleCount = 4;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorUtility.SetDirty(pipeline);
        }

        private static void BuildPlayground()
        {
            EnsureFolder($"{AuthoringRoot}/Scenes");
            EnsureFolder($"{AuthoringRoot}/Materials");
            EnsureFolder($"{AuthoringRoot}/Prefabs");

            Material portalMaterial = Material("Portal Surface", "jlinkdev/Portals/Portal Surface", new Color(0.9f, 0.97f, 1f));
            Material blue = Material("Blue Clipped", "jlinkdev/Portals/Portal Clipped Lit", new Color(0.12f, 0.5f, 1f));
            Material orange = Material("Orange Clipped", "jlinkdev/Portals/Portal Clipped Lit", new Color(1f, 0.34f, 0.06f));
            Material magenta = Material("Magenta Clipped", "jlinkdev/Portals/Portal Clipped Lit", new Color(0.9f, 0.12f, 0.7f));
            Material neutral = Material("Environment", "Universal Render Pipeline/Lit", new Color(0.24f, 0.28f, 0.34f));
            PortalRenderSettings settings = Settings();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Portal Playground";
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.23f, 0.3f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.14f, 0.18f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.05f, 0.07f);

            CreateLighting();
            CreateEnvironment(neutral, blue, magenta);
            GameObject pair = CreatePortalPair(settings, portalMaterial, neutral);
            PrefabUtility.SaveAsPrefabAsset(pair, $"{AuthoringRoot}/Prefabs/Linked Portal Pair.prefab");
            CreateRecursiveDisplayPair(settings, portalMaterial, neutral);
            CreatePlayer();
            CreateRigidbodyTraveller(orange);
            new GameObject("Sample Instructions").AddComponent<PortalSampleOverlay>();

            EditorSceneManager.SaveScene(scene, $"{AuthoringRoot}/Scenes/Portal Playground.unity");
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(0.86f, 0.92f, 1f);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private static void CreateEnvironment(Material neutral, Material blue, Material magenta)
        {
            CreateCube("Main Floor", new Vector3(0f, -0.25f, 0f), new Vector3(22f, 0.5f, 24f), neutral);
            CreateCube("Exit Floor", new Vector3(16f, -0.25f, 4f), new Vector3(14f, 0.5f, 16f), neutral);

            for (int i = 0; i < 5; i++)
            {
                GameObject bluePillar = CreateCube($"Blue Marker {i + 1}", new Vector3(-7f + i * 2.2f, 1f, 5f), new Vector3(0.7f, 2f + i * 0.35f, 0.7f), blue);
                bluePillar.transform.rotation = Quaternion.Euler(0f, i * 18f, 0f);
                GameObject pinkPillar = CreateCube($"Magenta Marker {i + 1}", new Vector3(13f + i * 1.7f, 0.8f + i * 0.25f, 2f + (i % 2) * 4f), Vector3.one * (1f + i * 0.18f), magenta);
                pinkPillar.transform.rotation = Quaternion.Euler(i * 9f, i * 25f, i * 5f);
            }

            CreateCube("Main Room Backdrop", new Vector3(0f, 2.5f, 7f), new Vector3(18f, 5f, 0.4f), neutral);
            CreateCube("Exit Room Backdrop", new Vector3(22f, 2.5f, 4f), new Vector3(0.4f, 5f, 14f), neutral);
        }

        private static GameObject CreatePortalPair(PortalRenderSettings settings, Material portalMaterial, Material frameMaterial)
        {
            GameObject pair = new GameObject("Linked Portal Pair (Scaled Exit)");
            Portal first = CreatePortal("Portal A — Main Room", pair.transform, new Vector3(0f, 2f, 0f), Quaternion.Euler(0f, 180f, 0f), Vector3.one, settings, portalMaterial, frameMaterial);
            Portal second = CreatePortal("Portal B — Exit Room", pair.transform, new Vector3(9f, 2f, 4f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.72f, settings, portalMaterial, frameMaterial);
            first.LinkedPortal = second;
            second.LinkedPortal = first;
            return pair;
        }

        private static void CreateRecursiveDisplayPair(PortalRenderSettings settings, Material portalMaterial, Material frameMaterial)
        {
            GameObject pair = new GameObject("Recursive Display Pair (Traversal Disabled)");
            Portal first = CreatePortal("Recursive Portal C", pair.transform, new Vector3(-6f, 2f, 3f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.8f, settings, portalMaterial, frameMaterial);
            Portal second = CreatePortal("Recursive Portal D", pair.transform, new Vector3(6f, 2f, 3f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 0.8f, settings, portalMaterial, frameMaterial);
            first.LinkedPortal = second;
            second.LinkedPortal = first;
            first.TraversalEnabled = false;
            second.TraversalEnabled = false;
        }

        private static Portal CreatePortal(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, PortalRenderSettings settings, Material portalMaterial, Material frameMaterial)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);
            root.transform.localScale = scale;

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.35f, 4f, 1.2f);

            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "Portal Surface";
            surface.transform.SetParent(root.transform, false);
            surface.transform.localScale = new Vector3(2f, 3.65f, 1f);
            Object.DestroyImmediate(surface.GetComponent<Collider>());
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = portalMaterial;

            CreateFramePiece(root.transform, "Frame Left", new Vector3(-1.08f, 0f, 0.05f), new Vector3(0.16f, 3.95f, 0.2f), frameMaterial);
            CreateFramePiece(root.transform, "Frame Right", new Vector3(1.08f, 0f, 0.05f), new Vector3(0.16f, 3.95f, 0.2f), frameMaterial);
            CreateFramePiece(root.transform, "Frame Top", new Vector3(0f, 1.9f, 0.05f), new Vector3(2.3f, 0.16f, 0.2f), frameMaterial);
            CreateFramePiece(root.transform, "Frame Bottom", new Vector3(0f, -1.9f, 0.05f), new Vector3(2.3f, 0.16f, 0.2f), frameMaterial);

            Portal portal = root.AddComponent<Portal>();
            portal.SurfaceRenderer = renderer;
            portal.TraversalTrigger = trigger;
            portal.RenderSettings = settings;
            return portal;
        }

        private static void CreateFramePiece(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject piece = CreateCube(name, Vector3.zero, localScale, material);
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
        }

        private static void CreatePlayer()
        {
            GameObject player = new GameObject("Portal Explorer");
            player.transform.position = new Vector3(-4f, 0.05f, -7f);
            player.transform.rotation = Quaternion.Euler(0f, 24f, 0f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            player.AddComponent<PortalTraveller>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 65f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

            PortalSampleController sampleController = player.AddComponent<PortalSampleController>();
            SerializedObject serializedController = new SerializedObject(sampleController);
            serializedController.FindProperty("cameraPivot").objectReferenceValue = cameraObject.transform;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateRigidbodyTraveller(Material material)
        {
            GameObject crate = CreateCube("Rigidbody Portal Crate", new Vector3(0f, 1.15f, -6f), Vector3.one * 0.75f, material);
            Rigidbody body = crate.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            crate.AddComponent<PortalTraveller>();
            crate.AddComponent<PortalSampleRigidbodyLoop>();
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            return gameObject;
        }

        private static Material Material(string name, string shaderName, Color color)
        {
            string path = $"{AuthoringRoot}/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new System.InvalidOperationException($"Required shader not found: {shaderName}");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Tint"))
                material.SetColor("_Tint", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static PortalRenderSettings Settings()
        {
            string path = $"{AuthoringRoot}/Portal Playground Render Settings.asset";
            PortalRenderSettings settings = AssetDatabase.LoadAssetAtPath<PortalRenderSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PortalRenderSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }

            SerializedObject serialized = new SerializedObject(settings);
            serialized.FindProperty("recursionLimit").intValue = 3;
            serialized.FindProperty("renderScale").floatValue = 0.75f;
            serialized.FindProperty("nearClipOffset").floatValue = 0.04f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
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
