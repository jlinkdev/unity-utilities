using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams.Editor
{
    public static class BeamCreationMenu
    {
        private const string DefaultMaterialGuid = "8040d74893194a53ae970188da1d76c4";

        public enum EnergyStyle { TripLaser, MiningPlasma, GoldenTether, TractorStream }

        [MenuItem("GameObject/jlinkdev/Beams/Trip Laser", false, 20)]
        private static void Trip(MenuCommand command) => Finish(CreateEnergyBeam(EnergyStyle.TripLaser, command));
        [MenuItem("GameObject/jlinkdev/Beams/Mining Plasma", false, 21)]
        private static void Mining(MenuCommand command) => Finish(CreateEnergyBeam(EnergyStyle.MiningPlasma, command));
        [MenuItem("GameObject/jlinkdev/Beams/Golden Healing Tether", false, 22)]
        private static void Tether(MenuCommand command) => Finish(CreateEnergyBeam(EnergyStyle.GoldenTether, command));
        [MenuItem("GameObject/jlinkdev/Beams/Tractor Stream", false, 23)]
        private static void Tractor(MenuCommand command) => Finish(CreateEnergyBeam(EnergyStyle.TractorStream, command));

        public static GameObject CreateEnergyBeam(EnergyStyle style, MenuCommand command = null)
        {
            bool curved = style == EnergyStyle.GoldenTether;
            string name;
            float width;
            switch (style)
            {
                case EnergyStyle.TripLaser: name = "Trip Laser"; width = 0.032f; break;
                case EnergyStyle.MiningPlasma: name = "Mining Plasma"; width = 0.44f; break;
                case EnergyStyle.GoldenTether: name = "Golden Tether"; width = 0.25f; break;
                default: name = "Tractor Stream"; width = 0.36f; break;
            }
            GameObject root = command == null ? new GameObject(name) : CreateRoot(name, command);
            var target = CreateTarget(root, Vector3.forward * 5f);
            target.transform.localRotation = Quaternion.Euler(curved ? -30f : 0f, 180f, 0f);
            var renderer = AddRenderer(root);
            renderer.VolumeTube = true;
            renderer.OrbitingFilaments = curved;
            renderer.Width = width;
            renderer.Color = Color.white;
            root.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                $"Packages/com.jlinkdev.beams/Runtime/Materials/{name}.mat");
            if (curved)
            {
                var smooth = target.gameObject.AddComponent<SmoothedBeamEndpoint>();
                smooth.Input = target;
                var sampling = root.AddComponent<BeamResampleModifier>();
                sampling.MaximumSegmentLength = 0.055f;
                Configure(root.AddComponent<Beam>(), smooth, root.AddComponent<BezierBeamPath>(), renderer,
                    new BeamPathModifier[] { root.AddComponent<BeamSagModifier>(), sampling });
                root.AddComponent<BeamPulseDriver>();
            }
            else
            {
                Configure(root.AddComponent<Beam>(), target, root.AddComponent<StraightBeamPath>(), renderer,
                    new BeamPathModifier[] { root.AddComponent<BeamResampleModifier>() });
            }
            Color tint = curved ? new Color(1,0.73f,0.13f) :
                style == EnergyStyle.TripLaser ? new Color(1,0.015f,0.02f) :
                style == EnergyStyle.MiningPlasma ? new Color(1,0.26f,0.035f) : new Color(0.1f,0.78f,1);
            BeamAccentAuthoring.Attach(root.GetComponent<Beam>(), tint, width * 4f, false);
            return root;
        }

        [MenuItem("GameObject/jlinkdev/Beams/Continuous Beam", false, 10)]
        public static void CreateContinuousBeam(MenuCommand command)
        {
            GameObject root = CreateRoot("Continuous Beam", command);
            TransformBeamEndpoint target = CreateTarget(root, new Vector3(0f, 0f, 5f));
            StraightBeamPath path = root.AddComponent<StraightBeamPath>();
            BeamResampleModifier resample = root.AddComponent<BeamResampleModifier>();
            BeamRibbonRenderer renderer = AddRenderer(root);
            Configure(root.AddComponent<Beam>(), target, path, renderer, new BeamPathModifier[] { resample });
            BeamAccentAuthoring.Attach(root.GetComponent<Beam>(),Color.cyan,0.44f,false);
            Finish(root);
        }

        [MenuItem("GameObject/jlinkdev/Beams/Curved Tether", false, 11)]
        public static void CreateCurvedBeam(MenuCommand command)
        {
            GameObject root = CreateRoot("Curved Tether", command);
            TransformBeamEndpoint target = CreateTarget(root, new Vector3(0f, -1f, 5f));
            target.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            BezierBeamPath path = root.AddComponent<BezierBeamPath>();
            BeamSagModifier sag = root.AddComponent<BeamSagModifier>();
            BeamRibbonRenderer renderer = AddRenderer(root);
            Configure(root.AddComponent<Beam>(), target, path, renderer, new BeamPathModifier[] { sag });
            BeamAccentAuthoring.Attach(root.GetComponent<Beam>(),new Color(0.3f,0.7f,1),0.5f,false);
            Finish(root);
        }

        [MenuItem("GameObject/jlinkdev/Beams/Electrical Arc", false, 12)]
        public static void CreateElectricalArc(MenuCommand command)
        {
            GameObject root = CreateRoot("Electrical Arc", command);
            TransformBeamEndpoint target = CreateTarget(root, new Vector3(0f, 0f, 5f));
            StraightBeamPath path = root.AddComponent<StraightBeamPath>();
            BeamElectricalModifier electrical = root.AddComponent<BeamElectricalModifier>();
            BeamRibbonRenderer renderer = AddRenderer(root);
            SetElectricalMaterial(root, renderer);
            Configure(root.AddComponent<Beam>(), target, path, renderer, new BeamPathModifier[] { electrical });
            BeamAccentAuthoring.Attach(root.GetComponent<Beam>(),new Color(0.45f,0.65f,1),0.6f,false);
            Finish(root);
        }

        [MenuItem("GameObject/jlinkdev/Beams/Branching Lightning", false, 13)]
        public static void CreateBranchingLightning(MenuCommand command)
        {
            GameObject root = CreateRoot("Branching Lightning", command);
            TransformBeamEndpoint target = CreateTarget(root, new Vector3(0f, 0f, 5f));
            StraightBeamPath path = root.AddComponent<StraightBeamPath>();
            BeamBranchModifier branches = root.AddComponent<BeamBranchModifier>();
            BeamElectricalModifier electrical = root.AddComponent<BeamElectricalModifier>();
            BeamRibbonRenderer renderer = AddRenderer(root);
            SetElectricalMaterial(root, renderer);
            Configure(root.AddComponent<Beam>(), target, path, renderer, new BeamPathModifier[] { branches, electrical });
            BeamAccentAuthoring.Attach(root.GetComponent<Beam>(),new Color(0.45f,0.65f,1),0.6f,false);
            Finish(root);
        }

        [MenuItem("Tools/jlinkdev/Beams/Validate Package Assets")]
        public static void ValidatePackageAssets()
        {
            Shader shader = Shader.Find(BeamRibbonRenderer.DefaultShaderName);
            Material material = LoadDefaultMaterial();
            if (shader == null)
                Debug.LogError($"[jlinkdev Beams] Shader not found: {BeamRibbonRenderer.DefaultShaderName}");
            else if (material == null)
                Debug.LogError("[jlinkdev Beams] Default Energy Beam material could not be loaded.");
            else if (material.shader != shader)
                Debug.LogError("[jlinkdev Beams] Default material does not reference the Energy Beam shader.");
            else
                Debug.Log("[jlinkdev Beams] Runtime assembly, shader, and default material are available.");
        }

        private static GameObject CreateRoot(string name, MenuCommand command)
        {
            GameObject root = new GameObject(name);
            GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(root, $"Create {name}");
            return root;
        }

        private static TransformBeamEndpoint CreateTarget(GameObject root, Vector3 localPosition)
        {
            GameObject targetObject = new GameObject("Beam Target");
            targetObject.transform.SetParent(root.transform, false);
            targetObject.transform.localPosition = localPosition;
            return targetObject.AddComponent<TransformBeamEndpoint>();
        }

        private static BeamRibbonRenderer AddRenderer(GameObject root)
        {
            BeamRibbonRenderer renderer = root.AddComponent<BeamRibbonRenderer>();
            Material material = LoadDefaultMaterial();
            if (material != null)
                root.GetComponent<MeshRenderer>().sharedMaterial = material;
            return renderer;
        }

        private static void Configure(
            Beam beam,
            BeamEndpointProvider target,
            BeamPathProvider path,
            BeamPathRenderer renderer,
            BeamPathModifier[] modifiers)
        {
            beam.Target = target;
            beam.PathProvider = path;
            beam.PathRenderer = renderer;
            beam.Modifiers = modifiers;
            beam.Refresh();
        }

        private static Material LoadDefaultMaterial()
        {
            return LoadMaterial(DefaultMaterialGuid);
        }

        private static void SetElectricalMaterial(GameObject root, BeamRibbonRenderer renderer)
        {
            renderer.Color = Color.white;
            root.GetComponent<MeshRenderer>().sharedMaterial = LoadMaterial("c39dcf68c97a46aba0a76635300e2fea");
        }

        private static Material LoadMaterial(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static void Finish(GameObject root)
        {
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }
    }
}
