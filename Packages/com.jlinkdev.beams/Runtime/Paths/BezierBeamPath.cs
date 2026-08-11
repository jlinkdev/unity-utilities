using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Builds a cubic Bezier strand from endpoint positions and forward directions.</summary>
    [AddComponentMenu("jlinkdev/Beams/Paths/Bezier Beam Path")]
    public sealed class BezierBeamPath : BeamPathProvider
    {
        [SerializeField, Range(2, 256)] private int pointCount = 24;
        [SerializeField, Min(0f)] private float sourceHandleLength = 1f;
        [SerializeField, Min(0f)] private float targetHandleLength = 1f;
        [SerializeField] private bool scaleHandlesWithDistance;

        public override void BuildPath(in BeamPathContext context, BeamPathBuffer output)
        {
            Vector3 start = context.Source.Position;
            Vector3 end = context.Target.Position;
            float distanceScale = scaleHandlesWithDistance ? Vector3.Distance(start, end) : 1f;
            Vector3 startControl = start + context.Source.Forward * (sourceHandleLength * distanceScale);
            Vector3 endControl = end + context.Target.Forward * (targetHandleLength * distanceScale);

            BeamStrand strand = output.AddStrand(seed: context.Seed);
            int count = Mathf.Clamp(pointCount, 2, 256);
            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                strand.Add(CubicBezier(start, startControl, endControl, end, t));
            }
            BeamPathUtility.RecalculateMetrics(strand, context.Source.Forward);
        }

        private static Vector3 CubicBezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * inverse * a +
                   3f * inverse * inverse * t * b +
                   3f * inverse * t * t * c +
                   t * t * t * d;
        }

        private void OnValidate()
        {
            pointCount = Mathf.Clamp(pointCount, 2, 256);
            sourceHandleLength = Mathf.Max(0f, sourceHandleLength);
            targetHandleLength = Mathf.Max(0f, targetHandleLength);
        }
    }
}
