using System;
using UnityEngine;

namespace JLink.Frames
{
    public sealed class FrameEdgeTool
    {
        public EdgeFeature feature=EdgeFeature.Centre;
        public int side=2;
        public bool Dragging { get; private set; }
        Vector2 press, projectedAxis;
        float start;

        public static int GripCount(FrameRecipe r) => r.outline==FrameOutline.Oval ? 4 : r.outline==FrameOutline.Rectangle ? 16 : 0;
        public static EdgeFeature Feature(FrameRecipe r,int grip) => r.outline==FrameOutline.Oval ? FrameEdges.OvalFeature(grip) : Feature(grip);
        public EdgeFeature Selected(FrameRecipe r) => r.outline==FrameOutline.Oval
            ? ((int)feature>=(int)EdgeFeature.Top ? feature : EdgeFeature.Top) : ((int)feature<(int)EdgeFeature.Top ? feature : EdgeFeature.Centre);
        public static Color ColorFor(EdgeFeature feature) => feature==EdgeFeature.Corner || feature==EdgeFeature.Top ? FrameWorkshop.Accent :
            feature==EdgeFeature.Waist || feature==EdgeFeature.Bottom ? new Color(.53f,.94f,.65f) : new Color(.38f,.84f,1);
        public string Selection(FrameEdges e) => feature==EdgeFeature.Corner ? "Four linked corners" :
            e.independentTop ? (side==2 ? "Top rail" : "Bottom and sides") : "All four rails";

        public static EdgeFeature Feature(int grip) => grip%4==0 ? EdgeFeature.Corner :
            grip%4==2 ? EdgeFeature.Centre : EdgeFeature.Waist;
        static void Station(FrameRecipe r,int grip,out int run,out float u)
        {
            run=r.outline==FrameOutline.Oval ? grip : grip/4;
            u=r.outline==FrameOutline.Oval ? 0 : grip%4*.25f;
        }
        public static Vector3 Handle(FrameRecipe r,int grip)
        {
            Station(r,grip,out int side,out float u);
            FrameContour.Basis(r,side,u,out var origin,out var axis);
            Vector2 p=origin+axis*(r.Border+r.railWidth+r.edges.Displacement(r,side,u));
            return new Vector3(p.x,p.y,-r.Height(1));
        }
        public void Release() => Dragging=false;
        public bool Press(FrameRecipe r,Vector2 mouse,Func<Vector3,Vector2> project)
        {
            Release();

            int hit=-1; float nearest=14;
            for (int i=0;i<GripCount(r);i++)
            {
                float d=Vector2.Distance(mouse,project(Handle(r,i)));
                if (d<nearest) { hit=i; nearest=d; }
            }
            if (hit<0) return false;
            feature=Feature(r,hit); Station(r,hit,out side,out float u);
            FrameContour.Basis(r,side,u,out _,out var axis);
            var p=Handle(r,hit);
            projectedAxis=(project(p+(Vector3)axis*.01f)-project(p))*100;
            start=r.edges.Displacement(r,side,u);
            press=mouse; Dragging=projectedAxis.sqrMagnitude>1;
            return Dragging;
        }
        public void Drag(FrameRecipe r,Vector2 mouse)
        {
            if (Dragging) r.edges.SetAmount(feature,side,Mathf.Clamp(start+Vector2.Dot(mouse-press,projectedAxis)/
                projectedAxis.sqrMagnitude,-.72f*r.railWidth,.4f));
        }
    }
}
