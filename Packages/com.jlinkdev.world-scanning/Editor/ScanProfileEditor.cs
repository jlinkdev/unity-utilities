using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.WorldScanning.Editor
{
    [CustomEditor(typeof(ScanProfile))]
    internal sealed class ScanProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "A profile is sampled into each emitted pulse. Runtime edits affect active scans, which makes profile tuning responsive in Play mode.",
                MessageType.Info);
            DrawDefaultInspector();
        }
    }
}
