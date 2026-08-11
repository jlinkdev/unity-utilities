using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    public static class BeamPathUtility
    {
        /// <summary>Evaluates a measured strand at normalized distance.</summary>
        public static BeamPoint Evaluate(BeamStrand strand, float normalizedDistance)
        {
            if (strand == null || strand.Count == 0)
                return default;
            if (strand.Count == 1 || normalizedDistance <= 0f)
                return strand[0];
            if (normalizedDistance >= 1f)
                return strand[strand.Count - 1];

            float targetDistance = strand.Length * normalizedDistance;
            for (int i = 0; i < strand.Count - 1; i++)
            {
                BeamPoint a = strand[i];
                BeamPoint b = strand[i + 1];
                if (b.Distance < targetDistance)
                    continue;
                float segmentLength = b.Distance - a.Distance;
                float t = segmentLength > 0.000001f ? (targetDistance - a.Distance) / segmentLength : 0f;
                BeamPoint result = new BeamPoint(Vector3.LerpUnclamped(a.Position, b.Position, t))
                {
                    Tangent = Vector3.SlerpUnclamped(a.Tangent, b.Tangent, t).normalized,
                    Normal = Vector3.SlerpUnclamped(a.Normal, b.Normal, t).normalized,
                    Distance = targetDistance,
                    NormalizedDistance = normalizedDistance
                };
                return result;
            }

            return strand[strand.Count - 1];
        }

        /// <summary>Recalculates cumulative distance, tangents, and a transported local frame.</summary>
        public static void RecalculateMetrics(BeamStrand strand, Vector3 referenceNormal)
        {
            if (strand == null || strand.Count == 0)
                return;

            float length = 0f;
            BeamPoint first = strand[0];
            first.Distance = 0f;
            strand[0] = first;

            for (int i = 1; i < strand.Count; i++)
            {
                BeamPoint point = strand[i];
                length += Vector3.Distance(strand[i - 1].Position, point.Position);
                point.Distance = length;
                strand[i] = point;
            }

            Vector3 previousTangent = Vector3.forward;
            Vector3 previousNormal = Vector3.up;
            for (int i = 0; i < strand.Count; i++)
            {
                BeamPoint point = strand[i];
                Vector3 tangent;
                if (strand.Count == 1)
                    tangent = previousTangent;
                else if (i == 0)
                    tangent = strand[1].Position - point.Position;
                else if (i == strand.Count - 1)
                    tangent = point.Position - strand[i - 1].Position;
                else
                    tangent = strand[i + 1].Position - strand[i - 1].Position;

                if (tangent.sqrMagnitude < 0.000001f)
                    tangent = previousTangent;
                tangent.Normalize();

                Vector3 normal;
                if (i == 0)
                {
                    normal = Vector3.ProjectOnPlane(referenceNormal, tangent);
                    if (normal.sqrMagnitude < 0.000001f)
                        normal = Vector3.ProjectOnPlane(Vector3.up, tangent);
                    if (normal.sqrMagnitude < 0.000001f)
                        normal = Vector3.ProjectOnPlane(Vector3.right, tangent);
                }
                else
                {
                    normal = Quaternion.FromToRotation(previousTangent, tangent) * previousNormal;
                    normal = Vector3.ProjectOnPlane(normal, tangent);
                }

                normal.Normalize();
                point.Tangent = tangent;
                point.Normal = normal;
                point.NormalizedDistance = length > 0.000001f ? point.Distance / length : 0f;
                strand[i] = point;
                previousTangent = tangent;
                previousNormal = normal;
            }
        }
    }
}
