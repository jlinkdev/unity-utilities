using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals.Editor
{
    [CustomEditor(typeof(Portal))]
    [CanEditMultipleObjects]
    internal sealed class PortalEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            if (targets.Length != 1)
                return;

            Portal portal = (Portal)target;
            EditorGUILayout.Space();
            if (portal.LinkedPortal == null)
                EditorGUILayout.HelpBox("Assign a linked portal before rendering or traversal can begin.", MessageType.Warning);
            else if (portal.LinkedPortal.LinkedPortal != portal)
                EditorGUILayout.HelpBox("The link is not reciprocal. Use Make Pair Reciprocal to repair it.", MessageType.Warning);

            if (portal.SurfaceRenderer == null)
                EditorGUILayout.HelpBox("A surface renderer is required for the portal view.", MessageType.Error);
            if (portal.TraversalTrigger == null)
                EditorGUILayout.HelpBox("A trigger collider is required for traversal.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (portal.LinkedPortal != null && GUILayout.Button("Select Linked Portal"))
                    Selection.activeGameObject = portal.LinkedPortal.gameObject;
                if (portal.LinkedPortal != null && portal.LinkedPortal.LinkedPortal != portal && GUILayout.Button("Make Pair Reciprocal"))
                {
                    Undo.RecordObject(portal.LinkedPortal, "Make Portal Pair Reciprocal");
                    portal.LinkedPortal.LinkedPortal = portal;
                    EditorUtility.SetDirty(portal.LinkedPortal);
                }
            }
        }
    }
}
