using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace JLink.Pots
{
    public sealed partial class PotteryWorkshop
    {
        static readonly PotForm[] Forms = (PotForm[])Enum.GetValues(typeof(PotForm));
        static readonly string[] Tabs = { "Form", "Clay", "Handles", "UVs", "Shelf" };
        static readonly Color Accent = new Color(.99f,.72f,.43f);
        static readonly Color Pale = new Color(.94f,.94f,.86f);
        int tab;
        bool guides = true;
        Texture2D[] thumbnails;
        PanelSettings panelSettings;
        VisualElement rootUI, canvas, controls, tabRow;
        Button undoButton, redoButton, guidesButton;
        Label title, message;
        readonly List<Action> refreshFields = new List<Action>();

        Rect CanvasArea
        {
            get
            {
                if (canvas != null && canvas.worldBound.width > 1) return canvas.worldBound;
                float w = Mathf.Min(252,Screen.width*.36f);
                return new Rect(w,0,Mathf.Max(1, Screen.width-w),Mathf.Max(1, Screen.height-94));
            }
        }

        void MakeInterface()
        {
            panelSettings = Instantiate(Resources.Load<PanelSettings>("JLinkPots/WorkshopPanel"));
            var host = Child("Workshop interface");
            host.SetActive(false);
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            host.SetActive(true);
            rootUI = document.rootVisualElement;
            rootUI.styleSheets.Add(Resources.Load<StyleSheet>("JLinkPots/Workshop"));
            rootUI.AddToClassList("workshop");
            var side = Box(rootUI,"sidebar");
            Text(side,"Pottery Workshop","heading");
            title = Text(side,P.title,"design-name");
            tabRow = Box(side,"tabs");
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                MakeButton(tabRow,Tabs[i],() => { tab = index; placingHandle = false; ShowPanel(); });
            }
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("inspector");
            side.Add(scroll);
            controls = scroll.contentContainer;
            var footer = Box(side,"footer");
            message = Text(footer,"","notice");
            var history = Box(footer,"row");
            undoButton = MakeButton(history,"Undo",() => History(false));
            redoButton = MakeButton(history,"Redo",() => History(true));
            MakeButton(footer,"Keep a copy",() => { KeepCopy(); message.text = "Kept on your shelf"; if (tab == 4) ShowPanel(); }).AddToClassList("primary");
            if (ExportRequested != null)
            {
                var exports = Box(footer,"row");
                MakeButton(exports,"Save design",() => ExportRequested?.Invoke(Vessel,false));
                MakeButton(exports,"Save mesh",() => ExportRequested?.Invoke(Vessel,true));
            }

            var work = Box(rootUI,"work-area");
            var viewport = Box(work,"viewport");
            canvas = Box(viewport,"sculpt-canvas");
            canvas.name = "SculptCanvas";
            canvas.focusable = true;
            canvas.generateVisualContent += DrawGuides;
            canvas.RegisterCallback<PointerDownEvent>(Grab);
            canvas.RegisterCallback<PointerMoveEvent>(Move);
            canvas.RegisterCallback<PointerUpEvent>(Release);
            canvas.RegisterCallback<PointerCaptureOutEvent>(_ => {
                if (drag != Drag.None) EndGesture();
                drag = Drag.None; Refresh();
            });
            canvas.RegisterCallback<WheelEvent>(e => {
                framing = Mathf.Clamp(framing*Mathf.Exp(e.delta.y*.035f),.035f,6);
                UpdateView(); canvas.MarkDirtyRepaint(); e.StopPropagation();
            });
            canvas.RegisterCallback<PointerLeaveEvent>(_ => { hovered = -1; canvas.MarkDirtyRepaint(); });
            var tools = Box(viewport,"view-tools");
            MakeButton(tools,"Fit",Fit);
            MakeButton(tools,"Side",() => SetView(0,0));
            MakeButton(tools,"Above",() => SetView(yaw,76));
            var section = new Button { text = "Slice" };
            section.clicked += () => {
                cutaway = !cutaway; section.EnableInClassList("chosen",cutaway); UpdateView(); canvas.MarkDirtyRepaint();
            };
            tools.Add(section);
            guidesButton = MakeButton(tools,"Guides",() => {
                guides = !guides; guidesButton.EnableInClassList("chosen",guides); canvas.MarkDirtyRepaint();
            });
            guidesButton.AddToClassList("chosen");
            var tray = Box(work,"tray");
            foreach (PotForm form in Forms)
            {
                var card = MakeButton(tray,"",() => { Replace(PotRecipe.Create(form)); tab = 0; ShowPanel(); });
                card.name = "Preset" + form;
                card.AddToClassList("preset");
                var image = new Image { image = thumbnails[(int)form], scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                card.Add(image);
                Text(card,form.ToString(),"preset-title").pickingMode = PickingMode.Ignore;
            }
            rootUI.RegisterCallback<GeometryChangedEvent>(_ => {
                side.style.width = Mathf.Min(252,Screen.width*.36f);
                rootUI.EnableInClassList("compact",Screen.height < 500);
            });
            canvas.RegisterCallback<GeometryChangedEvent>(_ => { UpdateView(); canvas.MarkDirtyRepaint(); });
            rootUI.RegisterCallback<KeyDownEvent>(e => {
                if (!(e.ctrlKey || e.commandKey) || (e.keyCode != KeyCode.Z && e.keyCode != KeyCode.Y)) return;
                History(e.shiftKey || e.keyCode == KeyCode.Y); e.StopPropagation();
            });
            ShowPanel();
        }

        void History(bool forward)
        {
            gestureStart = null;
            Restore(forward ? redo : undo,forward ? undo : redo);
            ShowPanel();
        }

        void SetView(float azimuth,float elevation)
        {
            yaw = azimuth; pitch = elevation; UpdateView(); canvas.MarkDirtyRepaint();
        }

        static VisualElement Box(VisualElement parent,string style)
        {
            var e = new VisualElement(); e.AddToClassList(style); parent.Add(e); return e;
        }

        static Label Text(VisualElement parent,string value,string style)
        {
            var e = new Label(value); e.AddToClassList(style); parent.Add(e); return e;
        }

        static Button MakeButton(VisualElement parent,string value,Action action)
        {
            var button = new Button(action) { text = value };
            parent.Add(button); return button;
        }

        void Refresh()
        {
            if (canvas == null) return;
            title.text = P.title;
            undoButton.SetEnabled(undo.Count > 0);
            redoButton.SetEnabled(redo.Count > 0);
            foreach (var update in refreshFields) update();
            canvas.MarkDirtyRepaint();
        }

        void Change(Action edit, bool remakePanel = false, bool continuous = false)
        {
            BeginGesture();
            edit();
            Vessel.Rebuild();
            if (!continuous) EndGesture();
            message.text = "";
            if (remakePanel) ShowPanel(); else Refresh();
        }

        void ShowPanel()
        {
            controls.Clear();
            refreshFields.Clear();
            hovered = -1;
            for (int i = 0; i < tabRow.childCount; i++) tabRow[i].EnableInClassList("chosen",i == tab);
            if (tab == 0)
            {
                Section("Pottery wheel");
                Hint("Grab the wall. Pull right to widen the whole band or push left to pinch it.");
                Number("Band height (%)",() => wheelBandPercent,v => wheelBandPercent = v,12,80,true);
                Section("Dimensions");
                Metric("Height",() => P.height,v => P.height = v,.04f,2);
                Metric("Body width",() => P.width,v => P.width = v,.06f,2);
                Number("Depth / width",() => P.depthRatio,v => P.depthRatio = v,.25f,1.8f);
                Section("Footprint");
                Choices(new[] {"Round","Soft square","Faceted"},(int)P.footprint,i => P.footprint = (Footprint)i);
                if (P.footprint == Footprint.Faceted) Number("Sides",() => P.facets,v => P.facets = Mathf.RoundToInt(v),3,16,true);
                if (selected > 0 && selected < P.rings.Count-1)
                    MakeButton(controls,"Remove selected ring",() => Change(() => { P.rings.RemoveAt(selected); selected = -1; },true));
                Hint("Pull a diamond to shape a ring. Double-click the right outline to add one.");
            }
            else if (tab == 1)
            {
                Section("Thumb sculpt");
                Hint("Drag left on the wall to press in, or right to pull out. Right-drag to orbit.");
                Number("Brush size",() => brushSize,v => brushSize = v,.08f,.42f);
                Section("Lip");
                Choices(new[] {"Plain","Rolled","Flared"},(int)P.lip,i => P.lip = (Lip)i);
                if (P.lip != Lip.Plain) Number("Fullness",() => P.rimSize,v => P.rimSize = v,.01f,.2f);
                Section("Base and interior");
                Choices(new[] {"Flat","Foot ring"},(int)P.baseStyle,i => P.baseStyle = (BaseStyle)i);
                var drain = new Toggle("Drainage hole") { value = P.drainage };
                controls.Add(drain); drain.RegisterValueChangedCallback(e => Change(() => P.drainage = e.newValue));
                var saucer = new Toggle("Matching saucer") { value = P.saucer };
                controls.Add(saucer); saucer.RegisterValueChangedCallback(e => Change(() => P.saucer = e.newValue));
                Metric("Floor",() => P.floor,v => P.floor = v,.001f,P.height*.35f);
                Metric("Wall",() => P.wall,v => P.wall = v,.001f,P.width*.18f);
                Section("Fluting");
                Number("Depth",() => P.fluteDepth,v => P.fluteDepth = v,0,.18f);
                Number("Repeats",() => P.flutes,v => P.flutes = Mathf.RoundToInt(v),3,32,true);
                Number("Twist",() => P.twist,v => P.twist = v,-1,1);
            }
            else if (tab == 2)
            {
                Section("Loop handles");
                MakeButton(controls,placingHandle ? "Cancel placement" : "Place on wall",() => { placingHandle = !placingHandle; ShowPanel(); });
                Hint(placingHandle ? "Click the outside wall to attach a loop here." : "Pull a loop diamond to adjust its reach and height.");
                Number("Copies",() => P.handles,v => P.handles = Mathf.RoundToInt(v),0,6,true);
                Number("Reach",() => P.handleReach,v => P.handleReach = v,.12f,.85f);
                Number("Span",() => P.handleSpan,v => P.handleSpan = v,.14f,.75f);
                Number("Height",() => P.handleLevel,v => P.handleLevel = v,.05f,.96f);
                Number("Thickness",() => P.handleGauge,v => P.handleGauge = v,.025f,.14f);
                Number("Angle",() => P.handleAngle,v => P.handleAngle = v,0,359.9f);
            }
            else if (tab == 3)
            {
                Section("Material mapping");
                var preview = new Toggle("Numbered checker") { value = checker };
                controls.Add(preview);
                preview.RegisterValueChangedCallback(e => { checker = e.newValue; UpdateView(); });
                Number("Tile size (cm)",() => P.tileSize*100,v => P.tileSize = v/100,.5f,200);
                MakeButton(controls,"Refit to current shape",() => Change(P.RefitUVs));
                Hint("Refit to restore the material scale after stretching the clay.");
                if (PaintableExportRequested != null)
                {
                    Section("Paintable export");
                    var combined = new Toggle("Single texture set") { value = oneTextureSet };
                    controls.Add(combined);
                    combined.RegisterValueChangedCallback(e => oneTextureSet = e.newValue);
                    MakeButton(controls,"Export Painter OBJ",() => PaintableExportRequested(Vessel,oneTextureSet,true));
                    MakeButton(controls,"Save paintable mesh",() => PaintableExportRequested(Vessel,oneTextureSet,false));
                }
            }
            else
            {
                Section("Your pots");
                var name = new TextField { value = P.title, maxLength = 48, isDelayed = true };
                controls.Add(name);
                name.RegisterValueChangedCallback(e => Change(() => P.title = e.newValue));
                if (shelf.pots.Count == 0) Hint("Keep a copy to return to this shape later.");
                for (int i = shelf.pots.Count-1; i >= 0; i--)
                {
                    var saved = shelf.pots[i];
                    MakeButton(controls,saved.title+"   ·   "+(i+1),() => { Replace(saved); ShowPanel(); });
                }
            }
            Refresh();
        }

        void Section(string name) => Text(controls,name,"section");
        void Hint(string hint) => Text(controls,hint,"hint");

        void Choices(string[] names,int current,Action<int> apply)
        {
            var row = Box(controls,"row");
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                MakeButton(row,names[i],() => Change(() => apply(index),true)).EnableInClassList("chosen",i == current);
            }
        }

        void Metric(string name,Func<float> read,Action<float> write,float min,float max)
            => Number(name+" (mm)",() => read()*1000,v => write(v/1000),min*1000,max*1000,true);

        void Number(string name,Func<float> read,Action<float> write,float min,float max,bool whole = false)
        {
            var row = Box(controls,"value-row");
            Text(row,name,"parameter");
            var field = new FloatField { isDelayed = true, formatString = whole ? "0" : "0.##" };
            row.Add(field);
            var slider = new Slider(min,max) { name = name };
            controls.Add(slider);
            bool draggingSlider = false;
            void FinishSlider()
            {
                draggingSlider = false;
                EndGesture();
                Refresh();
            }
            slider.RegisterCallback<PointerDownEvent>(_ => { draggingSlider = true; BeginGesture(); },TrickleDown.TrickleDown);
            slider.RegisterCallback<PointerUpEvent>(_ => FinishSlider(),TrickleDown.TrickleDown);
            slider.RegisterCallback<PointerCaptureOutEvent>(_ => FinishSlider(),TrickleDown.TrickleDown);
            void Apply(float value,bool continuous) => Change(() => write(Mathf.Clamp(whole ? Mathf.Round(value) : value,min,max)),continuous:continuous);
            slider.RegisterValueChangedCallback(e => Apply(e.newValue,draggingSlider));
            field.RegisterValueChangedCallback(e => Apply(e.newValue,false));
            refreshFields.Add(() => { slider.SetValueWithoutNotify(read()); field.SetValueWithoutNotify(read()); });
        }
        void MakeThumbnails()
        {
            const int width = 96, height = 68;
            thumbnails = new Texture2D[Forms.Length];
            for (int i = 0; i < Forms.Length; i++)
            {
                var recipe = PotRecipe.Create(Forms[i]);
                float scale = Mathf.Min((width-12)/recipe.width,(height-10)/recipe.height);
                var pixels = new Color32[width*height];
                for (int y = 4; y < height-2; y++)
                {
                    float level = (y-4)/(scale*recipe.height);
                    if (level > 1) continue;
                    float radius = PotMesh.Surface(recipe,level,0).x*scale;
                    for (int x = 0; x < width; x++)
                    {
                        float u = (x+.5f-width*.5f)/radius;
                        if (Mathf.Abs(u) > 1) continue;
                        float light = Mathf.Clamp01(.63f-.24f*u+.15f*Mathf.Sqrt(1-u*u));
                        pixels[y*width+x] = new Color(.95f*light,.68f*light,.48f*light,1);
                    }
                }
                var image = new Texture2D(width,height,TextureFormat.RGBA32,false);
                image.SetPixels32(pixels); image.Apply(false,true);
                thumbnails[i] = image;
            }
        }
    }
}
