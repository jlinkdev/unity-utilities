using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricRain.Editor
{
    [CustomEditor(typeof(RainBoxVolume), true), CanEditMultipleObjects]
    public sealed class RainBoxVolumeEditor : UnityEditor.Editor
    {
        private readonly BoxBoundsHandle bounds = new BoxBoundsHandle();
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox(target is RainVolume
                ? "Set the camera's Rain Profile > Extent to Volumes Only. Overlapping rain boxes share one world-space field."
                : "Removes both rain streaks and rain haze. Works with unbounded rain and bounded rain volumes.", MessageType.Info);
        }
        private void OnSceneGUI()
        {
            var box = (RainBoxVolume)target;
            using (new Handles.DrawingScope(box.transform.localToWorldMatrix))
            {
                bounds.center = box.center; bounds.size = box.size;
                bounds.SetColor(box is RainVolume ? Color.cyan : new Color(1,0.55f,0.15f));
                EditorGUI.BeginChangeCheck();
                bounds.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(box,"Resize Rain Volume");
                    box.center = bounds.center; box.size = bounds.size;
                    EditorUtility.SetDirty(box);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(box);
                }
            }
        }
    }
}
