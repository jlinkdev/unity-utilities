using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams.Editor
{
    public static class BeamCreationMenu
    {
        private const string DefaultMaterialGuid = "8040d74893194a53ae970188da1d76c4";

        [MenuItem("GameObject/jlinkdev/Beams/Continuous Beam", false, 10)]
        public static void CreateContinuousBeam(MenuCommand command)
        {
            GameObject root = CreateRoot("Continuous Beam", command);
            TransformBeamEndpoint target = CreateTarget(root, new Vector3(0f, 0f, 5f));
            StraightBeamPath path = root.AddComponent<StraightBeamPath>();
            BeamResampleModifier resample = root.AddComponent<BeamResampleModifier>();
            BeamRibbonRenderer renderer = AddRenderer(root);
            Configure(root.AddComponent<Beam>(), target, path, renderer, new BeamPathModifier[] { resample });
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
            Configure(root.AddComponent<Beam>(), target, path, renderer, new BeamPathModifier[] { electrical });
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
            Configure(root.AddComponent<Beam>(), target, path, renderer, new BeamPathModifier[] { branches, electrical });
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
            string path = AssetDatabase.GUIDToAssetPath(DefaultMaterialGuid);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static void Finish(GameObject root)
        {
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }
    }
}
