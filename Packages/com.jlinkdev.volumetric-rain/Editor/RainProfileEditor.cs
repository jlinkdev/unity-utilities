using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricRain.Editor
{
    [CustomEditor(typeof(RainProfile)), CanEditMultipleObjects]
    public sealed class RainProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            bool children = true;
            while (property.NextVisible(children))
            {
                children = false;
                bool distance = property.name == "nearFade" || property.name == "midDistance" ||
                    property.name == "farDistance" || property.name == "maxDistance";
                if (distance)
                {
                    if (property.name == "nearFade")
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
                        EditorGUILayout.PropertyField(property,true);
                }
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("Distances are measured from the viewing camera. Press Enter or leave a distance field to commit it. Editing one distance does not change the others.",MessageType.Info);
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
