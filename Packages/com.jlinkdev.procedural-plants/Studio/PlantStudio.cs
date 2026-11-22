using SliderDraw = System.Func<UnityEngine.Rect, string, string, float, float, float, float>;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace JLink.Plants.Studio
{
    public sealed class PlantStudio : MonoBehaviour
    {
        [SerializeField] ProceduralPlant specimen;
        [SerializeField] Camera studioCamera;
        public static event Action<ProceduralPlant> BakeRequested;
        const string SaveKey = "JLink.Plants.Studio.Specimen.v1";
        static readonly PlantSpecies[] Species = (PlantSpecies[])Enum.GetValues(typeof(PlantSpecies));
        static readonly string[] DetailNames = { "Simple", "Standard", "Close-up" };
        Color Paper => sampleSkin ? new Color(.09f,.12f,.16f) : new Color(.96f,.95f,.91f);
        Color Ink => sampleSkin ? new Color(.92f,.95f,.97f) : new Color(.14f,.19f,.16f);
        Color Muted => sampleSkin ? new Color(.68f,.75f,.81f) : new Color(.34f,.39f,.34f);
        Color Moss => sampleSkin ? new Color(.86f,.63f,.40f) : new Color(.20f,.32f,.24f);
        Color Pale => sampleSkin ? new Color(.19f,.24f,.30f) : new Color(.86f,.88f,.82f);

        PlantRecipe draft;
        string seedText;
        float startUnits, azimuth, elevation, speed = 1, orbit = -13, tilt = 12, zoom = 1;
        float pending;
        bool running, growingTab, dragging;
        bool showTopology;
        readonly LeafTopology topology = new LeafTopology();
        string feedback = "";
        Vector2 scroll;
        GUIStyle title, small, body, button, lightButton;
        float panelWidth;
        [SerializeField] PlantWorkshopPanel workshop = new PlantWorkshopPanel();
        Button restoreButton, surfaceButton, wireButton;
        Label workshopMessage;

        [Serializable]
        sealed class Slot
        {
            public string plant;
            public PlantConditions conditions;
            public PlantCare care;
            public LeafDetail detail = LeafDetail.Standard;
            public PlantSurface surface;
        }

        void Start()
        {
            draft = specimen.Growth.Recipe;
            seedText = draft.seed.ToString();
            startUnits = draft.species == PlantSpecies.Pothos ? 44 : 13;
            ReadLightAngles();
        }

        void ReadLightAngles()
        {
            var light = specimen.Conditions.lightDirection.normalized;
            azimuth = Mathf.Atan2(light.x, -light.z) * Mathf.Rad2Deg;
            elevation = Mathf.Asin(Mathf.Clamp(light.y,-1,1)) * Mathf.Rad2Deg;
        }

        void Update()
        {
            if (specimen && specimen.Growth != null && seedText != null)
            {
                if (workshop.Open(sampleIdentity,SelectSpecies)) BindWorkshop();
                if (workshop.Mounted)
                {
                    workshop.Select(growingTab ? 1 : 0, specimen.Growth.Recipe.species);
                    surfaceButton.EnableInClassList("chosen",specimen.Surface == PlantSurface.PBR);
                    wireButton.EnableInClassList("chosen",showTopology);
                    restoreButton.SetEnabled(PlayerPrefs.HasKey(SaveKey));
                    workshopMessage.text = feedback;
                }
            }
            if (!running) return;
            pending += Time.deltaTime;
            if (pending >= .1f)
            {
                specimen.Advance(pending * speed);
                pending = 0;
            }
            if (!specimen.CanAdvance) running = false;
        }

        float Scale => workshop.Ready ? workshop.Scale : Mathf.Clamp(Mathf.Min(Screen.height / 710f, Screen.width / 880f), .64f, 1.7f);

        void LateUpdate()
        {
            if (!specimen || !specimen.CurrentMesh || !studioCamera) return;
            float scale = Scale;
            panelWidth = workshop.Ready ? workshop.RailWidth : Mathf.Min(318, Screen.width / scale * .40f);
            var bounds = specimen.CurrentMesh.bounds;
            bounds.Encapsulate(new Bounds(new Vector3(0,-.13f,0),new Vector3(.31f,.28f,.31f)));
            var target = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
            var rotation = Quaternion.Euler(tilt, orbit, 0);
            float panelFraction = panelWidth * scale / Screen.width;
            float bottomFraction = workshop.Ready ? workshop.ShelfHeight / workshop.PanelHeight : 0;
            float availableHeight = Mathf.Max(.1f,1-bottomFraction);
            float availAspect = studioCamera.aspect * (1-panelFraction) / availableHeight;
            float horizontal = ProjectedExtent(bounds,rotation * Vector3.right);
            float vertical = ProjectedExtent(bounds,rotation * Vector3.up);
            studioCamera.orthographicSize = Mathf.Max(.39f,vertical*1.16f,
                horizontal / Mathf.Max(.1f, availAspect) * 1.12f) * zoom / availableHeight;
            float shift = studioCamera.orthographicSize * studioCamera.aspect * panelFraction;
            studioCamera.transform.SetPositionAndRotation(target - rotation * Vector3.right * shift -
                rotation * Vector3.up * (studioCamera.orthographicSize * bottomFraction) -
                rotation * Vector3.forward * 3, rotation);
            if (showTopology) topology.Show(specimen);
            else topology.Hide();
        }

        void OnDisable()
        {
            topology.Release();
            workshop.Close();
            title = null;
        }

        void BindWorkshop()
        {
            title = null;
            workshop.Tab("Planting",() => SetTab(false));
            workshop.Tab("Growth",() => SetTab(true));
            workshop.Tool("Fit",() => { zoom = 1; orbit = -13; tilt = 12; },"action-fit");
            surfaceButton = workshop.Tool("PBR",() => specimen.Surface = specimen.Surface == PlantSurface.PBR ? PlantSurface.Studio : PlantSurface.PBR);
            wireButton = workshop.Tool("Wireframe",() => showTopology = !showTopology);
            workshopMessage = workshop.Message();
            workshop.Action("Generate plant",Generate,"primary");
            var row = workshop.ActionRow();
            workshop.RowAction(row,"Save specimen",SaveSlot,"action-save-design");
            restoreButton = workshop.RowAction(row,"Restore",LoadSlot);
            var presets = workshop.ActionRow();
            workshop.RowAction(presets,"Long pothos",ShowLongPothos);
            if (BakeRequested != null)
                workshop.RowAction(presets,"Save mesh",() => BakeRequested?.Invoke(specimen),"action-save-mesh");
        }

        void SetTab(bool growth)
        {
            growingTab = growth;
            scroll = Vector2.zero;
        }

        void SelectSpecies(PlantSpecies species)
        {
            draft.species = species;
            startUnits = species == PlantSpecies.Pothos ? 44 : species == PlantSpecies.SpiderPlant ? 29 : 13;
            Generate();
        }

        static float ProjectedExtent(Bounds bounds, Vector3 axis) => Vector3.Dot(bounds.extents,
            new Vector3(Mathf.Abs(axis.x),Mathf.Abs(axis.y),Mathf.Abs(axis.z)));

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

        void OnGUI()
        {
            var previous = GUI.skin;
            if (workshop.Ready) GUI.skin = workshop.InspectorSkin(sampleSkin ? sampleSkin : previous);
            else if (sampleSkin) GUI.skin = sampleSkin;
            try { DrawStudio(); }
            finally { GUI.skin = previous; }
        }

        internal static float LayoutInspectorHeader(UIDocument identity, float guiScale, float width)
        {
            var root = identity ? identity.rootVisualElement : null;
            var header = root?.Q(className: "sample-identity");
            if (header?.panel == null || !(header.worldBound.height > 0f) || !(root.worldBound.height > 0f)) return 0f;
            // Keep the sidebar background behind the UI Toolkit title. IMGUI draws after that panel.
            var backdrop = root.Q("inspector-backdrop");
            if (backdrop != null) backdrop.style.width = width * guiScale * root.worldBound.width / Screen.width;
            return (header.worldBound.yMax + 8f) * Screen.height / (root.worldBound.height * guiScale);
        }

        void DrawStudio()
        {
            if (!specimen || specimen.Growth == null || seedText == null) return;
            SetupStyles();
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale,Scale,1));
            float height = Screen.height / Scale;
            float width = Screen.width / Scale;
            if (workshop.Ready)
            {
                DrawWorkshopInspector();
                HandleOrbit(panelWidth);
                GUI.matrix = previousMatrix;
                return;
            }
            float header = LayoutInspectorHeader(sampleIdentity,Scale,panelWidth);
            if (header <= 0f) Fill(new Rect(0,0,panelWidth,height), Paper);
            float x = 20, w = panelWidth - 40;
            if (header <= 0f) Label(new Rect(x,18,w,23), "PLANT STUDIO", small);
            float speciesTop = header > 0f ? header : 45f;
            string name = specimen.Growth.Profile.DisplayName;
            Label(new Rect(x,speciesTop,w,44), name, title);
            Label(new Rect(x,speciesTop+47,w,22), $"Seed {specimen.Growth.Recipe.seed}   /   Age {specimen.Growth.Age:0.0}", body);
            if (Press(new Rect(x,speciesTop+84,w*.5f-2,32),"Planting",!growingTab)) { growingTab = false; scroll = Vector2.zero; }
            if (Press(new Rect(x+w*.5f+2,speciesTop+84,w*.5f-2,32),"Growth",growingTab)) { growingTab = true; scroll = Vector2.zero; }

            float contentHeight = growingTab ? 429 : 488;
            float scrollTop = speciesTop + 129f;
            var view = new Rect(0,scrollTop,panelWidth,Mathf.Max(1,height-scrollTop-55f));
            bool scrollBar = contentHeight > view.height;
            float contentWidth = panelWidth - (scrollBar ? 18 : 0);
            scroll = GUI.BeginScrollView(view,scroll,new Rect(0,0,contentWidth,contentHeight),false,false);
            if (growingTab) GrowthPanel(x,contentWidth-40); else PlantingPanel(x,contentWidth-40);
            GUI.EndScrollView();
            Fill(new Rect(x,height-48,w,1), Pale);
            Label(new Rect(x,height-44,w,39),feedback.Length > 0 ? feedback : "Drag to orbit   /   Scroll to zoom",small);
            if (Press(new Rect(width-95,16,78,29),"Fit view")) { zoom = 1; orbit = -13; tilt = 12; }
            if (Press(new Rect(width-208,16,106,29),"Leaf wireframe",showTopology)) showTopology = !showTopology;
            if (Press(new Rect(width-282,16,68,29),"PBR",specimen.Surface == PlantSurface.PBR))
                specimen.Surface = specimen.Surface == PlantSurface.PBR ? PlantSurface.Studio : PlantSurface.PBR;
            if (showTopology && specimen.CurrentMesh)
            {
                var countRect = new Rect(width-208,51,191,25);
                Fill(countRect,Paper);
                Label(new Rect(countRect.x+6,countRect.y,185,25),
                    $"{specimen.CurrentMesh.GetIndexCount(1)/3:N0} foliage triangles",small);
            }
            HandleOrbit(panelWidth);
            GUI.matrix = previousMatrix;
        }

        void DrawWorkshopInspector()
        {
            var rect = workshop.Body;
            float y = rect.y;
            Label(new Rect(rect.x,y,rect.width,23), specimen.Growth.Profile.DisplayName,workshop.SectionStyle);
            string details = showTopology && specimen.CurrentMesh
                ? $"{specimen.CurrentMesh.GetIndexCount(1)/3:N0} foliage triangles"
                : $"Seed {specimen.Growth.Recipe.seed}   ·   Age {specimen.Growth.Age:0.0}";
            Label(new Rect(rect.x,y+23,rect.width,18),details,small);
            var view = new Rect(rect.x,y+51,rect.width,Mathf.Max(1f,rect.height-51));
            float contentHeight = growingTab ? 337 : 295;
            float w = Mathf.Max(1f,view.width - (contentHeight > view.height ? 10 : 0));
            scroll = GUI.BeginScrollView(view,scroll,new Rect(0,0,w,contentHeight),false,false,GUIStyle.none,GUI.skin.verticalScrollbar);
            if (growingTab) GrowthPanel(0,w); else PlantingPanel(0,w);
            GUI.EndScrollView();
        }

        void PlantingPanel(float x, float w)
        {
            float cell = (w-8)/3;
            if (!workshop.Ready)
            {
                Label(new Rect(x,0,w,24),"Species",body);
                for (int i=0; i<Species.Length; i++)
                    if (Press(new Rect(x+(i%3)*(cell+4),22+(i/3)*22,cell,21),PlantSpeciesProfile.For(Species[i]).DisplayName,draft.species==Species[i]))
                        SelectSpecies(Species[i]);
            }
            float top = workshop.Ready ? 0f : 89f;
            Label(new Rect(x,top,w,22),"Seed",body);
            seedText = GUI.TextField(new Rect(x,top+20,w*.56f,29),seedText,12);
            if (Press(new Rect(x+w*.58f,top+20,w*.42f,29),"New seed"))
            {
                seedText = new System.Random(Guid.NewGuid().GetHashCode()).Next().ToString();
                Generate();
            }
            draft.crowns = Mathf.RoundToInt(Slider(x,top+58,w,"Crown shoots",draft.crowns,1,7,"0"));
            draft.spread = Slider(x,top+103,w,"Spread",draft.spread,.5f,1.6f);
            draft.leafSize = Slider(x,top+148,w,"Leaf size",draft.leafSize,.55f,1.6f);
            startUnits = Slider(x,top+193,w,"Starting growth",startUnits,0,PlantGrowth.MaxOrgans,"0");
            Label(new Rect(x,top+239,w,22),"Leaf mesh detail",body);
            for (int i=0; i<DetailNames.Length; i++)
                if (Press(new Rect(x+i*(cell+4),top+260,cell,30),DetailNames[i],specimen.Detail==(LeafDetail)i))
                    specimen.Detail = (LeafDetail)i;
            if (workshop.Ready) return;
            if (Press(new Rect(x,top+300,w*.52f,36),"Generate plant",true)) Generate();
            if (Press(new Rect(x+w*.54f,top+300,w*.46f,36),"Long pothos")) ShowLongPothos();
        }

        public void ShowLongPothos()
        {
            draft = PlantRecipe.Default;
            draft.crowns = 5;
            draft.seed = 63;
            draft.leafSize = .91f;
            seedText = draft.seed.ToString();
            startUnits = 90;
            zoom = 1;
            Generate();
        }

        void Generate()
        {
            if (!int.TryParse(seedText, out draft.seed)) { feedback = "Enter a whole-number seed."; return; }
            specimen.Generate(draft,startUnits);
            running = false;
            pending = 0;
            feedback = "Generated " + specimen.Growth.Profile.DisplayName + ".";
        }

        void GrowthPanel(float x,float w)
        {
            var c = specimen.Conditions;
            azimuth = Slider(x,0,w,"Light direction",azimuth,-180,180,"0°");
            elevation = Slider(x,53,w,"Light elevation",elevation,-85,90,"0°");
            c.lightDirection = Quaternion.AngleAxis(-azimuth, Vector3.up) *
                new Vector3(0, Mathf.Sin(elevation*Mathf.Deg2Rad), -Mathf.Cos(elevation*Mathf.Deg2Rad));
            c.lightResponse = Slider(x,106,w,"Light response",c.lightResponse,0,1);
            c.moisture = Slider(x,159,w,"Moisture",c.moisture,0,1);
            specimen.Conditions = c;
            speed = Slider(x,212,w,"Growth units / second",speed,.1f,8f);
            float half = (w-6)*.5f;
            bool enabled = GUI.enabled;
            GUI.enabled = specimen.CanAdvance;
            if (Press(new Rect(x,270,half,32),running ? "Pause" : "Grow",true)) { running = !running; pending = 0; }
            if (Press(new Rect(x+half+6,270,half,32),"Step +1")) specimen.Advance(1);
            GUI.enabled = enabled;
            Label(new Rect(x,309,w,22),$"{specimen.Growth.OrganCount} / {specimen.Growth.Capacity} organs" +
                (specimen.Growth.FullyGrown ? "    ·    Mature" : ""),small);
            if (workshop.Ready) return;
            if (Press(new Rect(x,344,half,30),"Save specimen")) SaveSlot();
            GUI.enabled = PlayerPrefs.HasKey(SaveKey);
            if (Press(new Rect(x+half+6,344,half,30),"Load specimen")) LoadSlot();
            GUI.enabled = enabled;
            if (BakeRequested != null && Press(new Rect(x,388,w,32),"Save mesh with UVs…"))
                BakeRequested.Invoke(specimen);
        }

        void SaveSlot()
        {
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(new Slot { plant=specimen.SaveState(), conditions=specimen.Conditions,
                detail=specimen.Detail, surface=specimen.Surface, care=specimen.Care }));
            PlayerPrefs.Save();
            feedback = "Specimen and conditions saved.";
        }

        void LoadSlot()
        {
            try
            {
                var slot = JsonUtility.FromJson<Slot>(PlayerPrefs.GetString(SaveKey));
                if (slot == null) throw new ArgumentException();
                new PlantSpecimen {growth=slot.plant,conditions=slot.conditions,detail=slot.detail,
                    surface=slot.surface,supports=specimen.Supports,care=slot.care}.Restore(specimen);
                draft = specimen.Growth.Recipe;
                seedText = draft.seed.ToString();
                ReadLightAngles();
                running = false;
                pending = 0;
                feedback = "Saved specimen restored.";
            }
            catch (ArgumentException) { feedback = "Could not read this specimen."; }
        }

        float Slider(float x,float y,float w,string caption,float value,float min,float max,string format="0.00")
        {
            if (SampleSlider != null) return SampleSlider(new Rect(x,y,w,35),caption,value.ToString(format,CultureInfo.InvariantCulture),value,min,max);
            Label(new Rect(x,y,w*.78f,22),caption,body);
            Label(new Rect(x+w*.80f,y,w*.20f,22),value.ToString(format,CultureInfo.InvariantCulture),small);
            return GUI.HorizontalSlider(new Rect(x,y+28,w,18),value,min,max);
        }

        static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color = previous;
        }

        static void Label(Rect rect,string text,GUIStyle style) => GUI.Label(rect,text,style);

        bool Press(Rect rect,string caption,bool selected=false)
        {
            if (workshop.Ready)
            {
                bool next = GUI.Toggle(rect,selected,caption,GUI.skin.button);
                return next != selected;
            }
            Fill(rect,selected ? Moss : Pale);
            return GUI.Button(rect,caption,selected ? lightButton : button);
        }

        void SetupStyles()
        {
            if (title != null) return;
            title = TextStyle(workshop.Ready ? 16 : 31,FontStyle.Bold,Ink);
            small = TextStyle(12,FontStyle.Normal,Muted);
            body = TextStyle(14,FontStyle.Normal,Ink);
            button = TextStyle(13,FontStyle.Bold,Ink);
            lightButton = TextStyle(13,FontStyle.Bold,Paper);
            button.alignment = lightButton.alignment = TextAnchor.MiddleCenter;
        }

        static GUIStyle TextStyle(int size,FontStyle weight,Color color)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight,
                alignment = TextAnchor.MiddleLeft, wordWrap = true };
            style.normal.textColor = style.hover.textColor = style.active.textColor = color;
            return style;
        }

        void HandleOrbit(float panel)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && (workshop.Ready ? workshop.OverViewport(e.mousePosition) : e.mousePosition.x > panel))
            { dragging = true; e.Use(); }
            else if (e.type == EventType.MouseUp && dragging) { dragging = false; e.Use(); }
            else if (e.type == EventType.MouseDrag && dragging)
            {
                orbit += e.delta.x*.38f;
                tilt = Mathf.Clamp(tilt + e.delta.y*.28f, -15, 66);
                e.Use();
            }
            else if (e.type == EventType.ScrollWheel && (workshop.Ready ? workshop.OverViewport(e.mousePosition) : e.mousePosition.x > panel))
            { zoom = Mathf.Clamp(zoom*Mathf.Exp(e.delta.y*.055f),.42f,2.2f); e.Use(); }
        }
    }
}
