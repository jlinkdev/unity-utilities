using System;
using UnityEngine;

namespace JLink.Frames
{
    public enum BandShape { Rounded, Sharp, Bevelled, Stepped }
    public enum BandAnchor { Relative, Inner, Outer }
    public enum BandOrnament { None, Beads, LeafScroll, RopeTwist, Fluting, EggAndDart }

    [Serializable]
    public sealed class FrameBand
    {
        public BandShape shape;
        public BandAnchor anchor;
        [Tooltip("Fraction of the rail for Relative, or metres from the anchored edge.")]
        public float position = .5f;
        [Min(.0005f)] public float width = .012f;
        [Tooltip("Signed relief in metres. Negative values carve grooves.")]
        public float height = .006f;
        [Tooltip("Fraction of the sculpted silhouette displacement followed by this loop.")]
        [Range(0,1)] public float edgeFollow;
        public BandOrnament ornament;
        [Tooltip("Width of one ornament across the rail, in metres.")]
        public float detailSize = .012f;
        [Tooltip("Requested clear space between successive ornaments, in metres.")]
        public float detailGap = .003f;
        public bool mirrorOppositeRails;

        public float FittedSize(FrameRecipe recipe)
        {
            float center = Center(recipe.railWidth);
            float size = Mathf.Min(detailSize,2*Mathf.Min(center,recipe.railWidth-center));
            if (recipe.outline != FrameOutline.Rectangle) return size;
            float shortRail = Mathf.Min(recipe.artworkSize.x,recipe.artworkSize.y)+2*(recipe.Border+center);
            float aspect = FrameOrnament.For(ornament)?.length ?? 1;
            return Mathf.Min(size,shortRail/(aspect+1));
        }

        public float Width(float rail) => Mathf.Clamp(width,.0005f,rail);

        public float Center(float rail)
        {
            float half = Width(rail)*.5f;
            float center = anchor == BandAnchor.Relative ? rail*position : anchor == BandAnchor.Inner ? position : rail-position;
            return Mathf.Clamp(center,half,rail-half);
        }

        public void MoveTo(float rail,float center)
        {
            float half = Width(rail)*.5f;
            center = Mathf.Clamp(center,half,rail-half);
            position = anchor == BandAnchor.Relative ? center/rail : anchor == BandAnchor.Inner ? center : rail-center;
        }

        public void SetWidth(float rail,float value)
        {
            float center = Center(rail);
            width = Mathf.Clamp(value,.0005f,2*Mathf.Min(center,rail-center));
            MoveTo(rail,center);
        }

        public void SetAnchor(float rail,BandAnchor value)
        {
            float center = Center(rail);
            anchor = value;
            MoveTo(rail,center);
        }

        internal float Offset(float rail,float x)
        {
            float d = Mathf.Abs((x-Center(rail))*2/Width(rail));
            if (d >= 1) return 0;
            float amount;
            switch (shape)
            {
                case BandShape.Sharp: amount = 1-d; break;
                case BandShape.Bevelled: amount = Mathf.Min(1,(1-d)*2); break;
                case BandShape.Stepped: amount = Mathf.Clamp01((1-d)*8)*.45f+Mathf.Clamp01((.5f-d)*8)*.55f; break;
                default: amount = .5f+.5f*Mathf.Cos(d*Mathf.PI); break;
            }
            return height*amount;
        }

        // Samples are spaced at eighths of the half-width. Preserve the facet breaks
        // when calculating normals for angular bands, including the joins to the rail.
        internal bool CreaseAt(int sample)
        {
            int i = Math.Abs(sample);
            if (height == 0) return false;
            switch (shape)
            {
                case BandShape.Sharp: return i == 0 || i == 8;
                case BandShape.Bevelled: return i == 4 || i == 8;
                case BandShape.Stepped: return i == 3 || i == 4 || i == 7 || i == 8;
                default: return false;
            }
        }

        internal void Sanitize()
        {
            edgeFollow=float.IsFinite(edgeFollow) ? Mathf.Clamp01(edgeFollow) : 0;
            if (!float.IsFinite(position)) position = 0;
            if (!float.IsFinite(width)) width = .012f;
            if (!float.IsFinite(height)) height = 0;
            width = Mathf.Clamp(width,.0005f,.4f);
            height = Mathf.Clamp(height,-.2f,.2f);
            if (!float.IsFinite(detailSize) || detailSize <= 0) detailSize = .012f;
            if (!float.IsFinite(detailGap)) detailGap = .003f;
            detailSize = Mathf.Clamp(detailSize,.001f,.20f);
            detailGap = Mathf.Clamp(detailGap,0,.25f);
        }
    }
}
