using System.Collections.Generic;
using UnityEngine;

namespace JLink.Frames
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class ProceduralFrame : MonoBehaviour
    {
        public FrameRecipe recipe = new FrameRecipe();
        public Mesh GeneratedMesh { get; private set; }
        internal FrameContour Contour { get; private set; }
        internal int MeshRevision { get; private set; }
        /// <summary>Maps upright recipe coordinates to world space, including a tabletop lean.</summary>
        public Matrix4x4 DesignToWorld => transform.localToWorldMatrix * (Contour?.designToMesh ?? Matrix4x4.identity);
        [SerializeField, HideInInspector] Material previousEaselMaterial;
        FrameSurfacePick picker;
        readonly List<Material> materials = new List<Material>(3);
        static readonly string[] DefaultMaterials = { "JLinkFrames/Rail","JLinkFrames/Mat","JLinkFrames/Artwork","JLinkFrames/EaselBack" };
        bool dirty;

        internal bool TryPick(Ray ray,out float profileT,out Vector3 point,out Vector2 outward)
        {
            if (picker==null) picker=new FrameSurfacePick();
            return picker.TryPick(this,ray,out profileT,out point,out outward);
        }

        public void Apply(FrameRecipe design)
        {
            recipe = design?.Copy() ?? new FrameRecipe();
            Rebuild();
        }

        public void Rebuild()
        {
            if (!GeneratedMesh)
            {
                GeneratedMesh = new Mesh { name = "Picture frame", hideFlags = HideFlags.DontSave };
                GeneratedMesh.MarkDynamic();
            }
            if (recipe == null) recipe = new FrameRecipe();
            bool hadEasel=GeneratedMesh.subMeshCount==4;
            Contour=FrameMesh.BuildWithContour(recipe,GeneratedMesh);
            MeshRevision++;
            GetComponent<MeshFilter>().sharedMesh = GeneratedMesh;
            var renderer = GetComponent<MeshRenderer>();
            renderer.GetSharedMaterials(materials);
            int slots=GeneratedMesh.subMeshCount;
            bool updateSlots=materials.Count<slots;
            if (!recipe.easelBack && hadEasel && materials.Count>3)
            {
                previousEaselMaterial=materials[3];
                materials.RemoveAt(3);
                updateSlots=true;
            }
            while (materials.Count<slots) materials.Add(materials.Count==3 ? previousEaselMaterial : null);
            for (int i=0;i<slots;i++)
            {
                if (materials[i]) continue;
                materials[i]=Resources.Load<Material>(DefaultMaterials[i]);
                updateSlots=true;
            }
            if (updateSlots) renderer.SetSharedMaterials(materials);
            materials.Clear();
            dirty = false;
        }

        void OnEnable() => dirty = true;
        void OnValidate() => dirty = true;
        void Update() { if (dirty) Rebuild(); }

        void OnDestroy()
        {
            if (!GeneratedMesh) return;
            if (Application.isPlaying) Destroy(GeneratedMesh);
            else DestroyImmediate(GeneratedMesh);
        }
    }
}
