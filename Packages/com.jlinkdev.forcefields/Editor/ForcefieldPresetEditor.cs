using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields.Editor
{
    [CustomEditor(typeof(ForcefieldPreset))]
    internal sealed class ForcefieldPresetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Presets are renderer-independent. Changes are reflected by forcefields using this asset without creating material instances.",
                MessageType.Info);
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (!EditorGUI.EndChangeCheck())
                return;

            ForcefieldPreset changedPreset = (ForcefieldPreset)target;
            Forcefield[] forcefields = Resources.FindObjectsOfTypeAll<Forcefield>();
            for (int i = 0; i < forcefields.Length; i++)
            {
                if (forcefields[i] != null && forcefields[i].Preset == changedPreset)
                    forcefields[i].Refresh();
            }

            SceneView.RepaintAll();
        }
    }
}
