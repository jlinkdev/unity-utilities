using System;
using System.Globalization;
using UnityEngine;

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
        static readonly Color Paper = new Color(.96f,.95f,.91f);
        static readonly Color Ink = new Color(.14f,.19f,.16f);
        static readonly Color Muted = new Color(.34f,.39f,.34f);
        static readonly Color Moss = new Color(.20f,.32f,.24f);
        static readonly Color Pale = new Color(.86f,.88f,.82f);

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
            if (!running) return;
            pending += Time.deltaTime;
            if (pending >= .1f)
            {
                specimen.Advance(pending * speed);
                pending = 0;
            }
            if (!specimen.CanAdvance) running = false;
        }

        float Scale => Mathf.Clamp(Mathf.Min(Screen.height / 710f, Screen.width / 880f), .64f, 1.7f);

        void LateUpdate()
        {
            if (!specimen || !specimen.CurrentMesh || !studioCamera) return;
            float scale = Scale;
            panelWidth = Mathf.Min(318, Screen.width / scale * .40f);
            var bounds = specimen.CurrentMesh.bounds;
            bounds.Encapsulate(new Bounds(new Vector3(0,-.13f,0),new Vector3(.31f,.28f,.31f)));
            var target = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
            var rotation = Quaternion.Euler(tilt, orbit, 0);
            float panelFraction = panelWidth * scale / Screen.width;
            float availAspect = studioCamera.aspect * (1-panelFraction);
            float horizontal = ProjectedExtent(bounds,rotation * Vector3.right);
            float vertical = ProjectedExtent(bounds,rotation * Vector3.up);
            studioCamera.orthographicSize = Mathf.Max(.39f,vertical*1.16f,
                horizontal / Mathf.Max(.1f, availAspect) * 1.12f) * zoom;
            float shift = studioCamera.orthographicSize * studioCamera.aspect * panelFraction;
            studioCamera.transform.SetPositionAndRotation(target - rotation * Vector3.right * shift -
                rotation * Vector3.forward * 3, rotation);
            if (showTopology) topology.Show(specimen);
            else topology.Hide();
        }

        void OnDisable() => topology.Release();

        static float ProjectedExtent(Bounds bounds, Vector3 axis) => Vector3.Dot(bounds.extents,
            new Vector3(Mathf.Abs(axis.x),Mathf.Abs(axis.y),Mathf.Abs(axis.z)));

        void OnGUI()
        {
            if (!specimen || specimen.Growth == null || seedText == null) return;
            SetupStyles();
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale,Scale,1));
            float height = Screen.height / Scale;
            float width = Screen.width / Scale;
            Fill(new Rect(0,0,panelWidth,height), Paper);
            float x = 20, w = panelWidth - 40;
            Label(new Rect(x,18,w,23), "PLANT STUDIO", small);
            string name = specimen.Growth.Profile.DisplayName;
            Label(new Rect(x,45,w,44), name, title);
            Label(new Rect(x,92,w,22), $"Seed {specimen.Growth.Recipe.seed}   /   Age {specimen.Growth.Age:0.0}", body);
            if (Press(new Rect(x,129,w*.5f-2,32),"Planting",!growingTab)) { growingTab = false; scroll = Vector2.zero; }
            if (Press(new Rect(x+w*.5f+2,129,w*.5f-2,32),"Growth",growingTab)) { growingTab = true; scroll = Vector2.zero; }

            float contentHeight = growingTab ? 429 : 488;
            var view = new Rect(0,174,panelWidth,Mathf.Max(50,height-229));
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

        void PlantingPanel(float x, float w)
        {
            Label(new Rect(x,0,w,24),"Species",body);
            float cell = (w-8)/3;
            for (int i=0; i<Species.Length; i++)
                if (Press(new Rect(x+(i%3)*(cell+4),22+(i/3)*22,cell,21),PlantSpeciesProfile.For(Species[i]).DisplayName,draft.species==Species[i]))
                {
                    draft.species = Species[i];
                    startUnits = i==0 ? 44 : draft.species==PlantSpecies.SpiderPlant ? 29 : 13;
                    Generate();
                }
            Label(new Rect(x,89,w,22),"Seed",body);
            seedText = GUI.TextField(new Rect(x,114,w*.56f,29),seedText,12);
            if (Press(new Rect(x+w*.58f,114,w*.42f,29),"New seed"))
            {
                seedText = new System.Random(Guid.NewGuid().GetHashCode()).Next().ToString();
                Generate();
            }
            draft.crowns = Mathf.RoundToInt(Slider(x,159,w,"Crown shoots",draft.crowns,1,7,"0"));
            draft.spread = Slider(x,211,w,"Spread",draft.spread,.5f,1.6f);
            draft.leafSize = Slider(x,263,w,"Leaf size",draft.leafSize,.55f,1.6f);
            startUnits = Slider(x,315,w,"Starting growth",startUnits,0,PlantGrowth.MaxOrgans,"0");
            Label(new Rect(x,366,w,22),"Leaf mesh detail",body);
            for (int i=0; i<DetailNames.Length; i++)
                if (Press(new Rect(x+i*(cell+4),391,cell,30),DetailNames[i],specimen.Detail==(LeafDetail)i))
                    specimen.Detail = (LeafDetail)i;
            if (Press(new Rect(x,439,w*.52f,36),"Generate plant",true)) Generate();
            if (Press(new Rect(x+w*.54f,439,w*.46f,36),"Long pothos")) ShowLongPothos();
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
            if (Press(new Rect(x,344,half,30),"Save specimen"))
            {
                PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(new Slot { plant=specimen.SaveState(), conditions=c,
                    detail=specimen.Detail, surface=specimen.Surface, care=specimen.Care }));
                PlayerPrefs.Save();
                feedback = "Specimen and conditions saved.";
            }
            GUI.enabled = PlayerPrefs.HasKey(SaveKey);
            if (Press(new Rect(x+half+6,344,half,30),"Load specimen")) LoadSlot();
            GUI.enabled = enabled;
            if (BakeRequested != null && Press(new Rect(x,388,w,32),"Save mesh with UVs…"))
                BakeRequested.Invoke(specimen);
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
            Fill(rect,selected ? Moss : Pale);
            return GUI.Button(rect,caption,selected ? lightButton : button);
        }

        void SetupStyles()
        {
            if (title != null) return;
            title = TextStyle(31,FontStyle.Bold,Ink);
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
            if (e.type == EventType.MouseDown && e.button == 0 && e.mousePosition.x > panel)
            { dragging = true; e.Use(); }
            else if (e.type == EventType.MouseUp && dragging) { dragging = false; e.Use(); }
            else if (e.type == EventType.MouseDrag && dragging)
            {
                orbit += e.delta.x*.38f;
                tilt = Mathf.Clamp(tilt + e.delta.y*.28f, -15, 66);
                e.Use();
            }
            else if (e.type == EventType.ScrollWheel && e.mousePosition.x > panel)
            { zoom = Mathf.Clamp(zoom*Mathf.Exp(e.delta.y*.055f),.42f,2.2f); e.Use(); }
        }
    }
}
