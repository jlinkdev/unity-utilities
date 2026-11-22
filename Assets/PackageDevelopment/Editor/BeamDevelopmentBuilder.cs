using System.IO;
using jlinkdev.UnityUtilities.Beams;
using jlinkdev.UnityUtilities.Beams.EditableSamples;
using jlinkdev.UnityUtilities.Beams.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace jlinkdev.UnityUtilities.Beams.Development
{
    public static partial class BeamDevelopmentBuilder
    {
        private const string AuthoringRoot = "Assets/PackageSamples/com.jlinkdev.beams/Beam Kit Demo";

        [MenuItem("Tools/jlinkdev/Beams/Rebuild Development Content")]
        public static void BuildAll()
        {
            EnsureFolder(AuthoringRoot);
            RebuildScene($"{AuthoringRoot}/Scenes/Beam Kit Demo.unity", BuildDemoScene);
            PackageSamplePublisher.Publish(AuthoringRoot);
            RebuildScene("Assets/PackageSamples/com.jlinkdev.beams/Energy Studies/Scenes/Energy Studies.unity", BuildEnergyStudies);
            RebuildScene(OrbitalRoot + "/Scenes/Orbital Descent.unity", BuildOrbital);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[jlinkdev Beams] Beam Kit Demo rebuilt and published to Samples~.");
        }

        private static void RebuildScene(string path, System.Action build)
        {
            var open = SceneManager.GetSceneByPath(path);
            if (!open.isLoaded) { build(); return; }
            if (open.isDirty) throw new IOException($"Save the open sample before rebuilding: {path}");
            bool active = SceneManager.GetActiveScene() == open;
            var scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            string scratchPath = AssetDatabase.GenerateUniqueAssetPath("Assets/PackageDevelopment/Builder Staging.unity");
            if (!EditorSceneManager.SaveScene(scratch, scratchPath))
            {
                EditorSceneManager.CloseScene(scratch, true);
                throw new IOException(scratchPath);
            }
            EditorSceneManager.CloseScene(open, true);
            try { build(); }
            finally
            {
                var restored = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (active) SceneManager.SetActiveScene(restored);
                EditorSceneManager.CloseScene(scratch, true);
                AssetDatabase.DeleteAsset(scratchPath);
            }
        }

        private static void BuildDemoScene()
        {
            EnsureFolder($"{AuthoringRoot}/Scenes");
            EnsureFolder($"{AuthoringRoot}/Materials");

            Material targets = Material("Demo Targets", new Color(0.16f, 0.22f, 0.3f), 0.75f);
            Material clean = LoadPackageMaterial("5ae079f5b395424ba626c300a2a069d4");
            Material flow = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.jlinkdev.beams/Runtime/Materials/Ribbon Flow.mat");
            Material electrical = LoadPackageMaterial("c39dcf68c97a46aba0a76635300e2fea");

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            scene.name = "Beam Kit Demo";

            CreateCamera();
            DemoBloom(CameraIn(scene), AuthoringRoot, 0.55f);
            CreateLight();
            RenderSettings.fog = false;

            Transform[] targetsToAnimate = new Transform[4];
            BeamPulseDriver[] pulses = new BeamPulseDriver[2];
            BeamPhysicsContacts[] contacts = new BeamPhysicsContacts[1];
            CreateContinuousStation(new Vector3(-2.5f, 2.2f, 0), clean, targets, out targetsToAnimate[0], out pulses[0], out contacts[0]);
            CreateCurvedStation(new Vector3(-2.5f, 1.0f, 0), flow, targets, out targetsToAnimate[1], out pulses[1]);
            CreateElectricalStation(new Vector3(-2.5f, -0.45f, 0), electrical, targets, false, out targetsToAnimate[2]);
            CreateElectricalStation(new Vector3(-2.5f, -2.05f, 0), electrical, targets, true, out targetsToAnimate[3]);

            GameObject controllerObject = new GameObject("Beam Kit Demo Controller");
            BeamKitDemoController controller = controllerObject.AddComponent<BeamKitDemoController>();
            SerializedObject serialized = new SerializedObject(controller);
            SetArray(serialized.FindProperty("movingTargets"), targetsToAnimate);
            SetArray(serialized.FindProperty("pulseDrivers"), pulses);
            SetArray(serialized.FindProperty("contactProbes"), contacts);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string scenePath = $"{AuthoringRoot}/Scenes/Beam Kit Demo.unity";
            SampleScenePresentation.Apply(scene, AuthoringRoot);
            if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException(scenePath);
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
            var indicator = CreatePrimitive("Contact Indicator", PrimitiveType.Cube, position + new Vector3(2.5f, 0.22f, 0), Vector3.one * 0.32f, targetMaterial);
            indicator.layer = 2;
            contacts.LayerMask = 1 << indicator.layer;
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
            root.transform.rotation = Quaternion.Euler(0,90,0);
            BeamRibbonRenderer renderer = root.AddComponent<BeamRibbonRenderer>();
            renderer.Color = name == "Curved Flow" ? new Color(.35f,.65f,1f) : Color.white;
            renderer.Width = name.Contains("Lightning") || name.Contains("Arc") ? 0.14f : 0.19f;
            renderer.Intensity = 2;
            root.GetComponent<MeshRenderer>().sharedMaterial = beamMaterial;

            GameObject targetObject = new GameObject("Target");
            targetObject.transform.SetParent(root.transform, false);
            targetObject.transform.localPosition = Vector3.forward * 5f;
            target = targetObject.AddComponent<TransformBeamEndpoint>();
            return root;
        }

        private static Beam Configure(GameObject root, BeamEndpointProvider target, BeamPathProvider path, BeamPathModifier[] modifiers)
        {
            Beam beam = root.AddComponent<Beam>();
            if (root.name == "Branching Lightning") beam.Seed = 37;
            beam.Target = target;
            beam.PathProvider = path;
            beam.PathRenderer = root.GetComponent<BeamRibbonRenderer>();
            beam.Modifiers = modifiers;
            BeamAccentAuthoring.Attach(beam, root.name.Contains("Lightning") || root.name.Contains("Arc") ?
                new Color(0.38f,0.58f,1f) : new Color(0.18f,0.9f,1f), 0.72f, false);
            beam.Refresh();
            return beam;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f,0f,-8f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.008f, 0.012f, 0.022f);
            camera.fieldOfView = 54f;
            camera.orthographic = true;
            camera.orthographicSize = 3.35f;
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

        private static void BuildEnergyStudies()
        {
            const string root = "Assets/PackageSamples/com.jlinkdev.beams/Energy Studies";
            foreach (var folder in new[] { "Scenes", "Prefabs", "Animations" }) EnsureFolder(root + "/" + folder);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera = new GameObject("Study Camera", typeof(Camera)).GetComponent<Camera>();
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 0, -8);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.022f, 0.037f);
                camera.orthographic = true;
                camera.orthographicSize = 3.25f;
                DemoBloom(camera, root, 0.65f);
                foreach (BeamCreationMenu.EnergyStyle style in System.Enum.GetValues(typeof(BeamCreationMenu.EnergyStyle)))
                {
                    GameObject beam = BeamCreationMenu.CreateEnergyBeam(style);
                    PrefabUtility.SaveAsPrefabAssetAndConnect(beam, $"{root}/Prefabs/{beam.name}.prefab", InteractionMode.AutomatedAction);
                    float y = 2f - (int)style * 1.32f;
                    beam.transform.SetPositionAndRotation(new Vector3(-2.5f, y, 0), Quaternion.Euler(0, 90, 0));
                    if (style == BeamCreationMenu.EnergyStyle.GoldenTether)
                    {
                        const string clipPath = root + "/Animations/Tether Motion.anim";
                        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                        if (clip == null)
                        {
                            clip = new AnimationClip { legacy = true };
                            AssetDatabase.CreateAsset(clip, clipPath);
                        }
                        clip.wrapMode = WrapMode.PingPong;
                        var rest = beam.transform.Find("Beam Target").localPosition;
                        clip.SetCurve("Beam Target", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0,1.6f,rest.x));
                        clip.SetCurve("Beam Target", typeof(Transform), "localPosition.y", AnimationCurve.EaseInOut(0, -0.3f, 1.6f, 0.4f));
                        clip.SetCurve("Beam Target", typeof(Transform), "localPosition.z", AnimationCurve.Constant(0,1.6f,rest.z));
                        EditorUtility.SetDirty(clip);
                        var motion = beam.AddComponent<Animation>();
                        motion.AddClip(clip, "Follow");
                        motion.clip = clip;
                    }
                    var label = new GameObject(beam.name + " Label").AddComponent<TextMesh>();
                    label.transform.position = new Vector3(-2.5f, y + 0.52f, 0);
                    label.text = beam.name;
                    label.characterSize = 0.60f;
                    label.fontSize = 40;
                    label.transform.localScale = Vector3.one * 0.12f;
                    beam.GetComponent<Beam>().Refresh();
                }
                SampleScenePresentation.Apply(scene, root);
                if (!EditorSceneManager.SaveScene(scene, root + "/Scenes/Energy Studies.unity")) throw new IOException(root);
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            PackageSamplePublisher.Publish(root);
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
