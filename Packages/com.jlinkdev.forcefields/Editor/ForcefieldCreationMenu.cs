using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields.Editor
{
    internal static class ForcefieldCreationMenu
    {
        private const string MaterialPath = "Packages/com.jlinkdev.forcefields/Runtime/Materials/Forcefield.mat";
        private const string DefaultPresetPath = "Packages/com.jlinkdev.forcefields/Runtime/Presets/Clean Energy.asset";

        [MenuItem("GameObject/jlinkdev/Forcefields/Create Forcefield Sphere", false, 10)]
        private static void CreateSphere(MenuCommand command)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Forcefield Sphere";
            sphere.transform.localScale = Vector3.one * 4f;
            GameObjectUtility.SetParentAndAlign(sphere, command.context as GameObject);

            Renderer renderer = sphere.GetComponent<Renderer>();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                renderer.sharedMaterial = material;

            Forcefield forcefield = Undo.AddComponent<Forcefield>(sphere);
            forcefield.TargetRenderers = new[] { renderer };
            forcefield.ApplyPreset(AssetDatabase.LoadAssetAtPath<ForcefieldPreset>(DefaultPresetPath));
            forcefield.Refresh();

            Undo.RegisterCreatedObjectUndo(sphere, "Create Forcefield Sphere");
            Selection.activeGameObject = sphere;
        }

        [MenuItem("GameObject/jlinkdev/Forcefields/Add Forcefield to Selected Renderer", false, 11)]
        private static void AddToSelected(MenuCommand command)
        {
            GameObject selected = command.context as GameObject;
            if (selected == null)
                selected = Selection.activeGameObject;
            if (selected == null)
                return;

            Renderer renderer = selected.GetComponent<Renderer>();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("Add Forcefield", "The selected GameObject does not have a Renderer.", "OK");
                return;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                Undo.RecordObject(renderer, "Assign Forcefield Material");
                renderer.sharedMaterial = material;
            }

            Forcefield forcefield = selected.GetComponent<Forcefield>();
            if (forcefield == null)
                forcefield = Undo.AddComponent<Forcefield>(selected);
            Undo.RecordObject(forcefield, "Configure Forcefield");
            forcefield.TargetRenderers = new[] { renderer };
            forcefield.ApplyPreset(AssetDatabase.LoadAssetAtPath<ForcefieldPreset>(DefaultPresetPath));
            forcefield.Refresh();
            EditorUtility.SetDirty(forcefield);
        }

        [MenuItem("GameObject/jlinkdev/Forcefields/Add Forcefield to Selected Renderer", true)]
        private static bool ValidateAddToSelected()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Renderer>() != null;
        }
    }
}
