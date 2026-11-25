using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JLink.ProceduralMeshes
{
    [Serializable]
    public sealed class MeshRecipe
    {
        public int schemaVersion = 2;
        public string generator = "cylinder";
        public int generatorVersion = 1;
        public float bottomRadius = 0.5f;
        public float topRadius = 0.5f;
        public float height = 2f;
        public int sides = 24;
        public int heightSegments = 4;
        public float seamDegrees;
        public bool heightInColorR;
        public bool capEnds;
        public bool closeCut;
        public bool separateEnds;
        public SideMapping sideUV;
        public float width = 1f;
        public float depth = 1f;
        public float length = 2f;
        public float radius = 0.5f;
        public float innerRadius = 0.25f;
        public float tubeRadius = 0.15f;
        public float bodyLength = 1f;
        [Range(1, 256)] public int columns = 4;
        [Range(1, 256)] public int rows = 8;
        [Range(1, 256)] public int depthSegments = 4;
        [Range(2, 128)] public int rings = 12;
        [Range(0, 5)] public int subdivisions = 2;
        [Range(1, 128)] public int bands = 2;
        [Range(3, 128)] public int tubeSegments = 12;
        [Range(1f, 360f)] public float sweep = 360f;
        [Range(-360f, 360f)] public float bend;
        public DiscMapping discUV;
        public Vector2 uvScale = Vector2.one;
        public Vector2 uvOffset;
        public float uvRotation;
        public MeshOrientation orientation;
        public MeshPivot pivot;
        public bool insideFacing;

        public void Validate()
        {
            if ((schemaVersion != 2 && !(schemaVersion == 1 && generator == "cylinder")) || generatorVersion != 1)
                throw new ArgumentException("Unsupported mesh recipe or generator version.");
            MeshCatalog.Find(generator);
            if (!(height > 0f && height <= 100000f && bottomRadius >= 0f && bottomRadius <= 100000f &&
                topRadius >= 0f && topRadius <= 100000f && (topRadius > 0f || bottomRadius > 0f)))
                throw new ArgumentException("Use positive height and at least one positive radius. Maximum dimension is 100000.");
            if (sides < 3 || sides > 512 || heightSegments < 1 || heightSegments > 512)
                throw new ArgumentException("Use 3–512 sides and 1–512 height segments.");
            if (float.IsNaN(seamDegrees) || float.IsInfinity(seamDegrees))
                throw new ArgumentException("Seam angle must be finite.");
            if (!(width > 0 && width <= 100000 && depth > 0 && depth <= 100000 && length > 0 && length <= 100000 &&
                radius > 0 && radius <= 100000 && tubeRadius > 0 && tubeRadius <= 100000 && bodyLength >= 0 && bodyLength <= 100000))
                throw new ArgumentException("Use positive dimensions up to 100000. Capsule body length may be zero.");
            if (!float.IsFinite(bend) || !float.IsFinite(uvRotation) || !float.IsFinite(uvOffset.x) ||
                !float.IsFinite(uvOffset.y) || !float.IsFinite(uvScale.x) || !float.IsFinite(uvScale.y))
                throw new ArgumentException("Bend and UV settings must be finite.");
            if (generator == "annulus" && !(innerRadius > 0 && innerRadius < radius))
                throw new ArgumentException("The inner radius must be between zero and the outer radius.");
            if (generator == "torus" && tubeRadius >= radius)
                throw new ArgumentException("Tube radius must be smaller than the torus major radius.");
            if (generator == "cone" && bottomRadius <= 0)
                throw new ArgumentException("A cone needs a positive base radius.");
            if (generator == "ribbon" && Mathf.Abs(bend * Mathf.Deg2Rad) * width >= 2f * length)
                throw new ArgumentException("Reduce the ribbon width or bend to keep the inner edge open.");
            if (generator == "ribbon" && Mathf.Abs(bend) >= rows * 180f)
                throw new ArgumentException("Increase the row count for this bend angle.");
            if (columns < 1 || columns > 256 || rows < 1 || rows > 256 || depthSegments < 1 || depthSegments > 256 ||
                rings < 2 || rings > 128 || subdivisions < 0 || subdivisions > 5 || bands < 1 || bands > 128 ||
                tubeSegments < 3 || tubeSegments > 128 || !(sweep > 0 && sweep <= 360))
                throw new ArgumentException("Subdivision count or sweep is out of range.");
        }
    }

    public static class CylinderGenerator
    {
        public static Mesh Generate(MeshRecipe recipe)
        {
            recipe.Validate();
            int stride = recipe.sides + 1;
            float topRadius = recipe.generator == "cone" ? 0f : recipe.topRadius;
            int wallCount = stride * (recipe.heightSegments + 1);
            int capCount = recipe.capEnds ? (recipe.bottomRadius > 0f ? 1 : 0) + (topRadius > 0f ? 1 : 0) : 0;
            bool full = recipe.sweep == 360f;
            bool cut = !full && recipe.closeCut;
            int rimCount = full ? recipe.sides : stride;
            int count = wallCount + (rimCount + 1) * capCount + (cut ? 4 * (recipe.heightSegments + 1) : 0);
            var positions = new Vector3[count];
            var normals = new Vector3[count];
            var tangents = new Vector4[count];
            var uvs = new Vector2[count];
            var colors = recipe.heightInColorR ? new Color32[count] : null;
            var indices = new List<int>(6 * recipe.sides * recipe.heightSegments);
            float slope = recipe.bottomRadius - topRadius;

            for (int row = 0; row <= recipe.heightSegments; row++)
            {
                float v = (float)row / recipe.heightSegments;
                float radius = row == recipe.heightSegments ? topRadius : Mathf.Lerp(recipe.bottomRadius, topRadius, v);
                for (int side = 0; side <= recipe.sides; side++)
                {
                    int i = row * stride + side;
                    float u = (float)side / recipe.sides;
                    float turn = full && side == recipe.sides ? 0f : u;
                    float angle = (recipe.seamDegrees % 360f + turn * recipe.sweep) * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle);
                    float z = Mathf.Sin(angle);
                    positions[i] = new Vector3(x * radius, (v - 0.5f) * recipe.height, z * radius);
                    normals[i] = new Vector3(recipe.height * x, slope, recipe.height * z).normalized;
                    tangents[i] = new Vector4(-z, 0f, x, -1f);
                    uvs[i] = new Vector2(u, v);
                    if (recipe.sideUV == SideMapping.WorldUnits)
                        uvs[i] = new Vector2(u * recipe.sweep * Mathf.Deg2Rad * Mathf.Max(topRadius, recipe.bottomRadius), v * recipe.height);
                    if (colors != null) colors[i] = new Color32((byte)Mathf.RoundToInt(v * 255f), 0, 0, 255);
                    if (row == recipe.heightSegments || side == recipe.sides) continue;

                    int above = i + stride;
                    if (row > 0 || recipe.bottomRadius > 0f)
                    {
                        indices.Add(i);
                        indices.Add(above);
                        indices.Add(i + 1);
                    }
                    if (row + 1 < recipe.heightSegments || topRadius > 0f)
                    {
                        indices.Add(i + 1);
                        indices.Add(above);
                        indices.Add(above + 1);
                    }
                }
            }

            int next = wallCount;
            int wallIndices = indices.Count;
            void Cap(bool top)
            {
                int center = next++;
                positions[center] = Vector3.up * ((top ? 0.5f : -0.5f) * recipe.height);
                uvs[center] = new Vector2(0.5f, 0.5f);
                for (int i = 0; i < rimCount; i++)
                {
                    int rim = next++;
                    int wall = (top ? recipe.heightSegments * stride : 0) + i;
                    positions[rim] = positions[wall];
                    float diameter = 2f * (top ? topRadius : recipe.bottomRadius);
                    uvs[rim] = uvs[center] + new Vector2(positions[rim].x, positions[rim].z) / diameter;
                    if (i == recipe.sides) continue;
                    int following = center + 1 + (i + 1) % rimCount;
                    indices.Add(center);
                    indices.Add(top ? following : rim);
                    indices.Add(top ? rim : following);
                }
                for (int i = center; i < next; i++)
                {
                    normals[i] = top ? Vector3.up : Vector3.down;
                    tangents[i] = new Vector4(1, 0, 0, top ? -1 : 1);
                    if (colors != null) colors[i] = new Color32(top ? (byte)255 : (byte)0, 0, 0, 255);
                }
            }
            if (recipe.capEnds && recipe.bottomRadius > 0f) Cap(false);
            if (recipe.capEnds && topRadius > 0f) Cap(true);
            void CutFace(bool end)
            {
                float angle = (recipe.seamDegrees % 360f + (end ? recipe.sweep : 0f)) * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 normal = Vector3.Cross(radial, Vector3.up) * (end ? 1f : -1f);
                void FaceTriangle(int a, int b, int c)
                {
                    indices.Add(a);
                    indices.Add(end ? c : b);
                    indices.Add(end ? b : c);
                }
                for (int row = 0; row <= recipe.heightSegments; row++)
                {
                    int a = next;
                    int wall = row * stride + (end ? recipe.sides : 0);
                    float v = (float)row / recipe.heightSegments;
                    for (int edge = 0; edge < 2; edge++)
                    {
                        positions[next] = edge == 0 ? Vector3.up * positions[wall].y : positions[wall];
                        normals[next] = normal;
                        tangents[next] = new Vector4(radial.x, 0f, radial.z, end ? 1f : -1f);
                        uvs[next] = new Vector2(edge, v);
                        if (colors != null) colors[next] = colors[wall];
                        next++;
                    }
                    if (row == recipe.heightSegments) continue;
                    if (row > 0 || recipe.bottomRadius > 0) FaceTriangle(a, a + 2, a + 1);
                    if (row + 1 < recipe.heightSegments || topRadius > 0) FaceTriangle(a + 1, a + 2, a + 3);
                }
            }
            if (cut) { CutFace(false); CutFace(true); }

            var mesh = new Mesh { indexFormat = count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.uv = uvs;
            if (colors != null) mesh.colors32 = colors;
            if (recipe.separateEnds && indices.Count > wallIndices)
            {
                mesh.subMeshCount = 2;
                mesh.SetTriangles(indices, 0, wallIndices, 0);
                mesh.SetTriangles(indices, wallIndices, indices.Count - wallIndices, 1);
            }
            else mesh.SetTriangles(indices, 0);
            return mesh;
        }
    }
}
