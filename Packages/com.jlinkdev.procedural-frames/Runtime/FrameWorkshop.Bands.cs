using UnityEngine;
using UnityEngine.UIElements;

namespace JLink.Frames
{
    public sealed partial class FrameWorkshop
    {
        readonly FrameBandTool bandTool = new FrameBandTool();
        static readonly Color HeightColor = new Color(.38f,.84f,1);

        void AddBandControls(VisualElement side)
        {
            var panel = new VisualElement();
            side.Add(panel);
            Section(panel,"Profile loops");
            var actions = Row(panel);
            var ridge = Button(actions,"+ Ridge",() => ArmBand(1));
            var groove = Button(actions,"+ Groove",() => ArmBand(-1));
            var hint = AddText(panel,"","help");
            var details = new VisualElement();
            panel.Add(details);
            var nav = Row(details);
            Button(nav,"Previous",() => Cycle(-1));
            Button(nav,"Next",() => Cycle(1));
            Button(nav,"Remove",() => Edit(() => {
                R.bands.RemoveAt(bandTool.Selected);
                bandTool.Select(Mathf.Min(bandTool.Selected,R.bands.Count-1));
            }));
            FrameBand Band() => bandTool.Current(R);
            var follow=Slider(details,"Follow edge (%)",0,100,()=>(Band()?.edgeFollow ?? 0)*100,
                v=>{if (Band()!=null) Band().edgeFollow=v*.01f;});
            sync.Add(()=>follow.style.display=FrameEdgeTool.GripCount(R)>0 ? DisplayStyle.Flex : DisplayStyle.None);
            Section(details,"Repeating detail");
            Choices(details,new[] { "Plain","Beads","Leaf scroll","Rope twist","Fluting","Egg-and-dart" },i => Band().ornament=(BandOrnament)i,
                () => (int)(Band()?.ornament ?? BandOrnament.None));
            var repeat = new VisualElement();
            details.Add(repeat);
            Slider(repeat,"Size (mm)",1,200,() => (Band()?.detailSize ?? .012f)*1000,
                v => { if (Band()!=null) Band().detailSize=v*.001f; });
            Slider(repeat,"Gap (mm)",0,250,() => (Band()?.detailGap ?? 0)*1000,
                v => { if (Band()!=null) Band().detailGap=v*.001f; });
            var fitNote = AddText(repeat,"","help");
            var mirror = new Toggle("Mirror opposite rails");
            repeat.Add(mirror);
            mirror.RegisterValueChangedCallback(e => Edit(() => Band().mirrorOppositeRails=e.newValue));
            sync.Add(() => {
                var b=Band();
                repeat.style.display = b != null && b.ornament != BandOrnament.None ? DisplayStyle.Flex : DisplayStyle.None;
                float fitted = b?.FittedSize(R) ?? 0;
                bool constrained = b != null && fitted < b.detailSize-.00001f;
                fitNote.style.display = constrained ? DisplayStyle.Flex : DisplayStyle.None;
                if (constrained) fitNote.text = $"Fits {fitted*1000:0.#} mm at this loop position.";
                mirror.style.display = b?.ornament == BandOrnament.LeafScroll && R.outline == FrameOutline.Rectangle
                    ? DisplayStyle.Flex : DisplayStyle.None;
                mirror.SetValueWithoutNotify(b?.mirrorOppositeRails ?? false);
            });
            Section(details,"Cross section");
            Choices(details,new[] { "Rounded","Sharp","Bevelled","Stepped" },i => Band().shape = (BandShape)i,() => (int)(Band()?.shape ?? BandShape.Rounded));
            Section(details,"Anchor");
            Choices(details,new[] { "Relative","Inner edge","Outer edge" },i => Band().SetAnchor(R.railWidth,(BandAnchor)i),() => (int)(Band()?.anchor ?? BandAnchor.Relative));
            Slider(details,"Centre (%)",0,100,() => (Band()?.Center(R.railWidth) ?? 0)*100/R.railWidth,
                v => Band()?.MoveTo(R.railWidth,v*.01f*R.railWidth));
            Slider(details,"Width (%)",0,100,() => (Band()?.Width(R.railWidth) ?? 0)*100/R.railWidth,
                v => Band()?.SetWidth(R.railWidth,v*.01f*R.railWidth));
            Slider(details,"Height (mm)",-200,200,() => (Band()?.height ?? 0)*1000,v => { if (Band()!=null) Band().height = v*.001f; });
            sync.Add(() => {
                panel.style.display = mode==FrameEditMode.Loops ? DisplayStyle.Flex : DisplayStyle.None;
                var band = Band();
                details.style.display = band != null && bandTool.Adding == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                ridge.EnableInClassList("selected",bandTool.Adding > 0);
                groove.EnableInClassList("selected",bandTool.Adding < 0);
                hint.text = bandTool.Adding != 0 ? "Point at a rail to preview. Press to place, then drag up or down to set relief." :
                    band == null ? "Choose a ridge or groove and click the rail to place its loop." :
                    $"Loop {bandTool.Selected+1} of {R.bands.Count}   ·   {(band.height < 0 ? "Groove" : "Ridge")}";
            });
        }

        void ArmBand(int sign) { bandTool.Arm(bandTool.Adding == sign ? 0 : sign); Refresh(); }
        void Cycle(int step)
        {
            if (R.bands.Count == 0) return;
            bandTool.Select((bandTool.Selected+R.bands.Count+step)%R.bands.Count);
            Refresh();
        }
        Ray BandRay(Vector2 p) => view.ViewportPointToRay(new Vector3(p.x/viewport.contentRect.width,1-p.y/viewport.contentRect.height));

        void DrawBands(Painter2D pen)
        {
            void Line(float t,Color color,float weight)
            {
                var points = bandTool.Loop(Frame,t);
                pen.strokeColor = color; pen.lineWidth = weight; pen.BeginPath();
                pen.MoveTo(Project(points[0]));
                for (int i = 1; i < points.Length; i++) pen.LineTo(Project(points[i]));
                pen.Stroke();
            }
            for (int i = 0; i < R.bands.Count; i++)
            {
                var b = R.bands[i];
                bool selected = i == bandTool.Selected && bandTool.Adding == 0;
                Line(b.Center(R.railWidth)/R.railWidth,selected ? Accent : new Color(1,1,1,.40f),selected ? 2 : 1);
            }
            if (bandTool.Preview >= 0) Line(bandTool.Preview,HeightColor,2);
            var band = bandTool.Current(R);
            if (band == null || bandTool.Adding != 0) return;
            for (var grip = FrameBandTool.Grip.InnerWidth; grip <= FrameBandTool.LastGrip(band); grip++)
            {
                var p = Project(bandTool.HandleFor(Frame,band,grip));
                Dot(pen,p,8,new Color(.09f,.13f,.14f));
                Dot(pen,p,5,FrameBandTool.GripColor(grip));
            }
        }
    }
}
