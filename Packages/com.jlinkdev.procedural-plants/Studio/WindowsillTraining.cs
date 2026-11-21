using System;
using UnityEngine;

namespace JLink.Plants.Studio
{
    public sealed partial class WindowsillWorkshop
    {
        [SerializeField] Vector2 motherPoleXZ=PlantSupports.Windowsill.poleXZ, nurseryPoleXZ=PlantSupports.Windowsill.poleXZ;
        [SerializeField] CuttingPlacement placement=CuttingPlacement.Trailing;
        [SerializeField] string previousBench;
        [SerializeField] bool showTips;
        [SerializeField] int selectedVine;
        GameObject previewObject;
        ProceduralPlant placementPreview;
        string tipReport;

        public bool IsComparing => !string.IsNullOrEmpty(previousBench);
        public bool ShowingTips => showTips;
        public string TipReport => tipReport;
        public CuttingPlacement Placement => placement;
        public ProceduralPlant PlacementPreview => placementPreview;

        public void SetPlacement(CuttingPlacement pose)
        {
            if(!HasCutting) return;
            pose.poleXZ=pose.SupportsFor(PlantSupports.Windowsill).poleXZ;
            placement=pose;
            RefreshPlacementPreview(); UpdateCutGuide(); FrameCamera();
        }

        public void SetTipDisplay(bool enabled,int vine=0)
        {
            showTips=enabled;
            selectedVine=Mathf.Clamp(vine,0,Active.Growth.Recipe.crowns);
            UpdateCutGuide();
        }

        void ClearPlacementPreview()
        {
            DisposeObject(ref previewObject);
            placementPreview=null;
        }

        void RefreshPlacementPreview()
        {
            if (view!=View.Cuttings || !HasCutting || occupied)
            {
                ClearPlacementPreview();
                nurseryPole.SetActive(nurseryClimbs);
                nurseryPole.transform.localPosition=new Vector3(nurseryPoleXZ.x,0,nurseryPoleXZ.y);
                return;
            }
            if (!previewObject)
            {
                previewObject=Child("Cutting placement preview",nursery.transform.parent);
                placementPreview=previewObject.AddComponent<ProceduralPlant>();
            }
            clipping.PlantCutting(placementPreview,placement,nursery.Supports);
            var layout=placementPreview.Supports;
            nurseryPole.SetActive(layout.pole);
            nurseryPole.transform.localPosition=new Vector3(layout.poleXZ.x,0,layout.poleXZ.y);
        }

        public void StartComparison(bool usePending=true)
        {
            if(IsComparing || !Ready) return;
            // A pending clipping can be compared directly. Otherwise reproduce the
            // eight-node clipping from the original Windowsill pothos.
            PlantSpecimen source=clipping;
            if(!HasCutting || !usePending)
            {
                var recipe=PlantRecipe.Default;
                var conditions=PlantConditions.Default;
                var layout=PlantSupports.Windowsill;
                layout.shelfXZ=Rect.MinMaxRect(-.76f,-.38f,1.74f,.46f);
                var stock=PlantGrowth.Sow(recipe,conditions,layout);
                stock.Advance(46,conditions,layout);
                source=new PlantSpecimen {growth=stock.Cut(26,true).ToJson(),conditions=conditions,
                    supports=layout,detail=LeafDetail.Standard,surface=PlantSurface.PBR};
            }
            previousBench=SaveBench();
            playingClock=false; elapsed=0;
            ClearPlacementPreview(); DisposeObject(ref frozenDisplay);
            occupied=true; frozen=false; useNursery=false; view=View.Compare;
            potCloseUp=false; zoom=1;
            motherClimbs=true; nurseryClimbs=false;
            edgeDistance=.34f+nursery.transform.parent.localPosition.z;
            motherPoleXZ=nurseryPoleXZ=PlantSupports.Windowsill.poleXZ;
            nursery.gameObject.SetActive(true);
            Sync();
            source.PlantCutting(mother,CuttingPlacement.Climbing,mother.Supports);
            source.PlantCutting(nursery,CuttingPlacement.Trailing,nursery.Supports);
            clipping=null; selectedNode=0; selectedVine=0; showTips=true;
            notice=""; Sync();
        }

        public void EndComparison()
        {
            if(!IsComparing) return;
            string original=previousBench;
            RestoreBench(original);
            if(view==View.Compare) SetView(HasCutting ? View.Cuttings : View.Grow);
            notice="Original windowsill restored.";
        }

        void PlacementPanel()
        {
            GUILayout.Label("Preview cutting in pot 2");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Let it trail")) SetPlacement(CuttingPlacement.Trailing);
            if(GUILayout.Button("Train to pole")) SetPlacement(CuttingPlacement.Climbing);
            GUILayout.EndHorizontal();
            var pose=placement;
            pose.yaw=Slider("Rotate",pose.yaw,-180,180,"0°");
            pose.tilt=Slider("Tilt",pose.tilt,-85,85,"0°");
            pose.pole=GUILayout.Toggle(pose.pole,"Place moss pole");
            if(pose.pole)
            {
                pose.poleXZ.x=Slider("Pole left / right (m)",pose.poleXZ.x,-.105f,.105f);
                pose.poleXZ.y=Slider("Pole front / back (m)",pose.poleXZ.y,-.105f,.105f);
            }
            if(!pose.Equals(placement)) SetPlacement(pose);
            GUILayout.Label(tipReport);
        }

        void TipsPanel()
        {
            bool next=GUILayout.Toggle(showTips,"Show growing tip and contacts");
            if(next!=showTips) SetTipDisplay(next);
            if(!showTips) return;
            var g=Active.Growth;
            if(!g.IsRootedCutting && g.Recipe.crowns>1 && g.Profile.IsVine)
            {
                int vine=Mathf.RoundToInt(Slider("Vine (0 = next bud)",selectedVine,0,g.Recipe.crowns,"0"));
                if(vine!=selectedVine) SetTipDisplay(true,vine);
            }
            GUILayout.Label(tipReport);
        }

        void ComparisonPanel()
        {
            if(!IsComparing)
            {
                GUILayout.Label(HasCutting ? "Compare two copies of your pending cutting." :
                    "Compare two copies of the eight-node Windowsill pothos cutting.");
                GUILayout.Label("Pot 1 has a moss pole. Pot 2 grows freely. Both receive the same light, moisture and time.");
                if(GUILayout.Button("Start side-by-side",GUILayout.Height(30))) StartComparison();
                if(HasCutting && GUILayout.Button("Use example pothos instead")) StartComparison(false);
                return;
            }
            GUILayout.Label("Same cutting · shared growth clock");
            GUILayout.BeginHorizontal();
            if(GUILayout.Toggle(!useNursery,"1 · Climbing","Button")) FocusIfChanged(false);
            string other=nursery.Growth.Recipe.species==PlantSpecies.Pothos ? "2 · Trailing" : "2 · Free";
            if(GUILayout.Toggle(useNursery,other,"Button")) FocusIfChanged(true);
            GUILayout.EndHorizontal();
            GrowthButtons(); TipsPanel();
            GUILayout.Label($"Pot 1   {mother.Growth.OrganCount} nodes     Pot 2   {nursery.Growth.OrganCount} nodes");
            if(GUILayout.Button("Return to original windowsill")) EndComparison();
        }
    }
}
