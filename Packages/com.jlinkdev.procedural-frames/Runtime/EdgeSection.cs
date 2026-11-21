using System;
using System.Collections.Generic;
using UnityEngine;

namespace JLink.Frames
{
    // A cross section carries the fixed lip, each loop's two shoulders and crest,
    // and the outside wall. Ordered interpolation keeps the skin from folding.
    internal sealed class EdgeSection
    {
        readonly float[] original, mapped;
        internal sealed class Layout
        {
            internal readonly float[] offsets, follow;
            public Layout(FrameRecipe r)
            {
                var anchors=new SortedDictionary<float,float>();
                float lip=r.Border, outer=r.Border+r.railWidth;
                void Place(float offset,float weight)
                {
                    if (offset>lip+.000001f && offset<outer-.000001f) anchors[offset]=weight;
                }
                foreach (var band in r.bands)
                {
                    float center=r.Border+band.Center(r.railWidth), half=band.Width(r.railWidth)*.5f;
                    Place(center-half,band.edgeFollow); Place(center+half,band.edgeFollow);
                }
                foreach (var band in r.bands) Place(r.Border+band.Center(r.railWidth),band.edgeFollow);
                anchors[lip]=0; anchors[outer]=1;
                offsets=new float[anchors.Count]; follow=new float[anchors.Count];
                anchors.Keys.CopyTo(offsets,0); anchors.Values.CopyTo(follow,0);
            }
        }
        public EdgeSection(Layout layout,float delta)
        {
            original=layout.offsets; mapped=new float[original.Length];
            int last=original.Length-1;
            mapped[0]=original[0]; mapped[last]=original[last]+delta;
            for (int i=1;i<last;i++)
            {
                float low=mapped[i-1]+.035f*(original[i]-original[i-1]);
                float high=mapped[last]-.035f*(original[last]-original[i]);
                mapped[i]=Mathf.Clamp(original[i]+layout.follow[i]*delta,low,high);
            }
        }
        public float Transform(float offset) => Interpolate(offset,original,mapped);
        static float Interpolate(float x,float[] from,float[] to)
        {
            if (x<=from[0]) return x;
            int last=from.Length-1;
            if (x>=from[last]) return to[last]+x-from[last];
            int j=Array.BinarySearch(from,x);
            if (j>=0) return to[j];
            j=~j;
            return to[j-1]+(x-from[j-1])*(to[j]-to[j-1])/(from[j]-from[j-1]);
        }
    }
}
