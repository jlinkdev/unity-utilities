using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.WorldScanning.Editor
{
    [CustomEditor(typeof(ScanEmitter))]
    [CanEditMultipleObjects]
    internal sealed class ScanEmitterEditor : UnityEditor.Editor
    {
        private SerializedProperty profile;
        private SerializedProperty rangeMultiplier;
        private SerializedProperty shapeOverride;

        private void OnEnable()
        {
            profile = serializedObject.FindProperty("profile");
            rangeMultiplier = serializedObject.FindProperty("rangeMultiplier");
            shapeOverride = serializedObject.FindProperty("shapeOverride");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Space(2f);
            EditorGUILayout.HelpBox(
                "Emitters create lightweight world-space pulses. Visual styling lives in the assigned Scan Profile; the renderer feature composites every active pulse once per camera.",
                MessageType.Info);
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(!Application.isPlaying || targets.Length != 1 || profile.objectReferenceValue == null))
            {
                if (GUILayout.Button("Emit Test Scan", GUILayout.Height(26f)))
                    ((ScanEmitter)target).Emit();
            }

            if (profile.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign a Scan Profile before emitting.", MessageType.Warning);
        }

        private void OnSceneGUI()
        {
            ScanEmitter emitter = (ScanEmitter)target;
            ScanProfile assignedProfile = profile.objectReferenceValue as ScanProfile;
            if (assignedProfile == null)
                return;

            float radius = assignedProfile.Range * rangeMultiplier.floatValue;
            ScanShape shape = assignedProfile.Shape;
            if (shapeOverride.enumValueIndex == 1)
                shape = ScanShape.Sphere;
            else if (shapeOverride.enumValueIndex == 2)
                shape = ScanShape.Cylinder;

            Handles.color = new Color(0.05f, 0.85f, 1f, 0.75f);
            if (shape == ScanShape.Sphere)
            {
                Handles.DrawWireDisc(emitter.Origin, Vector3.up, radius);
                Handles.DrawWireDisc(emitter.Origin, Vector3.right, radius);
                Handles.DrawWireDisc(emitter.Origin, Vector3.forward, radius);
            }
            else
            {
                Vector3 axis = emitter.Axis.normalized;
                float halfHeight = assignedProfile.CylinderHalfHeight;
                Vector3 top = emitter.Origin + axis * halfHeight;
                Vector3 bottom = emitter.Origin - axis * halfHeight;
                Handles.DrawWireDisc(top, axis, radius);
                Handles.DrawWireDisc(bottom, axis, radius);
                Vector3 tangent = Vector3.Cross(axis, Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up).normalized;
                Vector3 bitangent = Vector3.Cross(axis, tangent).normalized;
                Handles.DrawLine(top + tangent * radius, bottom + tangent * radius);
                Handles.DrawLine(top - tangent * radius, bottom - tangent * radius);
                Handles.DrawLine(top + bitangent * radius, bottom + bitangent * radius);
                Handles.DrawLine(top - bitangent * radius, bottom - bitangent * radius);
            }
        }
    }
}
