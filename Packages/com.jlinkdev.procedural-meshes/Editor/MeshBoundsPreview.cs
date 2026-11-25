using UnityEditor;
using UnityEngine;

namespace JLink.ProceduralMeshes.Editor
{
    internal static class MeshBoundsPreview
    {
        internal static void Draw(Bounds bounds)
        {
            Rect area = GUILayoutUtility.GetRect(20, 92, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                float half = area.width * 0.5f;
                DrawProjection(new Rect(area.x, area.y, half - 1, area.height), "Front · XY",
                    bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y);
                DrawProjection(new Rect(area.x + half, area.y, half, area.height), "Top · XZ",
                    bounds.min.x, bounds.max.x, bounds.min.z, bounds.max.z);
            }
            EditorGUILayout.LabelField("Size (XYZ)", bounds.size.ToString("G4"), EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Center (XYZ)", bounds.center.ToString("G4"), EditorStyles.miniLabel);
        }

        static void DrawProjection(Rect area, string title, float left, float right, float bottom, float top)
        {
            EditorGUI.DrawRect(area, new Color(0.15f, 0.15f, 0.15f));
            GUI.Label(new Rect(area.x + 4, area.y, area.width - 8, 18), title, EditorStyles.centeredGreyMiniLabel);
            // Include the local origin so the cross shows pivot placement even for a partial sweep.
            var min = new Vector2(Mathf.Min(left, 0), Mathf.Min(bottom, 0));
            var max = new Vector2(Mathf.Max(right, 0), Mathf.Max(top, 0));
            var span = max - min;
            float scale = Mathf.Min((area.width - 18) / Mathf.Max(1e-7f, span.x), (area.height - 33) / Mathf.Max(1e-7f, span.y));
            var mid = (max + min) * 0.5f;
            var pivot = new Vector2(area.center.x - mid.x * scale, area.center.y + 7 + mid.y * scale);
            var box = new Rect(pivot.x + left * scale, pivot.y - top * scale,
                Mathf.Max(1, (right - left) * scale), Mathf.Max(1, (top - bottom) * scale));
            Color edge = new Color(0.39f, 0.79f, 0.84f);
            EditorGUI.DrawRect(box, new Color(edge.r, edge.g, edge.b, 0.14f));
            EditorGUI.DrawRect(new Rect(box.x, box.y, box.width, 1), edge);
            EditorGUI.DrawRect(new Rect(box.x, box.yMax - 1, box.width, 1), edge);
            EditorGUI.DrawRect(new Rect(box.x, box.y, 1, box.height), edge);
            EditorGUI.DrawRect(new Rect(box.xMax - 1, box.y, 1, box.height), edge);
            EditorGUI.DrawRect(new Rect(pivot.x - 3, pivot.y, 7, 1), Color.white);
            EditorGUI.DrawRect(new Rect(pivot.x, pivot.y - 3, 1, 7), Color.white);
        }
    }
}
