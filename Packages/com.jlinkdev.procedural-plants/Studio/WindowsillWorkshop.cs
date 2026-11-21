using System;
using System.Collections.Generic;
using UnityEngine;

namespace JLink.Plants.Studio
{
    [ExecuteAlways,DisallowMultipleComponent]
    public sealed partial class WindowsillWorkshop : MonoBehaviour
    {
        public enum View { Grow, Cuttings, Tray, Compare }
        [Serializable] public sealed class TraySlot
        {
            public bool pinned;
            public int seed;
            public PlantSpecimen specimen;
        }
        [SerializeField] ProceduralPlant mother, nursery;
        [SerializeField] GameObject motherPole, nurseryPole;
        [SerializeField] Camera viewCamera;
        [SerializeField] WindowsillScenery sill;
        [SerializeField] bool occupied, frozen, useNursery;
        [SerializeField] PlantSpecimen clipping;
        [SerializeField] TraySlot[] slots=new TraySlot[6];
        [SerializeField] int selectedSlot, roll, selectedNode;
        [SerializeField] View view;
        [SerializeField] float edgeDistance=.38f;
        [SerializeField] bool motherClimbs, nurseryClimbs=true;
        [SerializeField] int seed=37;
        [SerializeField] PlantSpecies species=PlantSpecies.Pothos;
        [SerializeField] float startingUnits=46;
        [SerializeField] int crowns=4;
        [SerializeField] bool rubberVariegated;
        [SerializeField] float speed=2;
        [NonSerialized] GameObject frozenDisplay, trayRoot;
        [NonSerialized] ProceduralPlant[] previews;
        [NonSerialized] bool playingClock, dirtyView=true;
        [NonSerialized] float elapsed, orbit=-9, zoom=1;
        [NonSerialized] Vector3 trayCenter;
        [NonSerialized] Vector2 trayExtents;
        [NonSerialized] string notice="";
        public static event Action<ProceduralPlant,bool> PrefabRequested;
        public bool Ready => mother && nursery && viewCamera && motherPole && nurseryPole;
        public ProceduralPlant Mother => mother;
        public ProceduralPlant Nursery => nursery;
        public ProceduralPlant Active => useNursery && occupied ? nursery : mother;
        public bool HasCutting => !string.IsNullOrEmpty(clipping?.growth);
        public bool NurseryOccupied => occupied;
        public bool NurseryFrozen => occupied && frozen;
        public View CurrentView => view;
        public int SelectedSlot => selectedSlot;
        public int SelectedNode => selectedNode;
        public IReadOnlyList<TraySlot> Slots => slots;
        public ProceduralPlant SelectedPreview => previews!=null ? previews[selectedSlot] : null;
        public Transform CameraTransform => viewCamera.transform;

        void OnEnable()
        {
            // Earlier imported scenes predate the explicit shelf reference.
            if (!sill && Ready)
            {
                foreach (var candidate in FindObjectsByType<WindowsillScenery>(FindObjectsSortMode.None))
                {
                    if (candidate.mossPole || candidate.gameObject.scene!=gameObject.scene) continue;
                    if (sill) { sill=null; break; }
                    sill=candidate;
                }
            }
            dirtyView=true;
        }
        void OnValidate() { dirtyView=true; }
        public void Sync()
        {
            if (!Ready) return;
            dirtyView=false;
            mother.transform.parent.localPosition=new Vector3(-.49f,0,-.34f+edgeDistance);
            if(sill) sill.SetPotPositions(mother.transform.position,nursery.transform.position);
            ConfigureSupports(mother,motherClimbs,motherPoleXZ,motherPole);
            ConfigureSupports(nursery,nurseryClimbs,nurseryPoleXZ,nurseryPole);
            motherPole.SetActive(motherClimbs); nurseryPole.SetActive(nurseryClimbs);
            nursery.gameObject.SetActive(occupied && !frozen);
            if (frozen && occupied && !frozenDisplay)
            {
                nursery.gameObject.SetActive(true);
                frozenDisplay=PlantInstances.Frozen(nursery,nursery.transform.parent);
                frozenDisplay.hideFlags=HideFlags.DontSave;
                nursery.gameObject.SetActive(false);
            }
            if ((!frozen || !occupied) && frozenDisplay) DisposeObject(ref frozenDisplay);
            if (view==View.Tray && HasTray) BuildTray();
            else ClearTray();
            RefreshPlacementPreview();
            UpdateCutGuide();
            FrameCamera();
        }

        void ConfigureSupports(ProceduralPlant plant,bool pole,Vector2 poleXZ,GameObject model)
        {
            var pos=plant.transform.parent.localPosition;
            var layout=PlantSupports.Windowsill;
            layout.shelfXZ=Rect.MinMaxRect(-1.25f-pos.x,-.34f-pos.z,1.25f-pos.x,.50f-pos.z);
            layout.pole=pole;
            layout.poleXZ=poleXZ;
            model.transform.localPosition=new Vector3(poleXZ.x,0,poleXZ.y);
            plant.Supports=layout;
        }

        public void SetEdgeDistance(float metres)
        { edgeDistance=Mathf.Clamp(metres,.16f,.68f); Sync(); }
        public void SetPole(bool enabled)
        {
            if (Active==nursery) nurseryClimbs=enabled; else motherClimbs=enabled;
            Sync();
        }
        public void FocusNursery(bool value)
        {
            useNursery=value && occupied;
            selectedNode=0; selectedVine=0;
            UpdateCutGuide(); FrameCamera();
        }

