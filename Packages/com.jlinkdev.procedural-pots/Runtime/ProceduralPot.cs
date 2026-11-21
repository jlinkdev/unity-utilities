using UnityEngine;

namespace JLink.Pots
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ProceduralPot : MonoBehaviour
    {
        public PotRecipe recipe = new PotRecipe();
        readonly PotMesh builder = new PotMesh();
        Mesh mesh;
        bool dirty;

        public Mesh GeneratedMesh => mesh;

        public void Apply(PotRecipe source)
        {
            recipe = source.Copy();
            Rebuild();
        }

        public void Rebuild()
        {
            if (!mesh)
            {
                mesh = new Mesh { name = "Procedural pot", hideFlags = HideFlags.DontSave };
                mesh.MarkDynamic();
            }
            if (recipe == null) recipe = new PotRecipe();
            builder.Build(recipe, mesh);
            var filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh != mesh) filter.sharedMesh = mesh;
            var renderer = GetComponent<MeshRenderer>();
            if (!renderer.sharedMaterial)
            {
                var clay = Resources.Load<Material>("JLinkPots/Clay");
                renderer.sharedMaterials = new[] { clay, clay, clay, clay };
            }
            dirty = false;
        }

        void OnEnable() => dirty = true;
        void OnValidate() => dirty = true;
        void Update() { if (dirty) Rebuild(); }

        void OnDestroy()
        {
            if (!mesh) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
        }
    }
}
