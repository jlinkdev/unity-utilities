using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams.Editor
{
    [CustomEditor(typeof(Beam))]
    public sealed class BeamEditor : UnityEditor.Editor
    {
        private ReorderableList modifierList;
        private SerializedProperty modifiers;

        private void OnEnable()
        {
            modifiers = serializedObject.FindProperty("modifiers");
            modifierList = new ReorderableList(serializedObject, modifiers, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Ordered Path Modifiers"),
                elementHeight = EditorGUIUtility.singleLineHeight + 4f,
                drawElementCallback = DrawModifier
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "modifiers");
            EditorGUILayout.Space(3f);
            modifierList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();

            Beam beam = (Beam)target;
            EditorGUILayout.Space(6f);
            DrawStatus(beam);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh"))
                    beam.Refresh();
                if (GUILayout.Button("Restart Age"))
                    beam.RestartAge();
            }
        }

        private static void DrawStatus(Beam beam)
        {
            if (!beam.HasResolvedPath)
            {
                EditorGUILayout.HelpBox(
                    "The beam is not currently resolved. Assign an active target, path provider, and path renderer.",
                    MessageType.Info);
                return;
            }

            int pointCount = 0;
            float longest = 0f;
            BeamPathBuffer paths = beam.Paths;
            for (int i = 0; i < paths.Count; i++)
            {
                pointCount += paths[i].Count;
                longest = Mathf.Max(longest, paths[i].Length);
            }
            EditorGUILayout.LabelField("Live Strands", paths.Count.ToString());
            EditorGUILayout.LabelField("Live Points", pointCount.ToString());
            EditorGUILayout.LabelField("Longest Strand", $"{longest:0.###} m");
        }

        private void DrawModifier(Rect rect, int index, bool active, bool focused)
        {
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, modifiers.GetArrayElementAtIndex(index), GUIContent.none);
        }
    }
}
