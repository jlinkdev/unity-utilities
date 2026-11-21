using System;
using UnityEngine;

namespace JLink.Frames
{
    /// <summary>Shared selection and pointer gestures. Project returns viewport coordinates in pixels with Y down.</summary>
    public sealed class FrameBandTool
    {
        public enum Grip { None, Move, InnerWidth, OuterWidth, Height, DetailSize, DetailGap }
        public static Grip LastGrip(FrameBand b) => b.ornament == BandOrnament.None ? Grip.Height : Grip.DetailGap;
        public static Color GripColor(Grip grip)
        {
            switch (grip)
            {
                case Grip.Height: return new Color(.38f,.84f,1);
                case Grip.DetailSize: return new Color(.53f,.94f,.65f);
                case Grip.DetailGap: return new Color(1,.55f,.53f);
                default: return FrameWorkshop.Accent;
            }
        }
        public int Selected { get; private set; } = -1;
        public int Adding { get; private set; }
        public float Preview { get; private set; } = -1;
        public Grip Dragging { get; private set; }

        Vector2 press, across;
        float original, pixelsPerMetre;

        public FrameBand Current(FrameRecipe r) => Selected >= 0 && Selected < r.bands.Count ? r.bands[Selected] : null;
        public void Select(int index) { Selected = index; Arm(0); }
        public void Arm(int sign) { Adding = Math.Sign(sign); Preview = -1; Release(); }
        public void Leave() => Preview = -1;
        public void Release() => Dragging = Grip.None;

        public void Hover(ProceduralFrame f,Ray ray)
        {
            Preview = -1;
            if (Adding != 0 && f.TryPick(ray,out float t,out _,out _))
                Preview = Mathf.Clamp(t,.12f,.88f);
        }

        // Points in local frame space; the last point closes the loop.
        public Vector3[] Loop(ProceduralFrame f,float t)
        {
            var contour = f.Contour;
            var r = f.recipe;
            float offset = r.Border+t*r.railWidth;
            float z = -r.Height(t);
            int count = 1;
            foreach (var run in contour.rails) count += run.Count-1;
            var points = new Vector3[count];
            int k = 0;
            foreach (var run in contour.rails)
                for (int i = 0; i < run.Count-1; i++)
                {
                    Vector2 p = run[i].At(offset);
                    points[k++] = new Vector3(p.x,p.y,z);
                }
            points[k] = points[0];
            return points;
        }

        public Vector3 HandleFor(ProceduralFrame frame,FrameBand band,Grip grip)
        {
            return Handle(frame.recipe,band,grip,frame.Contour);
        }
        public static Vector3 Handle(FrameRecipe r,FrameBand b,Grip grip) => Handle(r,b,grip,null);

        static Vector3 Handle(FrameRecipe r,FrameBand b,Grip grip,FrameContour cached)
        {
            if (grip == Grip.DetailSize || grip == Grip.DetailGap) return DetailHandle(r,b,grip,out _,cached);
            float x = b.Center(r.railWidth);
            Vector2 dir = Vector2.up;
            if (grip == Grip.InnerWidth) { x -= .5f*b.Width(r.railWidth); dir = Vector2.left; }
            if (grip == Grip.OuterWidth) { x += .5f*b.Width(r.railWidth); dir = Vector2.right; }
            Vector2 xy = Vector2.Scale(dir,r.artworkSize*.5f)+dir*(r.Border+x);
            if (r.edges!=null && r.edges.Active(r)) xy=(cached??new FrameContour(r)).Deform(xy);
            return new Vector3(xy.x,xy.y,-r.Height(x/r.railWidth));
        }

        // Locate the two detail grips on opposite sides of the lower rail, even on an oval.
        static Vector3 DetailHandle(FrameRecipe r,FrameBand band,Grip grip,out Vector2 tangent,FrameContour cached)
        {
            var run = (cached??new FrameContour(r)).rails[0];
            float offset = r.Border+band.Center(r.railWidth);
            float length = 0;
            for (int i=1;i<run.Count;i++) length += Vector2.Distance(run[i-1].At(offset),run[i].At(offset));
            float remaining = length*(r.outline == FrameOutline.Oval ? .40f : .75f);
            Vector2 point = default;
            tangent = Vector2.right;
            for (int i=1;i<run.Count;i++)
            {
                Vector2 a = run[i-1].At(offset), edge = run[i].At(offset)-a;
                float segment = edge.magnitude;
                if (remaining <= segment || i == run.Count-1)
                {
                    tangent = edge/segment;
                    point = a+tangent*remaining;
                    break;
                }
                remaining -= segment;
            }
            if (grip == Grip.DetailSize) { point.x = -point.x; tangent.y = -tangent.y; }
            return new Vector3(point.x,point.y,-r.Height(band.Center(r.railWidth)/r.railWidth));
        }