        public void Generate(PlantSpecies kind,int plantingSeed,double units,bool variegated=false)
        {
            if (!Ready) return;
            if (Active==nursery && frozen) return;
            species=kind; seed=plantingSeed; startingUnits=Mathf.Clamp((float)units,0,PlantGrowth.MaxOrgans);
            if(kind==PlantSpecies.RubberPlant) rubberVariegated=variegated;
            var recipe=PlantRecipe.Default;
            recipe.species=kind; recipe.seed=seed;
            recipe.variegated=variegated;
            recipe.crowns=kind==PlantSpecies.Monstera ? 1 : crowns;
            Sync();
            Active.Generate(recipe,units);
            selectedNode=0; selectedVine=0;
            UpdateCutGuide(); FrameCamera();
        }

        public void Advance(double units)
        {
            if (Active==nursery && frozen) return;
            if (IsComparing) { mother.Advance(units); nursery.Advance(units); }
            else Active.Advance(units);
            UpdateCutGuide(); FrameCamera();
        }

        public void SelectCutNode(int id)
        {
            if (!Active.Growth.CanCut(id)) throw new ArgumentException("Choose a viable vine node.");
            selectedNode=id; UpdateCutGuide();
        }

        public void TakeCutting()
        {
            if (HasCutting || (Active==nursery && frozen) || !Active.Growth.CanCut(selectedNode)) return;
            clipping=PlantSpecimen.Capture(Active, Active.Cut(selectedNode,plantUpright:true));
            placement=CuttingPlacement.Trailing;
            selectedNode=0; playingClock=false;
            notice="Cutting ready. The parent can grow from the bud below the cut.";
            Sync();
        }

        public void PlantCutting()
        {
            if (occupied || !HasCutting) return;
            nursery.gameObject.SetActive(true);
            clipping.PlantCutting(nursery,placement,nursery.Supports);
            nurseryClimbs=placement.pole; nurseryPoleXZ=nursery.Supports.poleXZ;
            clipping=null; occupied=true; frozen=false; useNursery=true;
            selectedNode=0; notice="Cutting planted in pot 2.";
            Sync();
        }

        public void EmptyNursery()
        {
            occupied=false; frozen=false; useNursery=false; playingClock=false; selectedNode=0;
            Sync();
        }

        public void SetView(View next)
        {
            if(IsComparing && next!=View.Compare) EndComparison();
            playingClock=false; elapsed=0; scroll=Vector2.zero; view=next;
            if (view==View.Tray && !HasTray) Reroll();
            else Sync();
        }

        void Update()
        {
            if (!Ready) return;
            if (dirtyView) Sync();
            if (Application.IsPlaying(gameObject) && playingClock)
            {
                elapsed+=Time.deltaTime;
                if (elapsed>=.12f) { Advance(elapsed*speed); elapsed=0; }
                if (IsComparing ? !mother.CanAdvance && !nursery.CanAdvance : !Active.CanAdvance) playingClock=false;
            }
            FrameCamera();
        }

        bool HasTray => slots!=null && slots.Length==6 && !string.IsNullOrEmpty(slots[0]?.specimen?.growth);
        public void Reroll()
        {
            if (slots==null || slots.Length!=6) slots=new TraySlot[6];
            roll++;
            var source=Active;
            for (int i=0;i<6;i++)
            {
                if (slots[i]!=null && slots[i].pinned && slots[i].specimen!=null) continue;
                var recipe=source.Growth.Recipe;
                recipe.seed=unchecked(recipe.seed+roll*7919+i*104729);
                var g=PlantGrowth.Sow(recipe,source.Conditions,source.Supports);
                // Compare identical biological ages under the current environment.
                g.Advance(source.Growth.Age/Mathf.Lerp(.2f,1.25f,source.Conditions.moisture),source.Conditions,source.Supports);
                slots[i]=new TraySlot {seed=recipe.seed,specimen=PlantSpecimen.Capture(source,g)};
            }
            if (view==View.Tray) { BuildTray(); FrameCamera(); }
        }
        public void SelectSlot(int index)
        {
            if (index<0 || index>=6) throw new ArgumentOutOfRangeException(nameof(index));
            selectedSlot=index;
        }
        public void Pin(int index,bool value)
        { if (!HasTray) Reroll(); slots[index].pinned=value; }

        public void PlaceVariation(bool living)
        {
            if (!HasTray) return;
            DisposeObject(ref frozenDisplay);
            occupied=true; frozen=!living;
            nursery.gameObject.SetActive(true);
            slots[selectedSlot].specimen.Restore(nursery);
            nurseryClimbs=nursery.Supports.pole;
            nurseryPoleXZ=nursery.Supports.poleXZ;
            useNursery=true; selectedNode=0; view=View.Grow;
            notice=living?"Living copy placed in pot 2.":"Static mesh placed in pot 2.";
            Sync();
        }

        static void DisposeObject(ref GameObject go)
        {
            if (!go) return;
            go.SetActive(false);
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            go=null;
        }
        void OnDisable()
        {
            playingClock=false;
            ClearTray(); DisposeObject(ref frozenDisplay); ClearGuide(); ClearPlacementPreview();
        }
    }
}
