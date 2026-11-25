using System;
using UnityEngine;

namespace JLink.ProceduralMeshes.Editor
{
    // A fixed oblique view keeps XZ sheets and upright meshes recognizable in the Project window.
    // Rasterize on the CPU so batch and import-worker processes can bake the same thumbnail.
    internal static class MeshThumbnail
    {
        const int Size = 96;
        static readonly Vector3 View = new Vector3(1f, 0.9f, 1.4f).normalized;
        static readonly Vector3 Right = Vector3.Cross(Vector3.up, View).normalized;
        static readonly Vector3 Up = Vector3.Cross(View, Right);
        static readonly Vector3 Light = new Vector3(-0.3f, 1f, 0.8f).normalized;

        public static Texture2D Render(Mesh mesh)
        {
            var points = mesh.vertices;
            var normals = mesh.normals;
            var light = new float[points.Length];
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = -min;
            Vector3 meshCenter = mesh.bounds.center;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i] - meshCenter;
                points[i] = new Vector3(Vector3.Dot(p, Right), Vector3.Dot(p, Up), Vector3.Dot(p, View));
                light[i] = Mathf.Clamp01(0.45f + 0.52f * Vector3.Dot(normals[i], Light));
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }
            Vector2 center = (min + max) * 0.5f;
            float scale = (Size - 8) / Mathf.Max(0.000001f, Mathf.Max(max.x - min.x, max.y - min.y));
            for (int i = 0; i < points.Length; i++)
            {
                points[i].x = (points[i].x - center.x) * scale + Size * 0.5f;
                points[i].y = (points[i].y - center.y) * scale + Size * 0.5f;
            }

            var pixels = new Color32[Size * Size];
            var depth = new float[pixels.Length];
            Array.Fill(depth, float.NegativeInfinity);
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                Vector3 origin = points[a], ab = points[b] - origin, ac = points[c] - origin;
                float area = ab.x * ac.y - ab.y * ac.x;
                if (Mathf.Abs(area) < 0.0000001f) continue;
                Vector2 lo = Vector2.Min(origin, Vector2.Min(points[b], points[c]));
                Vector2 hi = Vector2.Max(origin, Vector2.Max(points[b], points[c]));
                for (int y = Mathf.Max(0, Mathf.CeilToInt(lo.y - 0.5f)); y < Size && y + 0.5f <= hi.y; y++)
                {
                    for (int x = Mathf.Max(0, Mathf.CeilToInt(lo.x - 0.5f)); x < Size && x + 0.5f <= hi.x; x++)
                    {
                        float px = x + 0.5f - origin.x, py = y + 0.5f - origin.y;
                        float u = (px * ac.y - py * ac.x) / area;
                        float v = (ab.x * py - ab.y * px) / area;
                        if (u < 0 || v < 0 || u + v > 1) continue;
                        float z = origin.z + u * ab.z + v * ac.z;
                        int index = y * Size + x;
                        if (z <= depth[index]) continue;
                        depth[index] = z;
                        float shade = light[a] + u * (light[b] - light[a]) + v * (light[c] - light[a]);
                        byte value = (byte)Mathf.RoundToInt(45f + shade * 205f);
                        pixels[index] = new Color32(value, value, value, 255);
                    }
                }
            }
            var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            image.SetPixels32(pixels);
            image.Apply(false);
            return image;
        }
    }
}
