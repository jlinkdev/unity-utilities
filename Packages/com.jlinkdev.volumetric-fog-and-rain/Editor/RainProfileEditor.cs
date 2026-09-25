using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain.Editor
{
    [CustomEditor(typeof(RainProfile)), CanEditMultipleObjects]
    public sealed class RainProfileEditor : UnityEditor.Editor
    {
        private static readonly System.Collections.Generic.HashSet<string> RainFields = new System.Collections.Generic.HashSet<string>
        { "density", "cellSize", "streakLength", "streakWidth", "fallSpeed", "direction", "wind", "nearFade", "midDistance", "farDistance", "maxCellSteps", "rainColor", "brightness", "streakOpacity", "scattering" };
        private static readonly System.Collections.Generic.HashSet<string> FogFields = new System.Collections.Generic.HashSet<string>
        { "hazeExtinction", "hazeSteps", "scattering", "independentFogSettings", "fogDensity", "fogColor", "fogBrightness", "fogScattering", "fogDistanceMode", "fogStartDistance", "fogDistanceFade", "fogMaxDistance", "fogBaseHeight", "fogHeightFalloff", "fogNoiseVelocity", "directionalScattering", "scatteringAnisotropy" };
        private static readonly System.Collections.Generic.HashSet<string> IndependentFogFields = new System.Collections.Generic.HashSet<string>
        { "fogDensity", "fogColor", "fogBrightness", "fogScattering" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var modeProperty = serializedObject.FindProperty("renderMode");
            bool mixedMode = modeProperty.hasMultipleDifferentValues;
            var mode = (RainRenderMode)modeProperty.enumValueIndex;
            bool fogOnly = !mixedMode && mode == RainRenderMode.FogOnly;
            bool rainOnly = !mixedMode && mode == RainRenderMode.RainOnly;
            var separateProperty = serializedObject.FindProperty("independentFogSettings");
            bool separateFog = mixedMode || fogOnly || separateProperty.hasMultipleDifferentValues || separateProperty.boolValue;
            var distanceMode = serializedObject.FindProperty("fogDistanceMode");
            bool independentDistance = mixedMode || fogOnly || distanceMode.hasMultipleDifferentValues || distanceMode.enumValueIndex == (int)FogDistanceMode.Independent;
            var property = serializedObject.GetIterator();
            bool children = true;
            while (property.NextVisible(children))
            {
                children = false;
                if (fogOnly && (property.name == "fogDistanceMode" || property.name == "fogMaxDistance")) continue;
                if (!independentDistance && (property.name == "fogStartDistance" || property.name == "fogDistanceFade" || property.name == "fogMaxDistance")) continue;
                if (fogOnly && RainFields.Contains(property.name)) continue;
                if (rainOnly && FogFields.Contains(property.name)) continue;
                if (!separateFog && IndependentFogFields.Contains(property.name)) continue;
                if (fogOnly && property.name == "independentFogSettings") continue;
                if (separateFog && property.name == "scattering") continue;
                if (fogOnly && property.name == "fogDensity")
                    EditorGUILayout.LabelField("Fog appearance", EditorStyles.boldLabel);
                bool distance = property.name == "nearFade" || property.name == "midDistance" ||
                    property.name == "farDistance" || property.name == "maxDistance" || property.name == "fogStartDistance" || property.name == "fogDistanceFade" || property.name == "fogMaxDistance";
                if (distance)
                {
                    if (property.name == "nearFade" || (fogOnly && property.name == "maxDistance"))
                        EditorGUILayout.LabelField("Distance and integration", EditorStyles.boldLabel);
                    var rect = EditorGUILayout.GetControlRect();
                    EditorGUI.BeginProperty(rect, GUIContent.none, property);
                    EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                    EditorGUI.BeginChangeCheck();
                    float value = EditorGUI.DelayedFloatField(rect,new GUIContent(property.displayName,property.tooltip),property.floatValue);
                    if (EditorGUI.EndChangeCheck()) property.floatValue = value;
                    EditorGUI.showMixedValue = false;
                    EditorGUI.EndProperty();
                }
                else
                {
                    using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                        EditorGUILayout.PropertyField(property,
                            property.name == "hazeExtinction" ? new GUIContent("Fog Extinction", "Extinction per metre, multiplied by fog density.") :
                            property.name == "hazeSteps" ? new GUIContent("Fog Samples", "Density integration samples per pixel; volume fragments can add samples.") :
                            new GUIContent(property.displayName,property.tooltip),true);
                }
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("Distances are measured from the viewing camera. Press Enter or leave a distance field to commit it. Editing one distance does not change the others.",MessageType.Info);
            if (fogOnly)
            {
                EditorGUILayout.HelpBox("Fog Only uses Fog Start Distance and Fog Distance Fade up to Max Distance, independently of rain. Fog appearance is independent of the rain controls. Rain Volume and Rain Exclusion Volume components define its bounds.",MessageType.Info);
                return;
            }
            if (targets.Length != 1) return;
            var profile = (RainProfile)target;
            float mid = Mathf.Max(profile.nearFade + .1f,profile.midDistance);
            float far = Mathf.Max(mid + .1f,profile.farDistance);
            float end = Mathf.Max(far,profile.maxDistance);
            if (mid != profile.midDistance || far != profile.farDistance || end != profile.maxDistance)
                EditorGUILayout.HelpBox($"Distances must satisfy Near < Mid < Far <= Max. Until corrected, rendering uses Mid {mid:F1}, Far {far:F1}, Max {end:F1} m; the asset keeps your entered values.",MessageType.Warning);
            float minimumRange = (Mathf.Clamp(profile.maxCellSteps,16,256)-6)*Mathf.Max(.1f,profile.cellSize)/new Vector3(1,1f/3,1).magnitude;
            if (minimumRange < far)
                EditorGUILayout.HelpBox($"At some viewing angles, the cell budget can shorten streak range to about {minimumRange:F1} m for a single wet interval. Increase Max Cell Steps or Cell Size to reach Far Distance. Multiple wet intervals may shorten it further.",MessageType.Warning);
        }
    }
}
