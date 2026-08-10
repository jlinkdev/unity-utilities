using System.IO;
using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals.Editor
{
    internal static class PortalPairMenu
    {
        private const string AssetRoot = "Assets/jlinkdev Portals";

        [MenuItem("GameObject/jlinkdev/Portals/Create Linked Portal Pair", false, 10)]
        private static void CreateLinkedPortalPair(MenuCommand command)
        {
            EnsureAssetFolder();
            PortalRenderSettings settings = GetOrCreateSettings();
            Material surfaceMaterial = GetOrCreateSurfaceMaterial();

            GameObject pairRoot = new GameObject("Linked Portal Pair");
            Undo.RegisterCreatedObjectUndo(pairRoot, "Create Linked Portal Pair");
            GameObjectUtility.SetParentAndAlign(pairRoot, command.context as GameObject);

            Portal first = CreatePortal("Portal A", pairRoot.transform, new Vector3(-2f, 1.75f, 0f), Quaternion.identity, settings, surfaceMaterial);
            Portal second = CreatePortal("Portal B", pairRoot.transform, new Vector3(2f, 1.75f, 4f), Quaternion.Euler(0f, 180f, 0f), settings, surfaceMaterial);
            first.LinkedPortal = second;
            second.LinkedPortal = first;
            EditorUtility.SetDirty(first);
            EditorUtility.SetDirty(second);
            Selection.activeGameObject = pairRoot;
        }

        private static Portal CreatePortal(string name, Transform parent, Vector3 position, Quaternion rotation, PortalRenderSettings settings, Material surfaceMaterial)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2f, 3.5f, 0.8f);

            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "Portal Surface";
            surface.transform.SetParent(root.transform, false);
            surface.transform.localScale = new Vector3(2f, 3.5f, 1f);
            Object.DestroyImmediate(surface.GetComponent<Collider>());
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = surfaceMaterial;

            Portal portal = root.AddComponent<Portal>();
            portal.SurfaceRenderer = renderer;
            portal.TraversalTrigger = trigger;
            portal.RenderSettings = settings;
            return portal;
        }

        private static void EnsureAssetFolder()
        {
            if (!AssetDatabase.IsValidFolder(AssetRoot))
                AssetDatabase.CreateFolder("Assets", "jlinkdev Portals");
            if (!AssetDatabase.IsValidFolder($"{AssetRoot}/Materials"))
                AssetDatabase.CreateFolder(AssetRoot, "Materials");
            if (!AssetDatabase.IsValidFolder($"{AssetRoot}/Settings"))
                AssetDatabase.CreateFolder(AssetRoot, "Settings");
        }

        private static PortalRenderSettings GetOrCreateSettings()
        {
            string path = $"{AssetRoot}/Settings/Default Portal Render Settings.asset";
            PortalRenderSettings settings = AssetDatabase.LoadAssetAtPath<PortalRenderSettings>(path);
            if (settings != null)
                return settings;

            settings = ScriptableObject.CreateInstance<PortalRenderSettings>();
            AssetDatabase.CreateAsset(settings, path);
            return settings;
        }

        private static Material GetOrCreateSurfaceMaterial()
        {
            string path = $"{AssetRoot}/Materials/Portal Surface.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            Shader shader = Shader.Find("jlinkdev/Portals/Portal Surface");
            material = new Material(shader) { name = "Portal Surface" };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
