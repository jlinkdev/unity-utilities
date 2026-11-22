using SliderDraw = System.Func<UnityEngine.Rect, string, string, float, float, float, float>;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace JLink.Plants.Studio
{
    public sealed partial class WindowsillWorkshop
    {
        static readonly string[] SpeciesNames = Array.ConvertAll(
            (PlantSpecies[])Enum.GetValues(typeof(PlantSpecies)), species => PlantSpeciesProfile.For(species).DisplayName);
        Vector2 scroll;
        float UiScale=>Mathf.Clamp(Mathf.Min(Screen.width/1000f,Screen.height/730f),.65f,1.65f);
        bool dragging, showPlantingSettings;
        GUIStyle heading,subheading;
        [SerializeField] GUISkin sampleSkin;
        [SerializeField] private UIDocument sampleIdentity;
        [SerializeField] private ScriptableObject sliderPresenter;
        private SliderDraw drawSampleSlider;
        private SliderDraw SampleSlider
        {
            get
            {
                if (drawSampleSlider == null && sliderPresenter)
                    drawSampleSlider = (SliderDraw)System.Delegate.CreateDelegate(typeof(SliderDraw), sliderPresenter, "Draw", false);
                return drawSampleSlider;
            }
        }

        GUISkin workshopSkin;
        [SerializeField] PlantWorkshopPanel workshop = new PlantWorkshopPanel();
        Button restoreButton, focusButton;
        Label workshopMessage;

        void UpdateWorkshop()
        {
            if (workshop.Open(sampleIdentity,SelectSpecies))
            {
                foreach (View tab in Enum.GetValues(typeof(View)))
                    workshop.Tab(tab.ToString(), () => SetView(tab));
                workshop.Tool("Fit",() => { orbit = -9; zoom = 1; FrameCamera(); },"action-fit");
                focusButton = workshop.Tool("Selected pot",() => SetPotCloseUp(!PotCloseUp));
                workshopMessage = workshop.Message();
                workshop.Action("Save windowsill",SaveWindow,"primary");
                restoreButton = workshop.Action("Restore",RestoreWindow,"action-undo");
            }
            if (!workshop.Mounted) return;
            workshop.Select((int)view,Active.Growth.Recipe.species,view==View.Grow && !(useNursery && frozen));
            focusButton.text = PotCloseUp ? "Both pots" : "Selected pot";
            focusButton.SetEnabled(view!=View.Tray);
            restoreButton.SetEnabled(PlayerPrefs.HasKey(SaveKey));
            workshopMessage.text = notice;
        }

        void SelectSpecies(PlantSpecies kind)
        {
            if (view != View.Grow || (useNursery && frozen)) return;
            switch (kind)
            {
                case PlantSpecies.SpiderPlant: ShowSpiderPreset(); break;
                case PlantSpecies.RubberPlant: ShowRubberPreset(); break;
                case PlantSpecies.JadePlant: ShowJadePreset(); break;
                default: Generate(kind,seed,kind==PlantSpecies.Pothos ? 44 : 16); break;
            }
        }

        void SaveWindow()
        {
            PlayerPrefs.SetString(SaveKey,SaveBench());
            PlayerPrefs.Save();
            notice = "Windowsill saved.";
        }

        void RestoreWindow()
        {
            try { RestoreBench(PlayerPrefs.GetString(SaveKey)); notice="Windowsill restored."; }
            catch(ArgumentException) { notice="Could not read the saved windowsill."; }
        }

        public void DrawControls()
        {
            if(!Ready || mother.Growth==null) return;
            GUILayout.BeginHorizontal();
            foreach(View tab in Enum.GetValues(typeof(View)))
            {
                bool selected=GUILayout.Toggle(view==tab,tab.ToString(),"Button",GUILayout.Height(29));
                if(selected && view!=tab) SetView(tab);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            DrawInspectorBody(false);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Save windowsill")) SaveWindow();
            bool enabled=GUI.enabled; GUI.enabled=PlayerPrefs.HasKey(SaveKey);
            if(GUILayout.Button("Restore")) RestoreWindow();
            GUI.enabled=enabled; GUILayout.EndHorizontal();
            if(!string.IsNullOrEmpty(notice)) GUILayout.Label(notice);
        }
        void DrawInspectorBody(bool useShelf)
        {
            if(view==View.Compare) ComparisonPanel();
            else if(view!=View.Tray)
            {
                GUILayout.BeginHorizontal();
                if(GUILayout.Toggle(!useNursery,"Pot 1","Button")) FocusIfChanged(false);
                bool old=GUI.enabled; GUI.enabled=occupied;
                if(GUILayout.Toggle(useNursery,"Pot 2","Button")) FocusIfChanged(true);
                GUI.enabled=old; GUILayout.EndHorizontal();
                var current=placementPreview ? placementPreview.Growth : Active.Growth;
                string name=current.Profile.DisplayName;
                GUILayout.Label(placementPreview ? $"{name} cutting  ·  {current.OrganCount} nodes" :
                    $"{name}  ·  {current.OrganCount} nodes  ·  age {current.Age:0.0}");
                if(view==View.Grow) GrowPanel(useShelf); else CutPanel();
            }
            else TrayPanel();
        }

        void FocusIfChanged(bool value) { if(useNursery!=value) FocusNursery(value); }
        float Slider(string label,float value,float min,float max,string format="0.00")
        {
            if (SampleSlider != null) return SampleSlider(GUILayoutUtility.GetRect(0, 35, GUILayout.ExpandWidth(true)),label,value.ToString(format),value,min,max);
            GUILayout.Label(label+"    "+value.ToString(format));
            return GUILayout.HorizontalSlider(value,min,max);
        }
        void GrowPanel(bool useShelf)
        {
            bool live=!(useNursery && frozen);
            if(!live) GUILayout.Label("Static copy · mesh and materials");
            if (useShelf) GUILayout.Label("Placement",workshop.SectionStyle);
            bool old=GUI.enabled; GUI.enabled=live;
            if (!useShelf)
            {
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Spider rosette")) ShowSpiderPreset();
                if(GUILayout.Button("Rubber tree")) ShowRubberPreset();
                if(GUILayout.Button("Jade shoot")) ShowJadePreset();
                GUILayout.EndHorizontal();
            }
            if(Active.Growth.Recipe.species==PlantSpecies.RubberPlant && GUILayout.Button(
                Active.Growth.Recipe.variegated?"Deep green preset":"Variegated preset"))
                ShowRubberPreset(!Active.Growth.Recipe.variegated);
            if(Active==mother && Active.Growth.Recipe.species==PlantSpecies.SpiderPlant && GUILayout.Button(HasCutting?"Place waiting cutting":"Choose a plantlet"))
                SetView(View.Cuttings);
            bool vine=Active.Growth.Profile.IsVine;
            if(vine)
            {
                bool pole=Active==nursery?nurseryClimbs:motherClimbs;
                bool next=GUILayout.Toggle(pole,"Moss pole");
                if(next!=pole) SetPole(next);
            }
            if(!useNursery)
            {
                float edge=Slider("Pot to front edge (m)",edgeDistance,.16f,.68f);
                if(!Mathf.Approximately(edge,edgeDistance)) SetEdgeDistance(edge);
            }
            if (useShelf)
            {
                GUILayout.Space(7);
                GUILayout.Label("Growing conditions",workshop.SectionStyle);
            }
            var conditions=Active.Conditions;
            bool care=GUILayout.Toggle(Active.Care.Enabled,"Simulate plant care");
            if(care!=Active.Care.Enabled) Active.SetCareEnabled(care);
            if(care)
            {
                float stress=Active.Care.Stress(Active.Growth.Recipe.species);
                GUILayout.Label($"Soil {Active.Care.Soil:P0}  ·  "+(stress>.05f?"Leaves wilting":"Hydrated"));
                if(GUILayout.Button("Water pot")) Active.Water();
            }
            else conditions.moisture=Slider("Moisture",conditions.moisture,0,1);
            float angle=Mathf.Atan2(conditions.lightDirection.x,-conditions.lightDirection.z)*Mathf.Rad2Deg;
            float direction=Slider("Growth light bearing",angle,-180,180,"0°");
            if(!Mathf.Approximately(angle,direction))
                conditions.lightDirection=Quaternion.Euler(0,-direction,0)*new Vector3(0,.55f,-.84f);
            if(!conditions.Equals(Active.Conditions)) { Active.Conditions=conditions; UpdateCutGuide(); }
            GrowthButtons();
            if(live && vine) TipsPanel();
            GUILayout.Space(8);
            if (useShelf)
            {
                if (GUILayout.Button(showPlantingSettings ? "Hide seed settings" : "Seed settings"))
                    showPlantingSettings = !showPlantingSettings;
            }
            else GUILayout.Label("New planting");
            if (!useShelf || showPlantingSettings) DrawPlantingSettings(useShelf);
            GUI.enabled=old;
            if(occupied && GUILayout.Button("Empty pot 2")) EmptyNursery();
        }

        void DrawPlantingSettings(bool useShelf)
        {
            if (!useShelf) species=(PlantSpecies)GUILayout.SelectionGrid((int)species,SpeciesNames,2);
            if(species==PlantSpecies.RubberPlant) rubberVariegated=GUILayout.Toggle(rubberVariegated,"Variegated rubber leaves");
            GUILayout.BeginHorizontal(); GUILayout.Label("Seed",GUILayout.Width(40));
            string input=GUILayout.TextField(seed.ToString(),12);
            if(int.TryParse(input,out int parsed)) seed=parsed;
            if(GUILayout.Button("Next",GUILayout.Width(52))) seed=unchecked(seed+1);
            GUILayout.EndHorizontal();
            if(species!=PlantSpecies.Monstera) crowns=Mathf.RoundToInt(Slider("Crown shoots",crowns,1,7,"0"));
            startingUnits=Slider("Starting growth",startingUnits,0,96,"0");
            if(GUILayout.Button("Plant this seed",GUILayout.Height(28)))
                Generate(species,seed,startingUnits,species==PlantSpecies.RubberPlant && rubberVariegated);
            if(GUILayout.Button("Climbing Monstera"))
            { SetPole(true); Generate(PlantSpecies.Monstera,53,16); }
        }
        void GrowthButtons()
        {
            bool old=GUI.enabled; GUI.enabled=old && (Active.CanAdvance ||
                (IsComparing && (mother.CanAdvance || nursery.CanAdvance)));
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Step +1")) Advance(1);
            if(GUILayout.Button("Grow +6")) Advance(6);
            if(Application.IsPlaying(gameObject) && GUILayout.Button(playingClock?"Pause":"Play")) playingClock=!playingClock;
            GUILayout.EndHorizontal();
            if(playingClock) speed=Slider("Units per second",speed,.2f,8);
            GUI.enabled=old;
        }
        void CutPanel()
        {
            if(!HasCutting && Active.Growth.Recipe.species==PlantSpecies.SpiderPlant)
            { PlantletPanel(); return; }
            int[] ids=CuttableNodes();
            if(!HasCutting && ids.Length>0)
            {
                int index=Mathf.Max(0,Array.IndexOf(ids,selectedNode));
                int next=Mathf.RoundToInt(Slider("Cut node "+selectedNode+" · position",index,0,Mathf.Max(1,ids.Length-1),"0"));
                next=Mathf.Clamp(next,0,ids.Length-1);
                if(selectedNode!=ids[next]) SelectCutNode(ids[next]);
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Previous")) SelectCutNode(ids[Mathf.Max(0,next-1)]);
                if(GUILayout.Button("Next")) SelectCutNode(ids[Mathf.Min(ids.Length-1,next+1)]);
                GUILayout.EndHorizontal();
                GUILayout.Label($"{Active.Growth.PreviewCut(selectedNode).Length} nodes in highlighted cutting");
                bool old=GUI.enabled; GUI.enabled=!HasCutting;
                if(GUILayout.Button("Cut highlighted branch",GUILayout.Height(28))) TakeCutting();
                GUI.enabled=old;
            }
            else if(!HasCutting) GUILayout.Label("Select a non-basal pothos node or grow a Monstera until it has a mature leaf with an aerial root.");
            if(HasCutting)
            {
                if(GUILayout.Button("Compare this cutting in both pots")) StartComparison();
                if(occupied && GUILayout.Button("Empty pot 2 for cutting")) EmptyNursery();
                if(!occupied) PlacementPanel();
                bool old=GUI.enabled; GUI.enabled=!occupied;
                if(GUILayout.Button("Plant cutting in pot 2")) PlantCutting();
                GUI.enabled=old;
            }
            if(!HasCutting && !(useNursery && frozen)) GrowthButtons();
        }
        void TrayPanel()
        {
            if(!HasTray) { if(GUILayout.Button("Generate six variations")) Reroll(); return; }
            GUILayout.Label("Six seeds · shared scale");
            for(int i=0;i<6;i++)
            {
                GUILayout.BeginHorizontal();
                if(GUILayout.Toggle(selectedSlot==i,(i+1)+"   Seed "+slots[i].seed,"Button",GUILayout.Height(25))) SelectSlot(i);
                slots[i].pinned=GUILayout.Toggle(slots[i].pinned,"Pin",GUILayout.Width(50));
                GUILayout.EndHorizontal();
            }
            if(GUILayout.Button("Reroll unpinned seeds",GUILayout.Height(28))) Reroll();
            GUILayout.Space(8); GUILayout.Label(occupied?"Replace pot 2 with selected copy":"Place selected copy in pot 2");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Living copy")) PlaceVariation(true);
            if(GUILayout.Button("Static copy")) PlaceVariation(false);
            GUILayout.EndHorizontal();
            if(PrefabRequested!=null && SelectedPreview)
            {
                GUILayout.Label("Save selected prefab");
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("Living prefab…")) PrefabRequested.Invoke(SelectedPreview,true);
                if(GUILayout.Button("Static prefab…")) PrefabRequested.Invoke(SelectedPreview,false);
                GUILayout.EndHorizontal();
            }
        }

        void OnGUI()
        {
            if(!Application.IsPlaying(gameObject) || !Ready) return;
            var oldMatrix=GUI.matrix; var oldSkin=GUI.skin;
            float scale=workshop.Ready ? workshop.Scale : UiScale, w=Screen.width/scale, h=Screen.height/scale;
            float panel=workshop.Ready ? workshop.RailWidth : Mathf.Min(294,w*.44f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            if (workshop.Ready)
            {
                GUI.skin = workshop.InspectorSkin(sampleSkin ? sampleSkin : oldSkin);
                Rect body = workshop.Body;
                GUILayout.BeginArea(body);
                scroll = GUILayout.BeginScrollView(scroll,false,false,GUIStyle.none,GUI.skin.verticalScrollbar);
                GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(1,body.width-10)));
                DrawInspectorBody(true);
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            else DrawStandaloneInspector(panel,h,scale);
            DrawViewport(w,h,panel,scale);
            GUI.matrix=oldMatrix; GUI.skin=oldSkin;
        }

        void DrawStandaloneInspector(float panel,float h,float scale)
        {
            if(!workshopSkin)
            {
                workshopSkin=Instantiate(sampleSkin ? sampleSkin : GUI.skin);
                workshopSkin.hideFlags=HideFlags.DontSave;
                workshopSkin.label.wordWrap=true;
                workshopSkin.label.normal.textColor=new Color(.83f,.88f,.80f);
                workshopSkin.button.fontSize=13; workshopSkin.button.fixedHeight=25;
                workshopSkin.label.fontSize=13;
                heading=new GUIStyle(workshopSkin.label){fontSize=28,fontStyle=FontStyle.Bold};
                subheading=new GUIStyle(workshopSkin.label){fontSize=12};
            }
            GUI.skin=workshopSkin;
            float header = PlantStudio.LayoutInspectorHeader(sampleIdentity,scale,panel);
            if (header <= 0f)
            {
                Color oldColor=GUI.color;
                GUI.color=sampleSkin ? new Color(.09f,.12f,.16f) : new Color(.12f,.18f,.15f);
                GUI.DrawTexture(new Rect(0,0,panel,h),Texture2D.whiteTexture);
                GUI.color=oldColor;
            }
            float controlsTop = Mathf.Max(12f,header);
            GUILayout.BeginArea(new Rect(14,controlsTop,panel-28,Mathf.Max(1f,h-controlsTop-10f)));
            if (header <= 0f)
            {
                GUILayout.Label("PLANT STUDIO",subheading);
                GUILayout.Label("Windowsill",heading);
            }
            scroll=GUILayout.BeginScrollView(scroll);
            DrawControls();
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }

        void DrawViewport(float w,float h,float panel,float scale)
        {
            if(view==View.Tray && previews!=null)
                for(int i=0;i<6;i++)
                {
                    var pos=viewCamera.WorldToScreenPoint(previews[i].transform.position+Vector3.down*.275f);
                    if(GUI.Toggle(new Rect(pos.x/scale-24,h-pos.y/scale,48,24),selectedSlot==i,
                        (i+1)+(slots[i].pinned?"  *":""),"Button")) SelectSlot(i);
                }
            else
            {
                if(!workshop.Ready && GUI.Button(new Rect(w-206,12,108,27),PotCloseUp?"Both pots":"Selected pot")) SetPotCloseUp(!PotCloseUp);
                if(!workshop.Ready && GUI.Button(new Rect(w-93,12,80,27),"Fit view")) {orbit=-9;zoom=1;FrameCamera();}
                var e=Event.current;
                if((workshop.Ready ? workshop.OverViewport(e.mousePosition) : e.mousePosition.x>panel) && e.type==EventType.MouseDown && e.button==0) {dragging=true;e.Use();}
                else if(dragging && e.type==EventType.MouseDrag) {orbit=Mathf.Clamp(orbit+e.delta.x*.27f,-46,46);e.Use();}
                else if(e.type==EventType.MouseUp) dragging=false;
                else if((workshop.Ready ? workshop.OverViewport(e.mousePosition) : e.mousePosition.x>panel) && e.type==EventType.ScrollWheel)
                {zoom=Mathf.Clamp(zoom*Mathf.Exp(e.delta.y*.05f),.35f,1.7f);e.Use();}
            }
        }
        void OnDestroy()
        {
            if(workshopSkin) { if(Application.isPlaying) Destroy(workshopSkin); else DestroyImmediate(workshopSkin); }
        }
    }
}
