using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace jlinkdev.UnityUtilities.Beams.Editor
{
    public static class BeamAccentAuthoring
    {
        public static void Attach(Beam beam, Color tint, float size, bool surface)
        {
            var source = LightCard(beam.transform, "Emitter glow", tint, size * 0.75f, false);
            var target = new GameObject("Contact light").transform;
            target.SetParent(beam.transform, false);
            var glow = LightCard(target, "Contact glow", tint, size, false);
            if (surface)
            {
                glow.localPosition = Vector3.forward * size * 0.09f;
                var footprint = LightCard(target, "Heated surface", tint, size * 2.2f, true);
                footprint.localPosition = Vector3.forward * 0.022f;
            }
            var visuals = beam.gameObject.AddComponent<BeamEndpointVisuals>();
            var serialized = new SerializedObject(visuals);
            serialized.FindProperty("sourceVisual").objectReferenceValue = source;
            serialized.FindProperty("targetVisual").objectReferenceValue = target;
            serialized.FindProperty("requireSurfaceForTarget").boolValue = surface;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            visuals.Refresh();
        }

        private static Transform LightCard(Transform parent, string name, Color tint, float size, bool surface)
        {
            var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(card.GetComponent<Collider>());
            card.name = name;
            card.transform.SetParent(parent, false);
            card.transform.localScale = Vector3.one * size;
            var renderer = card.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.jlinkdev.beams/Runtime/Materials/Endpoint Light.mat");
            var accent = card.AddComponent<BeamAccentTint>();
            accent.tint = tint;
            accent.surface = surface;
            accent.Apply();
            return card.transform;
        }
    }
}
