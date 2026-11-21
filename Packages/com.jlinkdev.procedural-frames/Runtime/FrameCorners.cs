using System;
using UnityEngine;

namespace JLink.Frames
{
    public enum CornerStyle { None, LeafFan, TwinScroll, ScallopShell }
    public enum CornerAdjustment { Position, Size, Relief }

    [Serializable]
    public sealed class FrameCorners
    {
        public CornerStyle style;
        [Range(0,1)] public float position = .55f;
        [Min(.001f)] public float size = .038f;
        [Min(.0001f)] public float relief = .005f;

        public bool Visible(FrameRecipe r) => style != CornerStyle.None && r.outline != FrameOutline.Oval;
        public float FittedSize(FrameRecipe r)
        {
            float fitted=Mathf.Min(size,r.railWidth);
            var motif=FrameOrnament.Corner(style);
            if (motif==null) return fitted;
            // Keep opposing wings apart even on a short picture with unusually wide rails.
            float shortSide=Mathf.Min(r.artworkSize.x,r.artworkSize.y);
            float span=shortSide+2*r.Border, reach=motif.mitreReach;
            if (r.outline==FrameOutline.Rounded)
            {
                float radius=shortSide*r.cornerFraction;
                float quadrant=2*r.curveSegments*Mathf.Sin(Mathf.PI*.25f/r.curveSegments);
                span=shortSide-2*radius+quadrant*(radius+r.Border);
                reach=motif.arcReach;
            }
            return Mathf.Min(fitted,.49f*span/reach);
        }
        internal float Centre(FrameRecipe r,float fitted) => r.Border+fitted*.5f+(r.railWidth-fitted)*position;

        internal void Sanitize()
        {
            position = float.IsFinite(position) ? Mathf.Clamp01(position) : .55f;
            size = Mathf.Clamp(float.IsFinite(size) && size>0 ? size : .038f,.001f,.40f);
            relief = Mathf.Clamp(float.IsFinite(relief) ? relief : .005f,.0001f,.10f);
        }

        // Signed distance along the rails from the bisector, and offset across the rail.
        internal Vector2 Point(FrameRecipe r,int corner,float along,float across,float centre,FrameContour contour=null)
        {
            Vector2 half = r.artworkSize*.5f;
            Vector2 p;
            if (r.outline == FrameOutline.Rectangle)
                p = half+new Vector2(across+Mathf.Min(0,along),across-Mathf.Max(0,along));
            else
            {
                float radius = Mathf.Min(r.artworkSize.x,r.artworkSize.y)*r.cornerFraction;
                float step = Mathf.PI*.5f/r.curveSegments;
                float segment = 2*(radius+centre)*Mathf.Sin(step*.5f);
                float index = r.curveSegments*.5f-along/segment;
                float bounded = Mathf.Clamp(index,0,r.curveSegments);
                int i = Mathf.Min(Mathf.FloorToInt(bounded),r.curveSegments-1);
                Vector2 Normal(int n) => new Vector2(Mathf.Cos(n*step),Mathf.Sin(n*step));
                var outward = Vector2.Lerp(Normal(i),Normal(i+1),bounded-i);
                p = half-Vector2.one*radius+outward*(radius+across);
                if (index<0) p.y += index*segment;
                if (index>r.curveSegments) p.x -= (index-r.curveSegments)*segment;
            }
            if ((corner & 1)!=0) p.x=-p.x;
            if ((corner & 2)!=0) p.y=-p.y;
            if (r.edges!=null && r.edges.Active(r)) return (contour??new FrameContour(r)).Deform(p);
            return p;
        }

        public Vector3 Handle(FrameRecipe r,int corner,CornerAdjustment adjustment) => Handle(r,corner,adjustment,null);
        public Vector3 Handle(ProceduralFrame frame,int corner,CornerAdjustment adjustment) =>
            Handle(frame.recipe,corner,adjustment,frame.Contour);

        internal Vector3 Handle(FrameRecipe r,int corner,CornerAdjustment adjustment,FrameContour contour)
        {
            float fitted = FittedSize(r), centre = Centre(r,fitted);
            float along = adjustment == CornerAdjustment.Size ? fitted*.80f : 0;
            Vector2 p = Point(r,corner,along,centre,centre,contour);
            return new Vector3(p.x,p.y,-r.Height((centre-r.Border)/r.railWidth)-relief*.5f);
        }
    }

    // Both views project the same local handles and edit the same four-corner recipe.
    public sealed class FrameCornerTool
    {
        public CornerAdjustment adjustment;
        public bool dragging { get; private set; }
        Vector2 press, axis;
        float original;

        public void Release() => dragging=false;
        public bool Press(FrameRecipe r,Vector2 mouse,Func<Vector3,Vector2> project) =>
            Press(r,mouse,project,r.edges!=null && r.edges.Active(r) ? new FrameContour(r) : null);

        public bool Press(ProceduralFrame frame,Vector2 mouse,Func<Vector3,Vector2> project) =>
            Press(frame.recipe,mouse,project,frame.Contour);

        bool Press(FrameRecipe r,Vector2 mouse,Func<Vector3,Vector2> project,FrameContour contour)
        {
            dragging=false;
            var c=r.corners;
            if (!c.Visible(r)) return false;
            int corner=-1;
            float nearest=14;
            for (int i=0;i<4;i++)
            {
                float d=Vector2.Distance(mouse,project(c.Handle(r,i,adjustment,contour)));
                if (d<nearest) { nearest=d; corner=i; }
            }
            if (corner<0) return false;
            var p=c.Handle(r,corner,adjustment,contour);
            Vector2 pixel=project(p);
            float fitted=c.FittedSize(r), centre=c.Centre(r,fitted);
            if (adjustment==CornerAdjustment.Relief)
            {
                float scale=Mathf.Max((project(p+Vector3.right*.01f)-pixel).magnitude,
                    (project(p+Vector3.up*.01f)-pixel).magnitude)*100;
                axis=Vector2.down*scale; original=c.relief;
            }
            else
            {
                Vector2 step=adjustment==CornerAdjustment.Size
                    ? c.Point(r,corner,.80f*(fitted+.001f),centre,centre,contour)
                    : c.Point(r,corner,0,centre+.001f,centre,contour);
                axis=(project(new Vector3(step.x,step.y,p.z))-pixel)*1000;
                if (adjustment==CornerAdjustment.Position) axis*=r.railWidth-fitted;
                original=adjustment==CornerAdjustment.Size ? fitted : c.position;
            }
            press=mouse; dragging=axis.sqrMagnitude>.0001f;
            return dragging;
        }
        public void Drag(FrameRecipe r,Vector2 mouse)
        {
            if (!dragging) return;
            float value=original+Vector2.Dot(mouse-press,axis)/axis.sqrMagnitude;
            switch (adjustment)
            {
                case CornerAdjustment.Position: r.corners.position=Mathf.Clamp01(value); break;
                case CornerAdjustment.Size: r.corners.size=Mathf.Clamp(value,.001f,.4f); break;
                case CornerAdjustment.Relief: r.corners.relief=Mathf.Clamp(value,.0001f,.10f); break;
            }
        }
    }
}
