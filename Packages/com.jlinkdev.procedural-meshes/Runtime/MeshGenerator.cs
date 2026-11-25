using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JLink.ProceduralMeshes
{
    public enum DiscMapping { Planar, Strip }
    public enum SideMapping { Normalized, WorldUnits }
    public enum MeshOrientation { YUp, ZUp, XUp }
    public enum MeshPivot { Center, Base }

    public static class MeshGenerator
    {
        public static Mesh Generate(MeshRecipe recipe)
        {
            recipe.Validate();
            Mesh mesh;
            if (recipe.generator == "cylinder" || recipe.generator == "cone")
                mesh = CylinderGenerator.Generate(recipe);
            else
            {
                var surface = new SurfaceBuilder();
                switch (recipe.generator)
                {
                    case "plane": surface.Plane(recipe); break;
                    case "box": surface.Box(recipe); break;
                    case "uv-sphere": surface.RoundBody(recipe, false); break;
                    case "capsule": surface.RoundBody(recipe, true); break;
                    case "icosphere": Icosphere.Build(surface, recipe); break;
                    case "disc": case "annulus": surface.Disc(recipe); break;
                    case "torus": surface.Torus(recipe); break;
                    case "ribbon": surface.Ribbon(recipe); break;
                    default: throw new ArgumentException("Unsupported mesh generator.");
                }
                mesh = surface.Finish(recipe.heightInColorR);
            }

            if (recipe.uvScale != Vector2.one || recipe.uvOffset != Vector2.zero || recipe.uvRotation != 0f)
            {
                var uv = mesh.uv;
                float angle = recipe.uvRotation * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                for (int i = 0; i < uv.Length; i++)
                {
                    Vector2 p = Vector2.Scale(uv[i], recipe.uvScale);
                    uv[i] = new Vector2(cos * p.x - sin * p.y, sin * p.x + cos * p.y) + recipe.uvOffset;
                }
                mesh.uv = uv;
                mesh.RecalculateTangents();
            }
            if (recipe.pivot != MeshPivot.Center || recipe.orientation != MeshOrientation.YUp || recipe.insideFacing)
                PlaceMesh(mesh, recipe);
            return mesh;
        }

        static void PlaceMesh(Mesh mesh, MeshRecipe recipe)
        {
            Quaternion rotation = Quaternion.identity;
            if (recipe.orientation == MeshOrientation.XUp) rotation = Quaternion.Euler(0, 0, -90);
            if (recipe.orientation == MeshOrientation.ZUp) rotation = Quaternion.Euler(90, 0, 0);
            Vector3 origin = recipe.pivot == MeshPivot.Base ? Vector3.up * mesh.bounds.min.y : Vector3.zero;
            var positions = mesh.vertices;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            float sign = recipe.insideFacing ? -1f : 1f;
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = rotation * (positions[i] - origin);
                normals[i] = rotation * normals[i] * sign;
                Vector3 t = rotation * (Vector3)tangents[i];
                tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w * sign);
            }
            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.tangents = tangents;
            if (recipe.insideFacing)
            {
                for (int part = 0; part < mesh.subMeshCount; part++)
                {
                    var triangles = mesh.GetTriangles(part);
                    for (int i = 0; i < triangles.Length; i += 3)
                        (triangles[i+1], triangles[i+2]) = (triangles[i+2], triangles[i+1]);
                    mesh.SetTriangles(triangles, part, false);
                }
            }
            mesh.RecalculateBounds();
        }
    }

    internal sealed class SurfaceBuilder
    {
        readonly List<Vector3> positions = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> triangles = new List<int>();

        internal int Vertex(Vector3 position, Vector3 normal, Vector2 texcoord)
        {
            int index = positions.Count;
            positions.Add(position);
            normals.Add(normal);
            uv.Add(texcoord);
            return index;
        }

        internal void Triangle(int a, int b, int c)
        {
            Vector3 face = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
            // A polar grid has one collapsed edge at a sphere tip or disc center.
            if (face.sqrMagnitude == 0f) return;
            bool forward = Vector3.Dot(face, normals[a] + normals[b] + normals[c]) > 0f;
            triangles.Add(a);
            triangles.Add(forward ? b : c);
            triangles.Add(forward ? c : b);
        }

        void Grid(int columns, int rows, Action<float, float> emit)
        {
            int first = positions.Count;
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= columns; x++)
                    emit((float)x / columns, (float)y / rows);

            int stride = columns + 1;
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    int a = first + y * stride + x;
                    Triangle(a, a + stride, a + 1);
                    Triangle(a + 1, a + stride, a + stride + 1);
                }
        }

        static Vector3 Direction(float u, float sweep, float seam)
        {
            if (u == 1f && sweep == 360f) u = 0;
            float a = (u * sweep + seam % 360f) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
        }

        void Face(Vector3 center, Vector3 across, Vector3 along, Vector3 normal, int columns, int rows)
        {
            Grid(columns, rows, (u, v) => Vertex(center + (u - 0.5f) * across + (v - 0.5f) * along, normal, new Vector2(u, v)));
        }

        internal void Plane(MeshRecipe r) =>
            Face(Vector3.zero, Vector3.right * r.width, Vector3.forward * r.depth, Vector3.up, r.columns, r.rows);

        internal void Box(MeshRecipe r)
        {
            Vector3 x = Vector3.right * r.width, y = Vector3.up * r.height, z = Vector3.forward * r.depth;
            Face( x / 2, -z, y, Vector3.right, r.depthSegments, r.rows);
            Face(-x / 2,  z, y, Vector3.left, r.depthSegments, r.rows);
            Face( y / 2,  x, z, Vector3.up, r.columns, r.depthSegments);
            Face(-y / 2,  x,-z, Vector3.down, r.columns, r.depthSegments);
            Face( z / 2,  x, y, Vector3.forward, r.columns, r.rows);
            Face(-z / 2, -x, y, Vector3.back, r.columns, r.rows);
        }

        internal void RoundBody(MeshRecipe r, bool capsule)
        {
            int hemisphere = r.rings;
            int body = capsule && r.bodyLength > 0f ? r.heightSegments : 0;
            int rows = capsule ? 2 * hemisphere + body : r.rings;
            float straight = capsule ? r.bodyLength : 0f;
            Grid(r.sides, rows, (u, v) =>
            {
                int row = Mathf.RoundToInt(v * rows);
                float latitude, center;
                if (!capsule)
                {
                    latitude = (v - 0.5f) * Mathf.PI;
                    center = 0;
                }
                else if (row <= hemisphere)
                {
                    latitude = ((float)row / hemisphere - 1f) * Mathf.PI * 0.5f;
                    center = -straight * 0.5f;
                }
                else if (row <= hemisphere + body)
                {
                    latitude = 0;
                    center = straight * ((float)(row - hemisphere) / body - 0.5f);
                }
                else
                {
                    latitude = (float)(row - hemisphere - body) / hemisphere * Mathf.PI * 0.5f;
                    center = straight * 0.5f;
                }
                var normal = Direction(u, 360f, r.seamDegrees) * Mathf.Cos(latitude) + Vector3.up * Mathf.Sin(latitude);
                if (row == 0) normal = Vector3.down;
                if (row == rows) normal = Vector3.up;
                float along = ((latitude + Mathf.PI / 2) * r.radius + center + straight / 2) / (Mathf.PI * r.radius + straight);
                Vertex(normal * r.radius + Vector3.up * center, normal, new Vector2(u, along));
            });
        }

        internal void Disc(MeshRecipe r)
        {
            float inner = r.generator == "disc" ? 0f : r.innerRadius;
            Grid(r.sides, r.bands, (u, v) =>
            {
                Vector3 p = Direction(u, r.sweep, r.seamDegrees) * Mathf.Lerp(inner, r.radius, v);
                Vector2 texcoord = r.discUV == DiscMapping.Strip ? new Vector2(u, v)
                    : new Vector2(0.5f + p.x / (2 * r.radius), 0.5f + p.z / (2 * r.radius));
                Vertex(p, Vector3.up, texcoord);
            });
        }

        internal void Torus(MeshRecipe r)
        {
            Grid(r.sides, r.tubeSegments, (u, v) =>
            {
                Vector3 radial = Direction(u, r.sweep, r.seamDegrees);
                float angle = (v == 1 ? 0 : v) * 2 * Mathf.PI;
                Vector3 normal = radial * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
                Vertex(radial * r.radius + normal * r.tubeRadius, normal, new Vector2(u, v));
            });
        }

        internal void Ribbon(MeshRecipe r)
        {
            float curvature = r.bend * Mathf.Deg2Rad / r.length;
            Grid(r.columns, r.rows, (u, v) =>
            {
                float distance = (v - 0.5f) * r.length;
                float angle = distance * curvature;
                float halfSin = Mathf.Sin(angle / 2);
                Vector3 center = curvature == 0 ? Vector3.forward * distance :
                    new Vector3(2 * halfSin * halfSin, 0, Mathf.Sin(angle)) / curvature;
                var across = new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle));
                Vertex(center + across * ((u - 0.5f) * r.width), Vector3.up, new Vector2(u, v));
            });
        }

        internal Mesh Finish(bool heightData)
        {
            var mesh = new Mesh { indexFormat = positions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            if (heightData)
            {
                var bounds = mesh.bounds;
                var colors = new Color32[positions.Count];
                for (int i = 0; i < colors.Length; i++)
                {
                    float height = Mathf.InverseLerp(bounds.min.y, bounds.max.y, positions[i].y);
                    colors[i] = new Color32((byte)Mathf.RoundToInt(255 * height), 0, 0, 255);
                }
                mesh.colors32 = colors;
            }
            return mesh;
        }
    }
}
