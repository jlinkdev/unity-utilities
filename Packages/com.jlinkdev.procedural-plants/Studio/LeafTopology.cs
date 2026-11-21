using System.Collections.Generic;
using UnityEngine;

namespace JLink.Plants.Studio
{
    // A disposable inspection overlay. The plant and its baked mesh retain their shared vertices.
    sealed class LeafTopology
    {
        readonly List<Vector3> positions = new List<Vector3>();
        readonly List<int> triangles = new List<int>();
        readonly List<int> lines = new List<int>();
        readonly HashSet<ulong> edges = new HashSet<ulong>();
        Mesh overlay;
        Material material;
        GameObject preview;
        PlantGrowth previous;
        double previousAge;
        LeafDetail previousDetail;

        public void Show(ProceduralPlant plant)
        {
            if (!overlay)
            {
                overlay = new Mesh { name = "Leaf topology preview", hideFlags = HideFlags.DontSave };
                material = new Material(Resources.Load<Shader>("JLinkPlants/LeafWire")) { hideFlags = HideFlags.DontSave };
                preview = new GameObject("Leaf wireframe") { hideFlags = HideFlags.DontSave, layer = plant.gameObject.layer };
                preview.transform.SetParent(plant.transform,false);
                preview.AddComponent<MeshFilter>().sharedMesh = overlay;
                var renderer = preview.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            if (!preview.activeSelf) preview.SetActive(true);
            if (previous != plant.Growth || previousAge != plant.Growth.Age || previousDetail != plant.Detail)
            {
                Mesh source = plant.CurrentMesh;
                source.GetVertices(positions);
                source.GetTriangles(triangles, 1);
                lines.Clear();
                edges.Clear();
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    Edge(triangles[i], triangles[i+1]);
                    Edge(triangles[i+1], triangles[i+2]);
                    Edge(triangles[i+2], triangles[i]);
                }
                overlay.Clear();
                overlay.indexFormat = source.indexFormat;
                overlay.SetVertices(positions);
                overlay.SetIndices(lines, MeshTopology.Lines, 0);
                previous = plant.Growth;
                previousAge = previous.Age;
                previousDetail = plant.Detail;
            }
        }

        public void Hide()
        {
            if (preview && preview.activeSelf) preview.SetActive(false);
        }

        void Edge(int a, int b)
        {
            ulong key = ((ulong)(uint)Mathf.Min(a,b) << 32) | (uint)Mathf.Max(a,b);
            if (edges.Add(key)) { lines.Add(a); lines.Add(b); }
        }

        public void Release()
        {
            if (preview) Object.Destroy(preview);
            if (overlay) Object.Destroy(overlay);
            if (material) Object.Destroy(material);
            overlay = null;
            material = null;
            previous = null;
            preview = null;
        }
    }
}
