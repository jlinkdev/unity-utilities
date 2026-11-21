using System;
using UnityEngine;

namespace JLink.Plants.Studio
{
    public sealed partial class WindowsillWorkshop
    {
        const string SaveKey="JLink.Plants.Windowsill.2";

        [Serializable] sealed class BenchSave
        {
            public int version=3;
            public PlantSpecimen mother,nursery;
            public bool occupied,frozen,motherPole,nurseryPole;
            public float edge;
            public PlantSpecimen clipping;
            public TraySlot[] slots;
            public int roll,selectedSlot;
            public CuttingPlacement placement;
            public Vector2 motherPoleXZ,nurseryPoleXZ;
            public string previousBench;
            public bool showTips,useNursery;
            public int selectedVine;
            public View view;
        }

        public string SaveBench() => JsonUtility.ToJson(new BenchSave {
            mother=PlantSpecimen.Capture(mother),nursery=occupied?PlantSpecimen.Capture(nursery):null,
            occupied=occupied,frozen=frozen,motherPole=motherClimbs,nurseryPole=nurseryClimbs,edge=edgeDistance,
            clipping=clipping,slots=slots,roll=roll,selectedSlot=selectedSlot,
            placement=placement,motherPoleXZ=motherPoleXZ,nurseryPoleXZ=nurseryPoleXZ,previousBench=previousBench,
            showTips=showTips,selectedVine=selectedVine,view=view,useNursery=useNursery
        });

        public void RestoreBench(string json)
        {
            var saved=JsonUtility.FromJson<BenchSave>(json);
            if(saved==null || (saved.version!=2 && saved.version!=3) || saved.mother==null || saved.slots==null || saved.slots.Length!=6 ||
                saved.selectedSlot<0 || saved.selectedSlot>=6 || !float.IsFinite(saved.edge)) throw new ArgumentException("Invalid windowsill save.");
            saved.mother.OpenGrowth();
            if(saved.occupied)
            {
                if(saved.nursery==null) throw new ArgumentException("Missing nursery specimen.");
                saved.nursery.OpenGrowth();
            }
            if(!string.IsNullOrEmpty(saved.clipping?.growth) && !saved.clipping.OpenGrowth().IsRootedCutting)
                throw new ArgumentException("Invalid pending vine cutting.");
            if(saved.version==3)
            {
                saved.placement.SupportsFor(PlantSupports.Windowsill);
                if(!float.IsFinite(saved.motherPoleXZ.x) || !float.IsFinite(saved.motherPoleXZ.y) ||
                   !float.IsFinite(saved.nurseryPoleXZ.x) || !float.IsFinite(saved.nurseryPoleXZ.y) ||
                   !Enum.IsDefined(typeof(View),saved.view)) throw new ArgumentException("Invalid windowsill layout.");
            }
            int populatedSlots=0;
            foreach(var slot in saved.slots)
            {
                if(string.IsNullOrEmpty(slot?.specimen?.growth)) continue;
                slot.specimen.OpenGrowth();
                populatedSlots++;
            }
            if(populatedSlots!=0 && populatedSlots!=saved.slots.Length)
                throw new ArgumentException("Incomplete variation tray.");
            DisposeObject(ref frozenDisplay);
            saved.mother.Restore(mother);
            if(saved.occupied) { nursery.gameObject.SetActive(true); saved.nursery.Restore(nursery); }
            occupied=saved.occupied; frozen=occupied && saved.frozen; useNursery=occupied && saved.useNursery;
            motherClimbs=saved.motherPole; nurseryClimbs=saved.nurseryPole;
            edgeDistance=saved.edge; slots=saved.slots; roll=saved.roll; selectedSlot=saved.selectedSlot;
            clipping=saved.clipping; selectedNode=0; playingClock=false; elapsed=0;
            placement=saved.version==2 ? CuttingPlacement.Trailing : saved.placement;
            if(saved.version==2) placement.pole=saved.nurseryPole;
            previousBench=saved.previousBench;
            motherPoleXZ=saved.version==2 ? PlantSupports.Windowsill.poleXZ : saved.motherPoleXZ;
            nurseryPoleXZ=saved.version==2 ? PlantSupports.Windowsill.poleXZ : saved.nurseryPoleXZ;
            view=IsComparing ? View.Compare : saved.view;
            showTips=saved.showTips; selectedVine=saved.selectedVine;
            var recipe=Active.Growth.Recipe;
            species=recipe.species; seed=recipe.seed; crowns=recipe.crowns; rubberVariegated=recipe.variegated;
            Sync();
        }
    }
}
