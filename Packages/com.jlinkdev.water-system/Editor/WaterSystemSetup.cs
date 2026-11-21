using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.WaterSystem.Editor
{
    /// <summary>Explicit setup actions. No renderer or project settings change on import.</summary>
    public static class WaterSystemSetup
    {
        [MenuItem("GameObject/jlinkdev/Water System/Water Body", false, 10)]
        public static void CreateWaterBody(MenuCommand command)
        {
            var gameObject = new GameObject("Water Body");
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Water Body");
            // Water surfaces use world-space horizontal coordinates; do not inherit transformed parents.
            var body = Undo.AddComponent<WaterBody>(gameObject);
            body.Size = new Vector2(24f, 24f);
            if (Selection.activeObject is WaterProfile profile)
                body.Profile = profile;
            body.Rebuild();
            Selection.activeGameObject = gameObject;
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        [MenuItem("Tools/jlinkdev/Water System/Add Feature to Selected Renderer")]
        public static void AddRendererFeature()
        {
            var renderer = Selection.activeObject as UniversalRendererData;
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("Select a Universal Renderer", "Select the Universal Renderer Data asset used by your camera, then run this command again. The pipeline asset itself is not the renderer data.", "OK");
                return;
            }
            foreach (var existing in renderer.rendererFeatures)
            {
                if (existing is WaterRendererFeature)
                {
                    Selection.activeObject = existing;
                    EditorGUIUtility.PingObject(existing);
                    return;
                }
            }
            Undo.RecordObject(renderer, "Add Water Renderer Feature");
            var feature = ScriptableObject.CreateInstance<WaterRendererFeature>();
            feature.name = "Water Renderer Feature";
            Undo.RegisterCreatedObjectUndo(feature, "Add Water Renderer Feature");
            AssetDatabase.AddObjectToAsset(feature, renderer);
            renderer.rendererFeatures.Add(feature);
            SynchronizeFeatureMap(renderer);
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            Selection.activeObject = renderer;
        }

        /// <summary>Keep URP's recovery map aligned with renderer feature sub-assets.</summary>
        public static void SynchronizeFeatureMap(UniversalRendererData renderer)
        {
            var serialized = new SerializedObject(renderer);
            var map = serialized.FindProperty("m_RendererFeatureMap");
            if (map == null) return;
            map.arraySize = renderer.rendererFeatures.Count;
            for (int i = 0; i < renderer.rendererFeatures.Count; i++)
            {
                var feature = renderer.rendererFeatures[i];
                long localId = 0;
                if (feature != null)
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string guid, out localId);
                map.GetArrayElementAtIndex(i).longValue = localId;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Assets/Create/jlinkdev/Water System/Calm Water Profile")]
        private static void CreateCalm() => CreateProfile(WaterPreset.Calm);

        [MenuItem("Assets/Create/jlinkdev/Water System/Coastal Water Profile")]
        private static void CreateCoastal() => CreateProfile(WaterPreset.Coastal);

        [MenuItem("Assets/Create/jlinkdev/Water System/Stylized Water Profile")]
        private static void CreateStylized() => CreateProfile(WaterPreset.Stylized);

        private static void CreateProfile(WaterPreset preset)
        {
            var profile = ScriptableObject.CreateInstance<WaterProfile>();
            ApplyPreset(profile, preset);
            ProjectWindowUtil.CreateAsset(profile, $"{preset} Water Profile.asset");
        }

        public enum WaterPreset { Calm, Coastal, Stylized }

        /// <summary>Assign a complete preset to a new profile. Existing assets require caller-managed Undo.</summary>
        public static void ApplyPreset(WaterProfile profile, WaterPreset preset)
        {
            profile.ShallowColor = new Color(0.12f, 0.62f, 0.65f, 0.62f);
            profile.DeepColor = new Color(0.015f, 0.12f, 0.2f, 0.95f);
            profile.FogColor = new Color(0.035f, 0.25f, 0.31f, 1f);
            profile.Absorption = new Vector3(0.18f, 0.055f, 0.03f);
            profile.Density = 0.055f;
            profile.Refraction = 0.008f;
            profile.Smoothness = 0.9f;
            profile.FoamColor = new Color(0.85f, 0.98f, 1f, 1f);
            profile.FoamWidth = 0.4f;
            profile.FoamStrength = 0.6f;
            profile.DetailNormalStrength = 0.035f;
            profile.UnderwaterSaturation = 0.82f;
            profile.Distortion = 0.0008f;
            profile.WaterlineColor = new Color(0.1f, 0.66f, 0.69f, 1f);
            profile.WaterlineWidth = 3f;
            profile.WaterlineStrength = 0.6f;
            profile.CausticsStrength = 0.4f;
            profile.CausticsScale = 0.75f;
            profile.CausticsSpeed = 0.25f;
            profile.Waves = new[] { new WaveSettings(30f, 0.1f, 5f, 0.7f), new WaveSettings(115f, 0.045f, 2.7f, 0.4f, 1.2f) };
            if (preset == WaterPreset.Coastal)
            {
                profile.Waves = new[] { new WaveSettings(15f, 0.36f, 8f, 1.4f), new WaveSettings(80f, 0.14f, 4.4f, 0.9f, 1.5f), new WaveSettings(155f, 0.065f, 2.3f, 0.55f, 2.4f) };
                profile.ShallowColor = new Color(0.08f, 0.48f, 0.51f, 0.68f);
                profile.FoamStrength = 0.9f;
                profile.DetailNormalStrength = 0.08f;
                profile.Refraction = 0.016f;
                profile.Absorption = new Vector3(0.24f, 0.07f, 0.045f);
                profile.Distortion = 0.0015f;
                profile.Density = 0.10f;
                profile.CausticsStrength = 0.5f;
            }
            else if (preset == WaterPreset.Stylized)
            {
                profile.Waves = new[] { new WaveSettings(35f, 0.2f, 6f, 0.85f), new WaveSettings(105f, 0.06f, 2.8f, 0.5f, 1.3f) };
                profile.ShallowColor = new Color(0.08f, 0.78f, 0.85f, 0.72f);
                profile.DeepColor = new Color(0.025f, 0.20f, 0.48f, 0.96f);
                profile.FogColor = new Color(0.025f, 0.36f, 0.49f, 1f);
                profile.FoamWidth = 0.6f;
                profile.FoamStrength = 1f;
                profile.WaterlineWidth = 6f;
                profile.WaterlineStrength = 0.9f;
                profile.CausticsStrength = 1f;
                profile.Refraction = 0.012f;
                profile.Distortion = 0.0012f;
                profile.Density = 0.08f;
            }
        }
    }
}
