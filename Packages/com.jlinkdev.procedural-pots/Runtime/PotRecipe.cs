using System;
using System.Collections.Generic;
using UnityEngine;

namespace JLink.Pots
{
    public enum PotForm { Nursery, Bowl, Cylinder, Urn, Trough, Bonsai }
    public enum Footprint { Ellipse, SoftSquare, Faceted }
    public enum Lip { Plain, Rolled, Flared }
    public enum BaseStyle { Flat, FootRing }
    public enum PotUvMode { Tiled, Paintable }

    [Serializable]
    public struct ThumbMark
    {
        public float level, degrees, size, depth;

        public float Influence(float atLevel, float atDegrees)
        {
            float dy = (atLevel-level) / size;
            float dx = Mathf.DeltaAngle(degrees, atDegrees) / (size * 220);
            float weight = Mathf.Max(0, 1 - dx*dx - dy*dy);
            return weight * weight;
        }
    }

    [Serializable]
    public struct ProfileRing
    {
        [Range(0, 1)] public float level;
        [Range(.12f, 2f)] public float radius;

        public ProfileRing(float level, float radius)
        {
            this.level = level;
            this.radius = radius;
        }
    }

    [Serializable]
    public sealed class PotRecipe
    {
        public string title = "Nursery";
        public float height = .32f;
        public float width = .34f;
        public float depthRatio = 1f;
        public float wall = .012f;
        public float floor = .018f;
        public Footprint footprint;
        public Lip lip = Lip.Rolled;
        public BaseStyle baseStyle;
        public int facets = 6;
        public float rimSize = .06f;
        public bool drainage = true;
        public bool saucer;
        public int flutes = 16;
        public float fluteDepth;
        public float twist;
        public int handles;
        public float handleAngle;
        public float handleReach = .34f;
        public float handleLevel = .66f;
        public float handleSpan = .32f;
        public float handleGauge = .065f;
        public int segments = 96;
        [Tooltip("Size of one texture tile in metres.")] public float tileSize = .1f;
        [SerializeField, HideInInspector] string uvReferenceJson;
        internal string UvReferenceJson => uvReferenceJson;

        // Keep the rest shape independent of both the editable recipe and its own snapshot.
        public void RefitUVs()
        {
            var rest = (PotRecipe)MemberwiseClone();
            rest.uvReferenceJson = null;
            uvReferenceJson = JsonUtility.ToJson(rest);
        }

        internal void EnsureUvReference()
        {
            if (string.IsNullOrEmpty(uvReferenceJson)) RefitUVs();
        }
        // Sampled along normalized height, in the same radius units as the editable rings.
        [HideInInspector] public float[] wheelOffsets = Array.Empty<float>();
        public List<ThumbMark> marks = new List<ThumbMark>();
        public List<ProfileRing> rings = new List<ProfileRing>
        {
            new ProfileRing(0, .65f), new ProfileRing(.48f, .83f), new ProfileRing(1, 1)
        };

        public PotRecipe Copy() => JsonUtility.FromJson<PotRecipe>(JsonUtility.ToJson(this));

        public static PotRecipe Create(PotForm form)
        {
            var p = new PotRecipe { title = form.ToString() };
            switch (form)
            {
                case PotForm.Bowl:
                    p.height = .16f; p.width = .4f;
                    p.SetProfile(.4f, .25f, .69f, .65f, .91f, 1);
                    break;
                case PotForm.Cylinder:
                    p.height = .35f; p.lip = Lip.Plain;
                    p.SetProfile(.96f, .45f, 1, .82f, 1, 1);
                    break;
                case PotForm.Urn:
                    p.height = .44f; p.lip = Lip.Flared; p.baseStyle = BaseStyle.FootRing;
                    p.SetProfile(.45f, .42f, 1, .83f, .49f, .53f);
                    p.handles = 2;
                    break;
                case PotForm.Trough:
                    p.height = .22f; p.width = .62f; p.depthRatio = .48f;
                    p.footprint = Footprint.SoftSquare;
                    p.SetProfile(.81f, .32f, .85f, .8f, .98f, 1);
                    break;
                case PotForm.Bonsai:
                    p.height = .1f; p.width = .4f; p.depthRatio = .72f;
                    p.baseStyle = BaseStyle.FootRing; p.lip = Lip.Plain;
                    p.footprint = Footprint.Faceted; p.facets = 8;
                    p.SetProfile(.7f, .22f, .85f, .64f, .94f, 1);
                    break;
            }
            return p;
        }

        void SetProfile(float bottom, float a, float ar, float b, float br, float top)
        {
            rings = new List<ProfileRing> { new ProfileRing(0, bottom), new ProfileRing(a, ar),
                new ProfileRing(b, br), new ProfileRing(1, top) };
        }