        public bool Press(ProceduralFrame f,Ray ray,Vector2 mouse,Func<Vector3,Vector2> project)
        {
            var r = f.recipe;
            var b = Current(r);
            press = mouse;
            Vector3 basisPoint = Vector3.zero;
            Vector2 outward = Vector2.zero;
            Release();
            if (Adding == 0 && b != null)
            {
                for (Grip grip = Grip.InnerWidth; grip <= LastGrip(b); grip++)
                {
                    var local = HandleFor(f,b,grip);
                    if (Vector2.Distance(project(local),mouse) > 10) continue;
                    Dragging = grip;
                    basisPoint = local;
                    outward = grip == Grip.InnerWidth ? Vector2.left : Vector2.right;
                    if (grip == Grip.DetailSize || grip == Grip.DetailGap) DetailHandle(r,b,grip,out outward,f.Contour);
                    break;
                }
            }
            if (Dragging == Grip.None)
            {
                if (!f.TryPick(ray,out float t,out basisPoint,out outward)) return false;
                float center = t*r.railWidth;
                if (Adding != 0)
                {
                    b = new FrameBand { width = r.railWidth*.24f, height = Adding*r.railWidth*.10f };
                    b.MoveTo(r.railWidth,center);
                    r.bands.Add(b);
                    Select(r.bands.Count-1);
                    Dragging = Grip.Height;
                }
                else
                {
                    int hit = -1;
                    float closest = float.PositiveInfinity;
                    for (int i = 0; i < r.bands.Count; i++)
                    {
                        var candidate = r.bands[i];
                        float d = Mathf.Abs(center-candidate.Center(r.railWidth));
                        float width = candidate.Width(r.railWidth);
                        if (candidate.ornament != BandOrnament.None) width = Mathf.Max(width,candidate.FittedSize(r));
                        if (d <= width*.53f && d < closest) { hit = i; closest = d; }
                    }
                    if (hit < 0) return false;
                    Select(hit);
                    b = Current(r);
                    Dragging = Grip.Move;
                }
            }
            // Use a short local probe so perspective and transformed frame objects behave like the orthographic bench.
            across = (project(basisPoint+(Vector3)outward*.01f)-project(basisPoint))*100;
            float scaleX = (project(basisPoint+Vector3.right*.01f)-project(basisPoint)).magnitude*100;
            float scaleY = (project(basisPoint+Vector3.up*.01f)-project(basisPoint)).magnitude*100;
            pixelsPerMetre = Mathf.Max(1,Mathf.Max(scaleX,scaleY));
            original = Dragging == Grip.Move ? b.Center(r.railWidth) : Dragging == Grip.Height ? b.height : b.Width(r.railWidth);
            if (Dragging == Grip.DetailSize) original = b.FittedSize(r);
            if (Dragging == Grip.DetailGap) original = b.detailGap;
            return true;
        }

        public void Drag(FrameRecipe r,Vector2 mouse)
        {
            var b = Current(r);
            if (b == null || Dragging == Grip.None) return;
            Vector2 delta = mouse-press;
            float distance = Vector2.Dot(delta,across)/Mathf.Max(1,across.sqrMagnitude);
            switch (Dragging)
            {
                case Grip.Move: b.MoveTo(r.railWidth,original+distance); break;
                case Grip.InnerWidth: b.SetWidth(r.railWidth,original-2*distance); break;
                case Grip.OuterWidth: b.SetWidth(r.railWidth,original+2*distance); break;
                case Grip.Height: b.height = Mathf.Clamp(original-delta.y/pixelsPerMetre,-.2f,.2f); break;
                case Grip.DetailSize: b.detailSize = Mathf.Clamp(original+distance,.001f,.2f); break;
                case Grip.DetailGap: b.detailGap = Mathf.Clamp(original+distance,0,.25f); break;
            }
        }
    }
}
