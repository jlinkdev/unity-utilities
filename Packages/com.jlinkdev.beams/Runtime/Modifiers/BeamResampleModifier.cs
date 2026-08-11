using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    [AddComponentMenu("jlinkdev/Beams/Modifiers/Beam Resample Modifier")]
    public sealed class BeamResampleModifier : BeamPathModifier
    {
        [SerializeField, Min(0.01f)] private float maximumSegmentLength = 0.2f;
        [SerializeField, Range(2, 1024)] private int maximumPointCount = 256;

        private readonly List<Vector3> sourcePositions = new List<Vector3>(32);

        public float MaximumSegmentLength
        {
            get => maximumSegmentLength;
            set => maximumSegmentLength = Mathf.Max(0.01f, value);
        }

        public override void Modify(in BeamPathContext context, BeamPathBuffer paths)
        {
            for (int strandIndex = 0; strandIndex < paths.Count; strandIndex++)
                Resample(paths[strandIndex], context.Source.Forward);
        }

        private void Resample(BeamStrand strand, Vector3 referenceNormal)
        {
            if (strand.Count < 2)
                return;

            sourcePositions.Clear();
            float totalLength = 0f;
            for (int i = 0; i < strand.Count; i++)
            {
                Vector3 position = strand[i].Position;
                sourcePositions.Add(position);
                if (i > 0)
                    totalLength += Vector3.Distance(sourcePositions[i - 1], position);
            }

            int pointCount = Mathf.Clamp(
                Mathf.CeilToInt(totalLength / Mathf.Max(0.01f, maximumSegmentLength)) + 1,
                2,
                maximumPointCount);

            int parentStrandIndex = strand.ParentStrandIndex;
            float parentPosition = strand.ParentPosition;
            int strandSeed = strand.Seed;
            int branchDepth = strand.BranchDepth;
            strand.Clear();
            strand.ParentStrandIndex = parentStrandIndex;
            strand.ParentPosition = parentPosition;
            strand.Seed = strandSeed;
            strand.BranchDepth = branchDepth;
            int segmentIndex = 0;
            float segmentStartDistance = 0f;
            float segmentLength = Vector3.Distance(sourcePositions[0], sourcePositions[1]);

            for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                float targetDistance = pointIndex == pointCount - 1
                    ? totalLength
                    : totalLength * pointIndex / (pointCount - 1f);

                while (segmentIndex < sourcePositions.Count - 2 &&
                       targetDistance > segmentStartDistance + segmentLength)
                {
                    segmentStartDistance += segmentLength;
                    segmentIndex++;
                    segmentLength = Vector3.Distance(sourcePositions[segmentIndex], sourcePositions[segmentIndex + 1]);
                }

                float t = segmentLength > 0.000001f
                    ? Mathf.Clamp01((targetDistance - segmentStartDistance) / segmentLength)
                    : 0f;
                strand.Add(Vector3.LerpUnclamped(sourcePositions[segmentIndex], sourcePositions[segmentIndex + 1], t));
            }

            BeamPathUtility.RecalculateMetrics(strand, referenceNormal);
        }

        private void OnValidate()
        {
            maximumSegmentLength = Mathf.Max(0.01f, maximumSegmentLength);
            maximumPointCount = Mathf.Clamp(maximumPointCount, 2, 1024);
        }
    }
}
