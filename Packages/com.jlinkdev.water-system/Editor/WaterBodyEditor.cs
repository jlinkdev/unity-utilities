using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.WaterSystem.Editor
{
    [CustomEditor(typeof(WaterBody))]
    [CanEditMultipleObjects]
    internal sealed class WaterBodyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var body = (WaterBody)target;
            if (serializedObject.FindProperty("profile").objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign a Water Profile to customize the water. Presets are available under Assets > Create > jlinkdev > Water System.", MessageType.Info);
            if (Quaternion.Angle(body.transform.rotation, Quaternion.identity) > 0.01f || (body.transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
                EditorGUILayout.HelpBox("Water bodies support translation only. Reset world rotation and scale, and use Size/Depth to set the bounds. Rotated or scaled water is not supported.", MessageType.Warning);
            if (!(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                EditorGUILayout.HelpBox("Use a Universal Render Pipeline asset with Render Graph enabled and a Water Renderer Feature on the camera's renderer.", MessageType.Warning);
            var profile = body.Profile;
            float minimumWavelength = float.PositiveInfinity;
            if (profile.Waves != null)
            {
                for (int i = 0; i < Mathf.Min(profile.Waves.Length, WaterProfile.MaximumWaves); i++)
                {
                    if (profile.Waves[i].Amplitude > 0f)
                        minimumWavelength = Mathf.Min(minimumWavelength, Mathf.Max(0.1f, profile.Waves[i].Wavelength));
                }
            }
            float cellSize = Mathf.Max(body.Size.x, body.Size.y) / body.Resolution;
            if (minimumWavelength / cellSize < 4f)
                EditorGUILayout.HelpBox("The shortest wave has fewer than four grid cells per wavelength. Increase Resolution, reduce Size, or lengthen that wave to improve the waterline silhouette.", MessageType.Warning);
            if (body.Depth <= profile.MaximumDisplacement)
                EditorGUILayout.HelpBox("Depth is smaller than the maximum wave displacement. Troughs can cross the volume bottom; increase Depth or reduce wave amplitudes.", MessageType.Warning);
            EditorGUILayout.HelpBox("The waterline and underwater atmosphere require Water Renderer Feature. Select your Universal Renderer Data, then Tools > jlinkdev > Water System > Add Feature to Selected Renderer.", MessageType.Info);
            if (GUILayout.Button("Rebuild Surface Mesh"))
            {
                foreach (var selected in targets)
                {
                    var selectedBody = (WaterBody)selected;
                    Undo.RecordObject(selectedBody, "Rebuild Water Surface");
                    selectedBody.Rebuild();
                    EditorUtility.SetDirty(selectedBody);
                }
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawBounds(WaterBody body, GizmoType gizmoType)
        {
            Gizmos.color = new Color(0.08f, 0.78f, 0.92f, 0.75f);
            Bounds bounds = body.WorldBounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    [CustomEditor(typeof(WaterProfile))]
    internal sealed class WaterProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var profile = (WaterProfile)target;
            if (profile.Waves != null && profile.Waves.Length > WaterProfile.MaximumWaves)
                EditorGUILayout.HelpBox("Only the first eight waves are used by rendering and physics. Remove extra entries to avoid ambiguous authoring.", MessageType.Warning);
            EditorGUILayout.LabelField("Maximum vertical displacement", profile.MaximumDisplacement.ToString("0.###") + " m");
            EditorGUILayout.HelpBox("Broad waves affect the geometry, waterline, queries, and buoyancy. Detail normals and caustics are decorative. Waterline Width is measured in render-target pixels.", MessageType.Info);
        }
    }
}
