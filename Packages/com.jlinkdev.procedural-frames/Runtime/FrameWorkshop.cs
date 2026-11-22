using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace JLink.Frames
{
    /// <summary>Optional mouse-driven framing bench for a dedicated scene.</summary>
    [DisallowMultipleComponent]
    public sealed partial class FrameWorkshop : MonoBehaviour
    {
        [SerializeField] private StyleSheet sampleStyle;
        [SerializeField] private VisualTreeAsset sliderTemplate;
        [SerializeField] private VisualTreeAsset shelfTemplate;
        [SerializeField] private Texture2D[] mouldingPreviews;
        [SerializeField] private UIDocument sampleIdentity;
        public FrameDesign startingDesign;
        public Material mouldingMaterial, matMaterial, artworkMaterial;
        public ProceduralFrame Frame { get; private set; }
        public static event Action<ProceduralFrame> SaveMeshRequested;
        public static readonly Color Accent = new Color(.98f,.73f,.39f);

        GameObject stage;
        [SerializeField] ProceduralFrame authoredFrame;
        [SerializeField] Camera view;
        [SerializeField] UIDocument controlsDocument;
        VisualElement viewport, viewTools, identityHeading, sidebar;
        Label status, measure, handleValue;
        ProfilePad pad;
        PanelSettings settings;
        FrameRecipe R => Frame.recipe;
        FrameEditMode mode;
        float yaw = -14, pitch = 8, zoom;
        Rect lastArea;
        Vector3 orbitCentre;
        bool fittedEasel;
        int grabbed = -1;
        bool sculpting;
        float pickedT, startHeight, dragY;
        int pickedKey;
        Vector2 lastPointer;
        readonly List<string> undo = new List<string>(), redo = new List<string>();
        string before;
        const string SavedKey = "com.jlinkdev.procedural-frames.workshop.design.v1";
        readonly List<Action> sync = new List<Action>();
        static readonly string[] HandleNames = { "Artwork width", "Artwork height", "Rail width", "Border" };

        void OnEnable()
        {
            Frame = authoredFrame;
            if (!Frame) CreateRuntimeBench();
            else if (startingDesign) Frame.Apply(startingDesign.recipe);
            else Frame.Rebuild();
            if (!controlsDocument.panelSettings)
                controlsDocument.panelSettings = Resources.Load<PanelSettings>("JLinkFrames/WorkshopPanel");
            BuildUI(controlsDocument.rootVisualElement);
            Fit();
            Refresh();
        }

        // Retains programmatic construction for callers creating a workshop on an empty object.
        void CreateRuntimeBench()
        {
            stage = new GameObject("Framing bench");
            stage.transform.SetParent(transform,false);
            Frame = Child("Frame").AddComponent<ProceduralFrame>();
            Frame.GetComponent<MeshRenderer>().sharedMaterials = new[] { mouldingMaterial,matMaterial,artworkMaterial };
            Frame.Apply(startingDesign ? startingDesign.recipe : new FrameRecipe());
            view = Child("Bench camera").AddComponent<Camera>();
            view.orthographic = true;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.13f,.16f,.17f);
            view.nearClipPlane = .01f;
            view.depth = 5;
            var light = Child("Finish light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.localRotation = Quaternion.Euler(35,-30,0);
            var host = Child("Workshop controls");
            host.SetActive(false);
            settings = Instantiate(Resources.Load<PanelSettings>("JLinkFrames/WorkshopPanel"));
            controlsDocument = host.AddComponent<UIDocument>();
            controlsDocument.panelSettings = settings;
            host.SetActive(true);
        }

        GameObject Child(string label)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(stage.transform,false);
            return obj;
        }

        void OnDisable()
        {
            controlsDocument?.rootVisualElement?.Clear();
            if (stage) { stage.SetActive(false); Destroy(stage); }
            if (settings) Destroy(settings);
            sync.Clear(); undo.Clear(); redo.Clear();
            before = null; grabbed = -1; viewport = null; viewTools = null; identityHeading = null; sidebar = null;
            mode=FrameEditMode.Frame; bandTool.Select(-1); cornerTool.Release(); edgeTool.Release();
        }

        void SetMode(FrameEditMode next)
        {
            mode=next;
            edgeTool.Release(); cornerTool.Release(); bandTool.Arm(0);
            if (mode==FrameEditMode.Loops && bandTool.Current(R)==null && R.bands.Count>0)
                bandTool.Select(0);
            Refresh();
        }

        void BuildUI(VisualElement root)
        {
            root.styleSheets.Add(Resources.Load<StyleSheet>("JLinkFrames/Workshop"));
            if (sampleStyle)
            {
                root.styleSheets.Add(sampleStyle);
                root.AddToClassList("sample-studio");
            }
            root.AddToClassList("bench");
            sidebar = new VisualElement();
            sidebar.AddToClassList("side");
            sidebar.AddToClassList("sidebar");
            root.Add(sidebar);
            AddText(sidebar,"Frame workshop","title");
            var tabs = Row(sidebar);
            tabs.AddToClassList("tabs");
            var side = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            side.AddToClassList("inspector");
            sidebar.Add(side);
            measure = AddText(side,"","measure");
            var form = new VisualElement();
            side.Add(form);
            sync.Add(() => form.style.display = mode!=FrameEditMode.Frame ? DisplayStyle.None : DisplayStyle.Flex);
            BuildForm(form);
            AddBandControls(side);
            AddCornerControls(side);
            AddEdgeControls(side);
            Section(side,"UVs and export");
            var footer = new VisualElement();
            footer.AddToClassList("footer");
            sidebar.Add(footer);
            var history = Row(footer);
            var back = Button(history,"Undo",() => Restore(undo,redo));
            var forward = Button(history,"Redo",() => Restore(redo,undo));
            sync.Add(() => { back.SetEnabled(undo.Count > 0); forward.SetEnabled(redo.Count > 0); });
            BuildExports(side,footer);

            var work = new VisualElement(); work.AddToClassList("work"); root.Add(work);
            var tools = Row(work); tools.AddToClassList("toolbar");
            var formButton = Button(tabs,"Frame",() => SetMode(FrameEditMode.Frame));
            var loopsButton = Button(tabs,"Loops",() => SetMode(FrameEditMode.Loops));
            var edgeButton=Button(tabs,"Edges",()=>SetMode(FrameEditMode.Edges));
            var cornerButton = Button(tabs,"Corners",()=>SetMode(FrameEditMode.Corners));
            sync.Add(() => {
                formButton.EnableInClassList("selected",mode==FrameEditMode.Frame);
                edgeButton.EnableInClassList("selected",mode==FrameEditMode.Edges);
                loopsButton.EnableInClassList("selected",mode==FrameEditMode.Loops);
                cornerButton.EnableInClassList("selected",mode==FrameEditMode.Corners);
            });
            BuildViewport(work,tools);
            if (shelfTemplate) BuildMouldingShelf(work);
        }

        static readonly string[] MouldingNames = { "Flat", "Bevel", "Scoop", "Reverse scoop", "Ogee", "Stepped" };

        void BuildMouldingShelf(VisualElement work)
        {
            shelfTemplate.CloneTree(work);
            var cards = work.Q("preset-cards");
            for (int i = 0; i < MouldingNames.Length; i++)
            {
                var kind = (Moulding)i;
                var curve = FrameRecipe.Profile(kind);
                var card = new Button(() => Edit(() => R.profile = FrameRecipe.Profile(kind)))
                    { name = "Preset" + kind, tooltip = MouldingNames[i] };
                card.AddToClassList("preset");
                cards.Add(card);
                if (mouldingPreviews != null && i < mouldingPreviews.Length)
                    card.Add(new Image { image = mouldingPreviews[i], pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit });
                AddText(card,kind == Moulding.ReverseScoop ? "Rev. scoop" : MouldingNames[i],"preset-title").pickingMode = PickingMode.Ignore;
                sync.Add(() => card.EnableInClassList("active-preset", R.profile.Equals(curve)));
            }
        }

        void BuildForm(VisualElement side)
        {
            Section(side,"Outline");
            Choices(side,new[] { "Rectangle","Rounded","Oval" }, i => R.outline = (FrameOutline)i, () => (int)R.outline);
            var radius = Slider(side,"Corner (%)",1,45,() => R.cornerFraction*100,v => R.cornerFraction = v*.01f);
            sync.Add(() => radius.style.display = R.outline == FrameOutline.Rounded ? DisplayStyle.Flex : DisplayStyle.None);
            Section(side,"Moulding");
            if (!shelfTemplate)
                Choices(side,MouldingNames,i => R.profile = FrameRecipe.Profile((Moulding)i));
            pad = new ProfilePad(() => R,Begin,Changed,End);
            side.Add(pad);
            AddText(side,"Drag a point to sculpt. Double-click to add one. Right-click to remove it.","help");
            Slider(side,"Relief (cm)",0,20,() => R.relief*100,v => R.relief = v*.01f);
            Slider(side,"Depth (cm)",.6f,35,() => R.railDepth*100,v => R.railDepth = v*.01f);
            Section(side,"Mount");
            Choices(side,new[] { "Matted","Shadow box","Floater" },i => R.mount = (FrameMount)i,() => (int)R.mount);
            var easel = new Toggle("Easel back");
            side.Add(easel);
            sync.Add(()=>easel.SetValueWithoutNotify(R.easelBack));
            easel.RegisterValueChangedCallback(e=>Edit(()=>R.easelBack=e.newValue));
            var lean=Slider(side,"Lean angle (degrees)",5,25,()=>R.leanAngle,v=>R.leanAngle=v);
            sync.Add(()=>lean.style.display=R.easelBack ? DisplayStyle.Flex : DisplayStyle.None);
            var art = new Toggle("Artwork insert");
            side.Add(art);
            sync.Add(() => art.SetValueWithoutNotify(R.includeArtwork));
            art.RegisterValueChangedCallback(e => Edit(() => R.includeArtwork = e.newValue));
        }

        void BuildExports(VisualElement side,VisualElement footer)
        {
            Choices(side,new[] { "Tiled material","Painter atlas" }, i => R.uvLayout = (FrameUV)i, () => (int)R.uvLayout);
            var tile = Slider(side,"Tile size (cm)",.5f,200,() => R.tileSize*100,v => R.tileSize = v*.01f);
            sync.Add(() => tile.style.display = R.uvLayout == FrameUV.Tiled ? DisplayStyle.Flex : DisplayStyle.None);
            Button(footer,"Keep design",() => { PlayerPrefs.SetString(SavedKey,JsonUtility.ToJson(R)); PlayerPrefs.Save(); Notice("Saved on this device."); }).AddToClassList("primary");
            var files = Row(footer);
            var load = Button(files,"Recall",Recall);
            sync.Add(() => load.SetEnabled(PlayerPrefs.HasKey(SavedKey)));
            Button(files,"Export OBJ",Export);
            if (SaveMeshRequested != null)
                Button(files,"Save mesh",() => SaveMeshRequested?.Invoke(Frame)).tooltip = "Save the frame mesh as a Unity asset";
            status = AddText(footer,"","notice");
            status.style.display = DisplayStyle.None;
        }

        void BuildViewport(VisualElement work,VisualElement tools)
        {
            Button(tools,"Fit",Fit);
            Button(tools,"Front",() => { yaw = pitch = 0; UpdateView(); });
            var rear=Button(tools,"Back",()=>{yaw=150; pitch=10; UpdateView();});
            sync.Add(()=>rear.style.display=R.easelBack ? DisplayStyle.Flex : DisplayStyle.None);
            handleValue = AddText(tools,"","handle-value");
            viewport = new VisualElement { name = "FrameViewport" };
            viewport.AddToClassList("viewport"); work.Add(viewport);
            viewport.generateVisualContent += DrawHandles;
            viewport.RegisterCallback<PointerDownEvent>(Grab);
            viewport.RegisterCallback<PointerMoveEvent>(Move);
            viewport.RegisterCallback<PointerUpEvent>(e => { if (viewport.HasPointerCapture(e.pointerId)) viewport.ReleasePointer(e.pointerId); });
            viewport.RegisterCallback<PointerCaptureOutEvent>(_ => { bandTool.Release(); cornerTool.Release(); edgeTool.Release(); End(); grabbed = -1; sculpting = false; handleValue.text = ""; });
            viewport.RegisterCallback<PointerLeaveEvent>(_ => { bandTool.Leave(); viewport.MarkDirtyRepaint(); if (grabbed < 0) handleValue.text = ""; });
            viewport.RegisterCallback<WheelEvent>(e => {
                zoom = Mathf.Clamp(zoom*Mathf.Exp(e.delta.y*.035f),.035f,8); UpdateView(); e.StopPropagation(); });
            var foot = sampleStyle ? new Label() : AddText(work,"","foot");
            if (sampleStyle)
            {
                foot.AddToClassList("help");
                measure.parent.Insert(measure.parent.IndexOf(measure) + 1,foot);
                var overlay = new VisualElement { pickingMode = PickingMode.Ignore };
                overlay.AddToClassList("frame-tools");
                work.Add(overlay);
                tools.RemoveFromHierarchy();
                tools.RemoveFromClassList("toolbar");
                tools.AddToClassList("view-tools");
                tools.pickingMode = PickingMode.Ignore;
                overlay.Add(tools);
                viewTools = tools;
            }
            sync.Add(() => foot.text = mode==FrameEditMode.Edges ? "Drag an edge handle to sculpt the silhouette   ·   Adjust individual loops under Loops" : mode==FrameEditMode.Corners ? "Drag any corner handle to adjust all four   ·   Drag empty space to turn" : mode==FrameEditMode.Loops && bandTool.Current(R) is FrameBand band && band.ornament != BandOrnament.None ?
                "Drag along the rail: green changes detail size, coral changes spacing   ·   Gold adjusts loop width   ·   Blue adjusts height" :
                mode==FrameEditMode.Loops ? "Drag a loop to move   ·   Side dots change width   ·   Drag the blue dot up or down for height" :
                "Pull a dot to resize   ·   Drag a rail up or down to sculpt   ·   Drag empty space to turn   ·   Scroll to zoom");
        }

        void Notice(string text)
        {
            status.text = text;
            status.style.display = DisplayStyle.Flex;
            Refresh();
        }
        static Label AddText(VisualElement parent,string value,string style)
        {
            var label = new Label(value); label.AddToClassList(style); parent.Add(label); return label;
        }
        static void Section(VisualElement p,string text) => AddText(p,text,"section");
        static VisualElement Row(VisualElement p)
        {
            var row = new VisualElement(); row.AddToClassList("row"); p.Add(row); return row;
        }
        static Button Button(VisualElement p,string text,Action click)
        {
            var b = new Button(click) { text = text };
            b.AddToClassList("action-" + text.Replace(' ', '-').ToLowerInvariant());
            p.Add(b); return b;
        }
        void Choices(VisualElement p,string[] labels,Action<int> setter,Func<int> selected = null)
        {
            var row = Row(p);
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                var b = Button(row,labels[i],() => Edit(() => setter(index)));
                if (selected != null) sync.Add(() => b.EnableInClassList("selected",selected() == index));
            }
        }
        Slider Slider(VisualElement p,string text,float min,float max,Func<float> get,Action<float> set)
        {
            var field = sliderTemplate ? sliderTemplate.Instantiate().Q<Slider>() : null;
            if (field == null) field = new Slider { showInputField = true };
            field.label = text;
            field.lowValue = min;
            field.highValue = max;
            p.Add(field);
            sync.Add(() => field.SetValueWithoutNotify(get()));
            field.RegisterCallback<PointerDownEvent>(_ => Begin(),TrickleDown.TrickleDown);
            field.RegisterCallback<PointerCaptureOutEvent>(_ => End());
            field.RegisterCallback<PointerUpEvent>(_ => End());
            field.RegisterValueChangedCallback(e => Edit(() => set(e.newValue)));
            return field;
        }
        void Begin() { if (before == null) before = JsonUtility.ToJson(R); }
        void End()
        {
            if (before == null) return;
            if (before != JsonUtility.ToJson(R))
            {
                if (undo.Count >= 64) undo.RemoveAt(0);
                undo.Add(before); redo.Clear();
            }
            before = null; Refresh();
        }
        void Edit(Action apply)
        {
            bool atomic = before == null;
            if (atomic) Begin();
            apply(); Changed();
            if (atomic) End();
        }
        void Changed() { Frame.Rebuild(); Refresh(); }
        void Refresh()
        {
            if (fittedEasel!=R.easelBack) Fit();
            foreach (var update in sync) update();
            measure.text = $"{R.artworkSize.x*100:0.#} × {R.artworkSize.y*100:0.#} cm artwork   /   {R.railWidth*100:0.#} cm rail";
            pad.MarkDirtyRepaint(); viewport.MarkDirtyRepaint();
        }
        void Restore(List<string> from,List<string> to)
        {
            if (from.Count == 0) return;
            int i = from.Count-1;
            to.Add(JsonUtility.ToJson(R));
            Frame.Apply(JsonUtility.FromJson<FrameRecipe>(from[i]));
            from.RemoveAt(i);
            Refresh();
        }
        void Recall()
        {
            FrameRecipe saved;
            try { saved=JsonUtility.FromJson<FrameRecipe>(PlayerPrefs.GetString(SavedKey)); }
            catch (ArgumentException) { Notice("Could not read the saved frame design."); return; }
            if (saved==null) { Notice("Could not read the saved frame design."); return; }
            Edit(()=>Frame.recipe=saved);
        }

        void Export()
        {
            try
            {
                string name = "Frame-"+Guid.NewGuid().ToString("N").Substring(0,12)+".obj";
                string path = Path.Combine(Application.persistentDataPath,"Picture Frames",name);
                FrameObj.Write(Frame.GeneratedMesh,path);
                Notice("Saved "+name); status.tooltip = path; Debug.Log("Frame export: "+path,this);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            { Notice(ex.Message); }
        }
        void Fit()
        {
            fittedEasel=R.easelBack;
            Bounds bounds=Frame.GeneratedMesh.bounds;
            orbitCentre=bounds.center;
            Vector3 size = bounds.size;
            var rotation=Quaternion.Euler(pitch,yaw,0);
            float Span(Vector3 axis) => Mathf.Abs(axis.x)*size.x+Mathf.Abs(axis.y)*size.y+Mathf.Abs(axis.z)*size.z;
            zoom=Mathf.Max(Span(rotation*Vector3.right),Span(rotation*Vector3.up))*.70f;
            UpdateView();
        }
        void LateUpdate()
        {
            if (viewport != null && viewport.worldBound != lastArea) UpdateView();
            if (viewTools == null || !sampleIdentity) return;
            if (identityHeading?.panel == null)
                identityHeading = sampleIdentity.rootVisualElement?.Q(className:"sample-identity");
            if (identityHeading?.panel == null) return;
            // Let the header determine the minimum width while the stylesheet supplies the usual quarter-width rail.
            float titleWidth = identityHeading.worldBound.xMax - sidebar.worldBound.xMin + 12f;
            if (float.IsFinite(titleWidth)) sidebar.style.minWidth = titleWidth;
            float left = Mathf.Max(0f, identityHeading.worldBound.xMax - viewTools.parent.worldBound.xMin) + 12f;
            if (float.IsFinite(left)) viewTools.style.left = left;
        }
        void UpdateView()
        {
            var area = viewport.worldBound;
            if (!(area.width > 1 && area.height > 1)) return;
            lastArea = area;
            var panel = controlsDocument.rootVisualElement.worldBound;
            if (!(panel.width > 1 && panel.height > 1)) return;
            view.rect = new Rect((area.x-panel.x)/panel.width, 1-(area.yMax-panel.y)/panel.height,
                area.width/panel.width, area.height/panel.height);
            view.orthographicSize = zoom/Mathf.Min(1,area.width/area.height);
            var rot = Quaternion.Euler(pitch,yaw,0);
            view.transform.localRotation = rot;
            view.transform.localPosition = orbitCentre+rot*Vector3.back*(zoom*5+2);
            view.farClipPlane = Mathf.Max(view.clearFlags == CameraClearFlags.Skybox ? 150f : 0f, zoom*12+10);
            viewport.MarkDirtyRepaint();
        }
        Vector2 Project(Vector3 local)
        {
            var p = view.WorldToViewportPoint(Frame.DesignToWorld.MultiplyPoint3x4(local));
            return new Vector2(p.x*viewport.contentRect.width,(1-p.y)*viewport.contentRect.height);
        }
        internal static void Dot(Painter2D pen,Vector2 center,float radius,Color color)
        {
            pen.fillColor = color;
            pen.BeginPath(); pen.Arc(center,radius,0,360); pen.Fill();
        }
        void DrawHandles(MeshGenerationContext context)
        {
            if (mode==FrameEditMode.Edges)
            {
                for (int i=0;i<FrameEdgeTool.GripCount(R);i++)
                {
                    var p=Project(FrameEdgeTool.Handle(R,i));
                    Dot(context.painter2D,p,7,new Color(.13f,.16f,.17f));
                    Dot(context.painter2D,p,4.5f,FrameEdgeTool.ColorFor(FrameEdgeTool.Feature(R,i)));
                }
                return;
            }
            if (mode==FrameEditMode.Corners)
            {
                if (R.corners.Visible(R))
                    for (int i=0;i<4;i++)
                    {
                        Vector2 p=Project(R.corners.Handle(Frame,i,cornerTool.adjustment));
                        Dot(context.painter2D,p,8,new Color(.13f,.16f,.17f));
                        Dot(context.painter2D,p,5,Accent);
                    }
                return;
            }
            if (mode==FrameEditMode.Loops) { DrawBands(context.painter2D); return; }
            for (int i = 0; i < HandleNames.Length; i++)
            {
                Vector2 center = Project(FrameHandles.Position(R,(FrameHandle)i));
                Dot(context.painter2D,center,8,new Color(.13f,.16f,.17f));
                Dot(context.painter2D,center,5,grabbed == i ? Color.white : Accent);
            }
        }
        int Hit(Vector2 p)
        {
            float nearest = 15; int hit = -1;
            for (int i = 0; i < HandleNames.Length; i++)
            {
                float d = Vector2.Distance(p,Project(FrameHandles.Position(R,(FrameHandle)i)));
                if (d < nearest) { nearest = d; hit = i; }
            }
            return hit;
        }
        string HandleText(int hit)
        {
            switch ((FrameHandle)hit)
            {
                case FrameHandle.ArtWidth: return $"Artwork width   {R.artworkSize.x*100:0.#} cm";
                case FrameHandle.ArtHeight: return $"Artwork height   {R.artworkSize.y*100:0.#} cm";
                case FrameHandle.RailWidth: return $"Rail width   {R.railWidth*100:0.#} cm";
                case FrameHandle.Border: return (R.mount == FrameMount.Floater ? "Float gap   " : "Mat width   ")+$"{R.Border*100:0.#} cm";
                default: return "";
            }
        }
        void Grab(PointerDownEvent e)
        {
            if (e.button != 0) return;
            lastPointer = e.localPosition;
            if (mode==FrameEditMode.Corners || mode==FrameEditMode.Edges)
            {
                if (mode==FrameEditMode.Edges ? edgeTool.Press(R,lastPointer,Project) : cornerTool.Press(Frame,lastPointer,Project))
                { Begin(); Refresh(); }
                grabbed=-1; sculpting=false;
                viewport.CapturePointer(e.pointerId);
                return;
            }
            if (mode==FrameEditMode.Loops)
            {
                Begin();
                bool placing=bandTool.Adding!=0;
                if (bandTool.Press(Frame,BandRay(lastPointer),lastPointer,Project))
                {
                    if (placing) Changed(); else Refresh();
                }
                grabbed = -1; sculpting = false;
                viewport.CapturePointer(e.pointerId);
                return;
            }
            grabbed = Hit(lastPointer);
            sculpting = false;
            if (grabbed < 0)
            {
                var uv = new Vector3(lastPointer.x/viewport.contentRect.width,1-lastPointer.y/viewport.contentRect.height);
                sculpting = Frame.TryPick(view.ViewportPointToRay(uv),out pickedT,out _,out _);
                if (sculpting)
                {
                    pickedKey = -1;
                    float nearest = R.profile.length >= 48 ? float.MaxValue : .07f;
                    for (int i = 0; i < R.profile.length; i++)
                    {
                        float d = Mathf.Abs(R.profile[i].time-pickedT);
                        if (d <= nearest) { nearest = d; pickedKey = i; }
                    }
                    startHeight = pickedKey >= 0 ? R.profile[pickedKey].value : R.profile.Evaluate(pickedT);
                    dragY = 0;
                }
            }
            if (grabbed >= 0 || sculpting) Begin();
            viewport.CapturePointer(e.pointerId);
        }
        void Move(PointerMoveEvent e)
        {
            Vector2 p = e.localPosition;
            if (!viewport.HasPointerCapture(e.pointerId))
            {
                if (mode==FrameEditMode.Corners || mode==FrameEditMode.Edges) return;
                if (mode==FrameEditMode.Loops) { bandTool.Hover(Frame,BandRay(p)); viewport.MarkDirtyRepaint(); return; }
                int hit = Hit(p);
                handleValue.text = HandleText(hit);
                return;
            }
            Vector2 delta = p-lastPointer;
            lastPointer = p;
            if (mode==FrameEditMode.Edges && edgeTool.Dragging)
            {
                edgeTool.Drag(R,p); Changed();
            }
            else if (mode==FrameEditMode.Corners && cornerTool.dragging)
            {
                cornerTool.Drag(R,p);
                Changed();
            }
            else if (mode==FrameEditMode.Loops && bandTool.Dragging != FrameBandTool.Grip.None)
            {
                bandTool.Drag(R,p);
                Changed();
            }
            else if (grabbed >= 0)
            {
                var h = (FrameHandle)grabbed;
                Vector3 origin = FrameHandles.Position(R,h);
                Vector2 basis = Project(origin+FrameHandles.Axis(h))-Project(origin);
                FrameHandles.Pull(R,h,Vector2.Dot(delta,basis)/Mathf.Max(1,basis.sqrMagnitude));
                handleValue.text = HandleText(grabbed);
                Changed();
            }
            else if (sculpting)
            {
                dragY -= delta.y;
                if (Mathf.Abs(dragY) < 1 && pickedKey < 0) return;
                if (pickedKey < 0) pickedKey = R.profile.AddKey(new Keyframe(pickedT,startHeight));
                if (pickedKey < 0) return;
                if (R.relief <= 0) R.relief = .02f;
                var k = R.profile[pickedKey];
                k.value = Mathf.Clamp01(startHeight+dragY/100);
                R.profile.MoveKey(pickedKey,k);
                handleValue.text = $"Profile height   {k.value*R.relief*100:0.##} cm";
                Changed();
            }
            else
            {
                yaw += delta.x*.35f;
                pitch = Mathf.Clamp(pitch-delta.y*.35f,-75,75);
                UpdateView();
            }
        }
    }
}
