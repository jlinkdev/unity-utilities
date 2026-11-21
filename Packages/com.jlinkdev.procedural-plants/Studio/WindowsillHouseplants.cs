using System;
using System.Collections.Generic;
using UnityEngine;

namespace JLink.Plants.Studio
{
    public sealed partial class WindowsillWorkshop
    {
        public void ShowSpiderPreset()
        {
            if(!Ready || (Active==nursery && frozen)) return;
            if(!occupied) nurseryClimbs=false;
            SetPole(false);
            crowns=4;
            Generate(PlantSpecies.SpiderPlant,81,29);
        }

        public void ShowJadePreset()
        {
            if(!Ready || (Active==nursery && frozen)) return;
            if(!occupied) nurseryClimbs=false;
            SetPole(false);
            crowns=1;
            Generate(PlantSpecies.JadePlant, 23, 4);
        }

        public void ShowRubberPreset(bool variegated=false)
        {
            if(!Ready || (Active==nursery && frozen)) return;
            if(!occupied) nurseryClimbs=false;
            SetPole(false);
            crowns=1;
            Generate(PlantSpecies.RubberPlant,73,16,variegated);
        }

        public int[] ReadyPlantlets()
        {
            var ids=new List<int>();
            var g=Active.Growth;
            for(int i=0;i<g.OrganCount;i++)
            {
                int id=g.NodeAt(i).Id;
                if(g.CanDetachPlantlet(id)) ids.Add(id);
            }
            return ids.ToArray();
        }

        public void SelectPlantlet(int id)
        {
            if(!Active.Growth.CanDetachPlantlet(id)) throw new ArgumentException("Choose a mature attached plantlet.");
            selectedNode=id; UpdateCutGuide();
        }

        public void PlantSpiderBaby(int runnerId)
        {
            if(occupied || Active!=mother || !mother.Growth.CanDetachPlantlet(runnerId))
                throw new InvalidOperationException("Select a mature plantlet with pot 2 empty.");
            var specimen=PlantSpecimen.Capture(mother, mother.DetachPlantlet(runnerId));
            nursery.gameObject.SetActive(true);
            specimen.Restore(nursery);
            occupied=true; frozen=false; useNursery=true; nurseryClimbs=false;
            selectedNode=0; selectedVine=0; view=View.Grow; scroll=Vector2.zero; playingClock=false;
            notice="Plantlet rooted in pot 2.";
            Sync();
        }

        void PlantletPanel()
        {
            if(Active==nursery) { GUILayout.Label("Select the mother in pot 1 to fill the nursery."); return; }
            int[] ids=ReadyPlantlets();
            if(ids.Length==0) GUILayout.Label("Advance the mother until a runner has a mature plantlet.");
            else
            {
                int index=Mathf.Max(0,Array.IndexOf(ids,selectedNode));
                GUILayout.Label($"Plantlet {index+1} of {ids.Length}  ·  runner {ids[index]}");
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Previous")) SelectPlantlet(ids[(index+ids.Length-1)%ids.Length]);
                if(GUILayout.Button("Next")) SelectPlantlet(ids[(index+1)%ids.Length]);
                GUILayout.EndHorizontal();
                if(occupied)
                {
                    if(GUILayout.Button("Empty pot 2 for plantlet")) EmptyNursery();
                }
                else if(GUILayout.Button("Detach and plant in pot 2",GUILayout.Height(30))) PlantSpiderBaby(selectedNode);
            }
            GrowthButtons();
        }
    }
}
