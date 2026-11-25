using System.Collections.Generic;
using UnityEngine;

namespace JLink.ProceduralMeshes
{
    internal static class Icosphere
    {
        static readonly int[] Faces =
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };

        internal static void Build(SurfaceBuilder surface, MeshRecipe r)
        {
            float t = (1 + Mathf.Sqrt(5)) / 2;
            var points = new List<Vector3>
            {
                new Vector3(-1,t,0), new Vector3(1,t,0), new Vector3(-1,-t,0), new Vector3(1,-t,0),
                new Vector3(0,-1,t), new Vector3(0,1,t), new Vector3(0,-1,-t), new Vector3(0,1,-t),
                new Vector3(t,0,-1), new Vector3(t,0,1), new Vector3(-t,0,-1), new Vector3(-t,0,1)
            };
            for (int i = 0; i < points.Count; i++) points[i] = points[i].normalized;
            var faces = new List<int>(Faces);
            for (int level = 0; level < r.subdivisions; level++)
            {
                var edges = new Dictionary<(int, int), int>();
                int Midpoint(int a, int b)
                {
                    var edge = a < b ? (a, b) : (b, a);
                    if (!edges.TryGetValue(edge, out int index))
                    {
                        index = points.Count;
                        points.Add((points[a] + points[b]).normalized);
                        edges.Add(edge, index);
                    }
                    return index;
                }
                var next = new List<int>(faces.Count * 4);
                for (int i = 0; i < faces.Count; i += 3)
                {
                    int a = faces[i], b = faces[i+1], c = faces[i+2];
                    int ab = Midpoint(a,b), bc = Midpoint(b,c), ca = Midpoint(c,a);
                    next.AddRange(new[] { a,ab,ca, b,bc,ab, c,ca,bc, ab,bc,ca });
                }
                faces = next;
            }

            var vertices = new Dictionary<(int, float), int>();
            var us = new float[3];
            var indices = new int[3];
            for (int i = 0; i < faces.Count; i += 3)
            {
                int pole = -1;
                for (int j = 0; j < 3; j++)
                {
                    Vector3 n = points[faces[i+j]];
                    us[j] = Mathf.Atan2(n.z, n.x) / (2 * Mathf.PI) + 0.5f;
                    if (n.x == 0 && n.z == 0) pole = j;
                }
                if (pole >= 0) us[pole] = us[(pole + 1) % 3];
                if (Mathf.Max(us[0], Mathf.Max(us[1], us[2])) - Mathf.Min(us[0], Mathf.Min(us[1], us[2])) > 0.5f)
                    for (int j = 0; j < 3; j++) if (us[j] < 0.5f) us[j] += 1f;
                if (pole >= 0) us[pole] = 0.5f * (us[(pole+1)%3] + us[(pole+2)%3]);
                for (int j = 0; j < 3; j++)
                {
                    int source = faces[i+j];
                    var key = (source, us[j]);
                    if (!vertices.TryGetValue(key, out indices[j]))
                    {
                        Vector3 n = points[source];
                        var uv = new Vector2(us[j], Mathf.Asin(n.y) / Mathf.PI + 0.5f);
                        indices[j] = surface.Vertex(n * r.radius, n, uv);
                        vertices.Add(key, indices[j]);
                    }
                }
                surface.Triangle(indices[0], indices[1], indices[2]);
            }
        }
    }
}
