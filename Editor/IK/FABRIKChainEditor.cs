using jlinkdev.UnityUtilities.IK;
using UnityEditor;

namespace jlinkdev.UnityUtilities.Editor.IK
{
    [CustomEditor(typeof(FABRIKChain))]
    public sealed class FABRIKChainEditor : UnityEditor.Editor
    {
        private SerializedProperty _joints;
        private SerializedProperty _target;
        private SerializedProperty _pole;
        private SerializedProperty _iterations;
        private SerializedProperty _tolerance;
        private SerializedProperty _lockRoot;
        private SerializedProperty _weight;
        private SerializedProperty _solveInLateUpdate;
        private SerializedProperty _drawGizmos;

        private void OnEnable()
        {
            _joints = serializedObject.FindProperty("joints");
            _target = serializedObject.FindProperty("target");
            _pole = serializedObject.FindProperty("pole");
            _iterations = serializedObject.FindProperty("iterations");
            _tolerance = serializedObject.FindProperty("tolerance");
            _lockRoot = serializedObject.FindProperty("lockRoot");
            _weight = serializedObject.FindProperty("weight");
            _solveInLateUpdate = serializedObject.FindProperty("solveInLateUpdate");
            _drawGizmos = serializedObject.FindProperty("drawGizmos");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_joints);
            EditorGUILayout.PropertyField(_target);
            EditorGUILayout.PropertyField(_pole);
            EditorGUILayout.PropertyField(_iterations);
            EditorGUILayout.PropertyField(_tolerance);
            EditorGUILayout.PropertyField(_lockRoot);
            EditorGUILayout.PropertyField(_weight);
            EditorGUILayout.PropertyField(_solveInLateUpdate);
            EditorGUILayout.PropertyField(_drawGizmos);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
