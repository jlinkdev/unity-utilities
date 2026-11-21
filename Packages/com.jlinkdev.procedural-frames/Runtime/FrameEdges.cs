using System;
using UnityEngine;

namespace JLink.Frames
{
    public enum EdgeFeature { Corner, Waist, Centre, Top, Bottom, Sides }

    [Serializable]
    public sealed class FrameEdges
    {
        [Tooltip("Projection in metres. Positive values widen the outside edge.")]
        public float corner, waist, centre;
        [Range(.05f,.25f)] public float cornerSpan=.15f, waistSpan=.25f, centreSpan=.16f;
        public bool independentTop;
        public float topWaist, topCentre;

        public float ovalTop, ovalBottom, ovalSides;
        [Range(12,45)] public float ovalTopReach=42, ovalBottomReach=42, ovalSideReach=42;

        public bool Active(FrameRecipe r) => r.outline==FrameOutline.Oval
            ? ovalTop!=0 || ovalBottom!=0 || ovalSides!=0 : r.outline==FrameOutline.Rectangle &&
            (corner!=0 || waist!=0 || centre!=0 || independentTop && (topWaist!=0 || topCentre!=0));

        public float Amount(EdgeFeature feature,int side)
        {
            if (feature==EdgeFeature.Top) return ovalTop;
            if (feature==EdgeFeature.Bottom) return ovalBottom;
            if (feature==EdgeFeature.Sides) return ovalSides;
            if (feature==EdgeFeature.Corner) return corner;
            bool top=independentTop && side==2;
            return feature==EdgeFeature.Waist ? (top ? topWaist : waist) : (top ? topCentre : centre);
        }
        public void SetAmount(EdgeFeature feature,int side,float value)
        {
            if (feature==EdgeFeature.Top) ovalTop=value;
            else if (feature==EdgeFeature.Bottom) ovalBottom=value;
            else if (feature==EdgeFeature.Sides) ovalSides=value;
            else if (feature==EdgeFeature.Corner) corner=value;
            else if (independentTop && side==2)
            {
                if (feature==EdgeFeature.Waist) topWaist=value; else topCentre=value;
            }
            else if (feature==EdgeFeature.Waist) waist=value; else centre=value;
        }
        public void SeparateTop(bool separate)
        {
            if (separate && !independentTop) { topCentre=centre; topWaist=waist; }
            independentTop=separate;
        }
        // Rectangle spans are fractions of a rail. Oval spans are fractions of one revolution.
        public float Span(EdgeFeature feature) => feature==EdgeFeature.Top ? ovalTopReach/360 :
            feature==EdgeFeature.Bottom ? ovalBottomReach/360 : feature==EdgeFeature.Sides ? ovalSideReach/360 :
            feature==EdgeFeature.Corner ? cornerSpan :
            feature==EdgeFeature.Waist ? waistSpan : centreSpan;
        public void SetSpan(EdgeFeature feature,float value)
        {
            if (feature==EdgeFeature.Top) ovalTopReach=value*360;
            else if (feature==EdgeFeature.Bottom) ovalBottomReach=value*360;
            else if (feature==EdgeFeature.Sides) ovalSideReach=value*360;
            else if (feature==EdgeFeature.Corner) cornerSpan=value;
            else if (feature==EdgeFeature.Waist) waistSpan=value; else centreSpan=value;
        }
        internal void Sanitize()
        {
            float AmountLimit(float x) => float.IsFinite(x) ? Mathf.Clamp(x,-.4f,.4f) : 0;
            float Reach(float x,float fallback) => x>0 && float.IsFinite(x) ? Mathf.Clamp(x,.05f,.25f) : fallback;
            float Degrees(float x) => x>0 && float.IsFinite(x) ? Mathf.Clamp(x,12,45) : 42;
            ovalTop=AmountLimit(ovalTop); ovalBottom=AmountLimit(ovalBottom); ovalSides=AmountLimit(ovalSides);
            ovalTopReach=Degrees(ovalTopReach); ovalBottomReach=Degrees(ovalBottomReach); ovalSideReach=Degrees(ovalSideReach);
            corner=AmountLimit(corner); waist=AmountLimit(waist); centre=AmountLimit(centre);
            topWaist=AmountLimit(topWaist); topCentre=AmountLimit(topCentre);
            cornerSpan=Reach(cornerSpan,.15f); waistSpan=Reach(waistSpan,.25f); centreSpan=Reach(centreSpan,.16f);
        }
        static float Bump(float distance,float span) => distance>=span ? 0 : .5f+.5f*Mathf.Cos(Mathf.PI*distance/span);

        internal float Displacement(FrameRecipe r,int side,float u)
        {
            if (r.outline==FrameOutline.Oval)
            {
                int pole=u<=.5f ? side : (side+1)%4;
                var feature=OvalFeature(pole);
                float distance=Mathf.Min(u,1-u)*.25f/Span(feature);
                // A dominant crest and two smaller shoulders fade smoothly into the oval.
                float scallop=Bump(distance,.64f)+.38f*Bump(Mathf.Abs(distance-.75f),.25f);
                return Mathf.Max(-.72f*r.railWidth,Amount(feature,pole)*scallop);
            }
            float s=Mathf.Min(u,1-u);
            float d=corner*Bump(s,cornerSpan)+Amount(EdgeFeature.Waist,side)*Bump(Mathf.Abs(s-.25f),waistSpan)
                +Amount(EdgeFeature.Centre,side)*Bump(.5f-s,centreSpan);
            return Mathf.Max(-.72f*r.railWidth,d);
        }
        internal static EdgeFeature OvalFeature(int pole) => pole==0 ? EdgeFeature.Bottom : pole==2 ? EdgeFeature.Top : EdgeFeature.Sides;
    }
}
