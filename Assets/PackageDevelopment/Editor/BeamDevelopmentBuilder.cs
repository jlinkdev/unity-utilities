using System.IO;
using jlinkdev.UnityUtilities.Beams;
using jlinkdev.UnityUtilities.Beams.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace jlinkdev.UnityUtilities.Beams.Development
{
    public static class BeamDevelopmentBuilder
    {
        private const string AuthoringRoot = "Assets/PackageDevelopment/Beams/SampleAuthoring/Beam Kit Demo";
        private const string PackageSampleRoot = "Packages/com.jlinkdev.beams/Samples~/Beam Kit Demo";

        [MenuItem("Tools/jlinkdev/Beams/Rebuild Development Content")]
        public static void BuildAll()
        {
            EnsureFolder(AuthoringRoot);
            BuildDemoScene();
            PublishSample();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[jlinkdev Beams] Beam Kit Demo rebuilt and published to Samples~.");
        }

        private static void BuildDemoScene()
        {
            EnsureFolder($"{AuthoringRoot}/Scenes");
            EnsureFolder($"{AuthoringRoot}/Materials");

            Material environment = Material("Demo Environment", new Color(0.09f, 0.12f, 0.17f), 0.45f);
            Material targets = Material("Demo Targets", new Color(0.16f, 0.22f, 0.3f), 0.75f);
            Material clean = LoadPackageMaterial("5ae079f5b395424ba626c300a2a069d4");
            Material soft = LoadPackageMaterial("84c6f66ca9c14a038c6b7a0dfffa1f95");
            Material electrical = LoadPackageMaterial("c39dcf68c97a46aba0a76635300e2fea");

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            scene.name = "Beam Kit Demo";

            CreateCamera();
            CreateLight();
            CreatePrimitive("Floor", PrimitiveType.Cube, new Vector3(0f, -0.45f, 3f), new Vector3(18f, 0.5f, 12f), environment);
            CreatePrimitive("Backdrop", PrimitiveType.Cube, new Vector3(0f, 2.5f, 8.5f), new Vector3(18f, 6f, 0.5f), environment);

            Transform[] targetsToAnimate = new Transform[4];
            BeamPulseDriver[] pulses = new BeamPulseDriver[2];
            BeamPhysicsContacts[] contacts = new BeamPhysicsContacts[1];
            CreateContinuousStation(new Vector3(-6f, 1.2f, 0f), clean, targets, out targetsToAnimate[0], out pulses[0], out contacts[0]);
            CreateCurvedStation(new Vector3(-2f, 1.2f, 0f), soft, targets, out targetsToAnimate[1], out pulses[1]);
            CreateElectricalStation(new Vector3(2f, 1.2f, 0f), electrical, targets, false, out targetsToAnimate[2]);
            CreateElectricalStation(new Vector3(6f, 1.2f, 0f), electrical, targets, true, out targetsToAnimate[3]);

            GameObject controllerObject = new GameObject("Beam Kit Demo Controller");
            BeamKitDemoController controller = controllerObject.AddComponent<BeamKitDemoController>();
            SerializedObject serialized = new SerializedObject(controller);
            SetArray(serialized.FindProperty("movingTargets"), targetsToAnimate);
            SetArray(serialized.FindProperty("pulseDrivers"), pulses);
            SetArray(serialized.FindProperty("contactProbes"), contacts);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string scenePath = $"{AuthoringRoot}/Scenes/Beam Kit Demo.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            if (previous.IsValid())
                SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateContinuousStation(
            Vector3 position,
            Material beamMaterial,
            Material targetMaterial,
            out Transform targetTransform,
            out BeamPulseDriver pulse,
            out BeamPhysicsContacts contacts)
        {
            GameObject root = CreateBeamRoot("Continuous Beam", position, beamMaterial, targetMaterial, out TransformBeamEndpoint target);
            StraightBeamPath path = root.AddComponent<StraightBeamPath>();
            BeamResampleModifier resample = root.AddComponent<BeamResampleModifier>();
            Beam beam = Configure(root, target, path, new BeamPathModifier[] { resample });
            pulse = root.AddComponent<BeamPulseDriver>();
            contacts = root.AddComponent<BeamPhysicsContacts>();
            contacts.Beam = beam;
            targetTransform = target.transform;
        }

        private static void CreateCurvedStation(
            Vector3 position,
            Material beamMaterial,
            Material targetMaterial,
            out Transform targetTransform,
            out BeamPulseDriver pulse)
        {
            GameObject root = CreateBeamRoot("Curved Flow", position, beamMaterial, targetMaterial, out TransformBeamEndpoint target);
            target.transform.localPosition = new Vector3(0f, -0.5f, 5f);
            target.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            BezierBeamPath path = root.AddComponent<BezierBeamPath>();
            BeamSagModifier sag = root.AddComponent<BeamSagModifier>();
            Configure(root, target, path, new BeamPathModifier[] { sag });
            pulse = root.AddComponent<BeamPulseDriver>();
            targetTransform = target.transform;
        }

        private static void CreateElectricalStation(
            Vector3 position,
            Material beamMaterial,
            Material targetMaterial,
            bool branching,
            out Transform targetTransform)
        {
            GameObject root = CreateBeamRoot(branching ? "Branching Lightning" : "Electrical Arc", position, beamMaterial, targetMaterial, out TransformBeamEndpoint target);
            StraightBeamPath path = root.AddComponent<StraightBeamPath>();
            BeamElectricalModifier electrical = root.AddComponent<BeamElectricalModifier>();
            if (branching)
            {
                BeamBranchModifier branches = root.AddComponent<BeamBranchModifier>();
                Configure(root, target, path, new BeamPathModifier[] { branches, electrical });
            }
            else
            {
                Configure(root, target, path, new BeamPathModifier[] { electrical });
            }
            targetTransform = target.transform;
        }

        private static GameObject CreateBeamRoot(
            string name,
            Vector3 position,
            Material beamMaterial,
            Material targetMaterial,
            out TransformBeamEndpoint target)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;
            BeamRibbonRenderer renderer = root.AddComponent<BeamRibbonRenderer>();
            root.GetComponent<MeshRenderer>().sharedMaterial = beamMaterial;

            GameObject sourceMarker = CreatePrimitive("Source", PrimitiveType.Sphere, position, Vector3.one * 0.28f, targetMaterial);
            sourceMarker.transform.SetParent(root.transform, true);
            GameObject targetObject = CreatePrimitive("Target", PrimitiveType.Sphere, position + Vector3.forward * 5f, Vector3.one * 0.36f, targetMaterial);
            targetObject.transform.SetParent(root.transform, true);
            target = targetObject.AddComponent<TransformBeamEndpoint>();
            return root;
        }

        private static Beam Configure(GameObject root, BeamEndpointProvider target, BeamPathProvider path, BeamPathModifier[] modifiers)
        {
            Beam beam = root.AddComponent<Beam>();
            beam.Target = target;
            beam.PathProvider = path;
            beam.PathRenderer = root.GetComponent<BeamRibbonRenderer>();
            beam.Modifiers = modifiers;
            beam.Refresh();
            return beam;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 5.2f, -10f);
            cameraObject.transform.LookAt(new Vector3(0f, 1.2f, 3.2f));
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.008f, 0.012f, 0.022f);
            camera.fieldOfView = 54f;
            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(0.72f, 0.82f, 1f);
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            return gameObject;
        }

        private static Material Material(string name, Color color, float smoothness)
        {
            string path = $"{AuthoringRoot}/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadPackageMaterial(string guid)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static void PublishSample()
        {
            string sampleContainer = Path.GetDirectoryName(PackageSampleRoot);
            if (!string.IsNullOrEmpty(sampleContainer) && !Directory.Exists(sampleContainer))
                Directory.CreateDirectory(sampleContainer);
            if (Directory.Exists(PackageSampleRoot))
                FileUtil.DeleteFileOrDirectory(PackageSampleRoot);
            FileUtil.CopyFileOrDirectory(AuthoringRoot, PackageSampleRoot);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void SetArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
