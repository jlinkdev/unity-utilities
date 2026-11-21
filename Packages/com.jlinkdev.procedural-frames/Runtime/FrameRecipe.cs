using System;
using System.Collections.Generic;
using UnityEngine;

namespace JLink.Frames
{
    public enum FrameOutline { Rectangle, Rounded, Oval }
    public enum FrameMount { Matted, ShadowBox, Floater }
    public enum Moulding { Flat, Bevel, Scoop, ReverseScoop, Ogee, Stepped }
    public enum FrameUV { Tiled, Paintable }
    public enum FrameSlot { Moulding, Mat, Artwork, EaselBack }

    [Serializable]
    public sealed class FrameRecipe
    {
        public FrameOutline outline;
        public FrameMount mount;
        [Tooltip("Visible artwork width and height in metres.")]
        public Vector2 artworkSize = new Vector2(.30f, .40f);
        public float railWidth = .047f;
        public float railDepth = .030f;
        public float relief = .021f;
        [Range(0, .45f)] public float cornerFraction = .17f;
        public float matWidth = .035f;
        public float floatGap = .008f;
        public bool includeArtwork = true;
        [Tooltip("Add a backing panel and hinged foot and place the frame on local Y = 0.")]
        public bool easelBack;
        [Range(5,25)] public float leanAngle = 12f;
        [Tooltip("A normalized cross section from the sight edge (0) to the outside (1).")]
        public AnimationCurve profile = Profile(Moulding.Ogee);
        public List<FrameBand> bands = new List<FrameBand>();
        public FrameEdges edges = new FrameEdges();
        public FrameCorners corners = new FrameCorners();
        [Range(4, 32)] public int curveSegments = 16;
        public FrameUV uvLayout;
        [Tooltip("Metres per texture repeat on the rails and mat.")]
        public float tileSize = .10f;

        public float Border => mount == FrameMount.Floater ? floatGap : matWidth;
        public float Seat => mount == FrameMount.ShadowBox ? railDepth * .76f : railDepth * .16f;
        public float Lip => Mathf.Min(.005f, railWidth * .23f);
        public float Height(float t)
        {
            float height = relief * Mathf.Clamp01(profile.Evaluate(t));
            if (bands != null)
                foreach (var band in bands)
                    if (band != null) height += band.Offset(railWidth,t*railWidth);
            // Keep the carved face in front of the shelf or the underside of the lip.
            float floor = mount == FrameMount.Floater ? railDepth*.80f : Seat*.92f;
            return Mathf.Max(-floor,height);
        }

        public static AnimationCurve Profile(Moulding style)
        {
            switch (style)
            {
                case Moulding.Flat: return AnimationCurve.Linear(0, .55f, 1, .55f);
                case Moulding.Bevel: return AnimationCurve.Linear(0, .12f, 1, .92f);
                case Moulding.Scoop: return Smooth(0, .16f, .35f, .12f, .70f, .37f, 1, .96f);
                case Moulding.ReverseScoop: return Smooth(0, .96f, .30f, .37f, .65f, .12f, 1, .16f);
                case Moulding.Stepped:
                    return Smooth(0, .18f, .23f, .18f, .26f, .49f, .70f, .49f, .73f, .92f, 1, .92f);
                default: return Smooth(0, .42f, .09f, .61f, .24f, .16f, .54f, .30f, .83f, .94f, 1, .74f);
            }
        }

        static AnimationCurve Smooth(params float[] points)
        {
            var keys = new Keyframe[points.Length / 2];
            for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(points[i*2], points[i*2+1]);
            return new AnimationCurve(keys);
        }

        public FrameRecipe Copy() => JsonUtility.FromJson<FrameRecipe>(JsonUtility.ToJson(this));

        public void Sanitize()
        {
            artworkSize = new Vector2(Limit(artworkSize.x,.05f,4), Limit(artworkSize.y,.05f,4));
            leanAngle = leanAngle>0 && float.IsFinite(leanAngle) ? Mathf.Clamp(leanAngle,5,25) : 12;
            railWidth = Limit(railWidth,.008f,.4f);
            railDepth = Limit(railDepth,.006f,.35f);
            relief = Limit(relief,0,.20f);
            matWidth = Limit(matWidth,0,.35f);
            floatGap = Limit(floatGap,.002f,.15f);
            cornerFraction = Limit(cornerFraction,.01f,.45f);
            tileSize = Limit(tileSize,.005f,2);
            curveSegments = Mathf.Clamp(curveSegments,4,32);
            if (profile == null || profile.length < 2) profile = Profile(Moulding.Flat);
            if (bands == null) bands = new List<FrameBand>();
            bands.RemoveAll(b => b == null);
            foreach (var band in bands) band.Sanitize();
            if (corners == null) corners = new FrameCorners();
            corners.Sanitize();
            if (edges==null) edges=new FrameEdges();
            edges.Sanitize();
        }

        static float Limit(float value, float min, float max) =>
            float.IsNaN(value) || float.IsInfinity(value) ? min : Mathf.Clamp(value, min, max);
    }
}
