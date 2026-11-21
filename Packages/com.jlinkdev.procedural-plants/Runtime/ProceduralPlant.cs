using System;
using UnityEngine;

namespace JLink.Plants
{
    public enum PlantSurface { Studio, PBR }

    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("JLink/Procedural Plant")]
    public sealed class ProceduralPlant : MonoBehaviour
    {
        [SerializeField] PlantRecipe recipe = PlantRecipe.Default;
        [SerializeField, Range(0, PlantGrowth.MaxOrgans)] float startingGrowth = 18;
        [SerializeField] PlantConditions conditions = PlantConditions.Default;
        [SerializeField] PlantCare care;
        [SerializeField] PlantSupports supports;
        [SerializeField] LeafDetail leafDetail = LeafDetail.Standard;
        [SerializeField] PlantSurface surface;
        [SerializeField] Material stemMaterial;
        [SerializeField] Material leafMaterial;
        [SerializeField, HideInInspector] PlantGrowth growth;
        [NonSerialized] Mesh ownedMesh;
        [NonSerialized] PlantMesher mesher;
        [NonSerialized] bool needsRefresh;
        [NonSerialized] uint renderedRevision;
        [NonSerialized] float renderedDroop;

        public PlantConditions Conditions { get => conditions; set => conditions = value; }
        public PlantSupports Supports { get => supports; set => supports = value; }
        public PlantGrowth Growth => growth;
        public Mesh CurrentMesh => ownedMesh;
        public PlantCare Care => care;
        public bool CanAdvance => growth!=null && (!growth.FullyGrown || (care.Enabled && care.Soil>0));
        public PlantSurface Surface
        {
            get => surface;
            set { surface = value; if (isActiveAndEnabled) ApplyMaterials(); }
        }
        public LeafDetail Detail
        {
            get => leafDetail;
            set
            {
                if (leafDetail == value) return;
                leafDetail = value;
                Refresh();
            }
        }

        public void SetCareEnabled(bool enabled)
        {
            care.SetEnabled(enabled,conditions.moisture);
            UpdateVisibleGrowth();
        }

        public void Water(float amount=1f)
        {
            care.Water(amount);
            if(care.Enabled) UpdateVisibleGrowth();
        }

        void OnEnable() => Refresh();

        void OnValidate() => needsRefresh = true;

        void Update()
        {
            if (needsRefresh) Refresh();
        }

        public void Generate(PlantRecipe design, double initialGrowth = 0)
        {
            var next = PlantGrowth.Sow(design, conditions,supports);
            next.Advance(initialGrowth, conditions,supports);
            ApplyGrowth(next);
        }

        public void GenerateFromInspector() => Generate(recipe, startingGrowth);

        public void Advance(double growthUnits)
        {
            EnsureGrowth();
            var environment=conditions;
            var nextCare=care;
            if(care.Enabled && double.IsFinite(growthUnits) && growthUnits>=0)
                environment.moisture=nextCare.Dry(growthUnits,growth.Recipe.species);
            growth.Advance(growthUnits,environment,supports);
            care=nextCare;
            UpdateVisibleGrowth();
        }

        void UpdateVisibleGrowth()
        {
            if (!isActiveAndEnabled) return;
            if (needsRefresh || !ownedMesh) { Refresh(); return; }
            if (renderedRevision != growth.Revision) { RenderGeometry(); return; }

            float droop = care.Droop(growth.Recipe.species);
            if (renderedDroop == droop) return;
            mesher.UpdateLeafPose(ownedMesh, droop);
            renderedDroop = droop;
        }

        public string SaveState()
        {
            EnsureGrowth();
            return growth.ToJson();
        }

        public void LoadState(string json) => ApplyGrowth(PlantGrowth.FromJson(json));

        internal void RestoreSpecimen(PlantGrowth restored,PlantSpecimen specimen,PlantSupports layout)
        {
            conditions=specimen.conditions; supports=layout;
            care=specimen.care;
            surface=specimen.surface; leafDetail=specimen.detail;
            ApplyGrowth(restored);
        }

        void ApplyGrowth(PlantGrowth restored)
        {
            growth = restored;
            recipe = restored.Recipe;
            Refresh();
        }

        public PlantGrowth Cut(int nodeId, bool plantUpright=false)
        {
            EnsureGrowth();
            var cutting = growth.Cut(nodeId,plantUpright);
            Refresh();
            return cutting;
        }

        public PlantGrowth DetachPlantlet(int runnerId)
        {
            EnsureGrowth();
            var daughter=growth.DetachPlantlet(runnerId);
            Refresh();
            return daughter;
        }

        public void CopyTo(ProceduralPlant destination) => CopyTo(destination, null);

        internal void CopyTo(ProceduralPlant destination, PlantGrowth structure)
        {
            if (!destination) throw new ArgumentNullException(nameof(destination));
            var specimen = structure == null ? PlantSpecimen.Capture(this) : PlantSpecimen.Capture(this, structure);
            destination.stemMaterial=stemMaterial;
            destination.leafMaterial=leafMaterial;
            destination.startingGrowth=startingGrowth;
            specimen.Restore(destination);
        }

        void EnsureGrowth()
        {
            if (growth != null && growth.Initialized) { growth.Upgrade(); return; }
            growth = PlantGrowth.Sow(recipe, conditions,supports);
            growth.Advance(startingGrowth, conditions,supports);
        }

        public void Refresh()
        {
            needsRefresh = false;
            EnsureGrowth();
            if (!isActiveAndEnabled) return;
            RenderGeometry();
            ApplyMaterials();
        }

        void ApplyMaterials()
        {
            bool lit = surface == PlantSurface.PBR;
            var stem = stemMaterial ? stemMaterial : Resources.Load<Material>(lit ? "JLinkPlants/Stem PBR" : "JLinkPlants/Botanical");
            var leaf = leafMaterial ? leafMaterial : Resources.Load<Material>(lit ? "JLinkPlants/Leaf PBR" : "JLinkPlants/Botanical");
            GetComponent<MeshRenderer>().sharedMaterials = new[] {
                stem, leaf
            };
        }

        void RenderGeometry()
        {
            if (!ownedMesh)
            {
                ownedMesh = new Mesh { name = "Procedural plant", hideFlags = HideFlags.DontSave };
                ownedMesh.MarkDynamic();
            }
            mesher ??= new PlantMesher();
            mesher.Write(growth, ownedMesh, leafDetail,care);
            renderedRevision = growth.Revision;
            renderedDroop = care.Droop(growth.Recipe.species);
            GetComponent<MeshFilter>().sharedMesh = ownedMesh;
        }

        void OnDisable()
        {
            if (!ownedMesh) return;
            var filter = GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh == ownedMesh) filter.sharedMesh = null;
            if (Application.isPlaying) Destroy(ownedMesh);
            else DestroyImmediate(ownedMesh);
            ownedMesh = null;
        }
    }
}