        internal static float Limit(float value, float lo, float hi, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, lo, hi);

        public void Normalize()
        {
            width = Limit(width, .06f, 2, .34f);
            tileSize = tileSize <= 0 ? .1f : Limit(tileSize,.005f,2,.1f);
            height = Limit(height, .04f, 2, .32f);
            depthRatio = Limit(depthRatio, .25f, 1.8f, 1);
            floor = Limit(floor, .001f, height * .35f, .004f);
            wall = Limit(wall, .001f, width * .18f, .004f);
            segments = Mathf.Clamp(segments, 24, 192);
            facets = Mathf.Clamp(facets, 3, 16);
            rimSize = Limit(rimSize, .01f, .2f, .06f);
            flutes = Mathf.Clamp(flutes, 3, 32);
            fluteDepth = Limit(fluteDepth, 0, .18f, 0);
            twist = Limit(twist, -1, 1, 0);
            handles = Mathf.Clamp(handles, 0, 6);
            handleReach = Limit(handleReach, .12f, .85f, .34f);
            handleSpan = Limit(handleSpan, .14f, .75f, .32f);
            handleLevel = Limit(handleLevel, .05f + handleSpan / 2, .96f - handleSpan / 2, .5f);
            handleGauge = Limit(handleGauge, .025f, .14f, .065f);
            handleAngle = Mathf.Repeat(Limit(handleAngle, -3600, 3600, 0), 360);
            if (wheelOffsets == null) wheelOffsets = Array.Empty<float>();
            for (int i = 0; i < wheelOffsets.Length; i++)
                wheelOffsets[i] = Limit(wheelOffsets[i], -1.88f, 1.88f, 0);
            if (marks == null) marks = new List<ThumbMark>();
            for (int i = 0; i < marks.Count; i++)
            {
                var mark = marks[i];
                mark.level = Limit(mark.level, 0, 1, .5f);
                mark.degrees = Mathf.Repeat(Limit(mark.degrees, -3600, 3600, 0), 360);
                mark.size = Limit(mark.size, .08f, .42f, .2f);
                mark.depth = Limit(mark.depth, -.4f, .5f, 0);
                marks[i] = mark;
            }
            if (rings == null || rings.Count < 2) rings = new PotRecipe().rings;
            for (int i = 0; i < rings.Count; i++)
            {
                var r = rings[i];
                rings[i] = new ProfileRing(Limit(r.level, 0, 1, 0), Limit(r.radius, .12f, 2, 1));
            }
            rings.Sort((a, b) => a.level.CompareTo(b.level));
            rings[0] = new ProfileRing(0, rings[0].radius);
            int last = rings.Count - 1;
            rings[last] = new ProfileRing(1, rings[last].radius);
            for (int i = last - 1; i > 0; i--)
                if (rings[i].level - rings[i-1].level < .015f || rings[i+1].level - rings[i].level < .015f)
                    rings.RemoveAt(i);
        }

        // Monotone cubic Hermite interpolation flattens at a belly or neck without overshoot.
        float Slope(int i)
        {
            int a = Mathf.Max(0, i - 1), b = Mathf.Min(rings.Count - 1, i + 1);
            if (a == i || b == i) return Secant(a, b);
            float left = Secant(a, i), right = Secant(i, b);
            return left * right <= 0 ? 0 : 2 * left * right / (left + right);
        }

        float Secant(int a, int b) => (rings[b].radius - rings[a].radius) / (rings[b].level - rings[a].level);

        public float RadiusAt(float level)
        {
            float radius = BaseRadiusAt(level);
            if (wheelOffsets == null || wheelOffsets.Length == 0) return radius;
            float sample = Mathf.Clamp01(level) * (wheelOffsets.Length - 1);
            int i = Mathf.FloorToInt(sample);
            float offset = Mathf.Lerp(wheelOffsets[i],wheelOffsets[Mathf.Min(i+1,wheelOffsets.Length-1)],sample-i);
            return Mathf.Clamp(radius + offset,.12f,2);
        }

        internal float BaseRadiusAt(float level)
        {
            level = Mathf.Clamp01(level);
            int i = 0;
            while (i < rings.Count - 2 && level > rings[i+1].level) i++;
            var a = rings[i]; var b = rings[i+1];
            float span = b.level - a.level, t = (level - a.level) / span;
            float s = 1 - t;
            return s*s*(1+2*t)*a.radius + t*t*(3-2*t)*b.radius
                + span * t*s*(s*Slope(i) - t*Slope(i+1));
        }
    }
}
