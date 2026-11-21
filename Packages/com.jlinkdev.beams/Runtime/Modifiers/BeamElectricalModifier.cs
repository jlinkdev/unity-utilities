using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    public enum BeamElectricalAnimationMode
    {
        Static = 0,
        Snap = 1,
        Morph = 2,
        HoldAndMorph = 3
    }

    /// <summary>Creates angular, seeded electrical silhouettes with stable endpoints and configurable restrike motion.</summary>
    [AddComponentMenu("jlinkdev/Beams/Modifiers/Beam Electrical Modifier")]
    public sealed class BeamElectricalModifier : BeamPathModifier
    {
        [SerializeField, Range(1, 8)] private int subdivisions = 5;
        [SerializeField, Min(0f)] private float amplitude = 1.05f;
        [SerializeField, Range(1, 7)] private int detailLayers = 5;
        [SerializeField, Range(0.1f, 1f)] private float roughness = 0.78f;
        [SerializeField] private BeamElectricalAnimationMode animationMode = BeamElectricalAnimationMode.HoldAndMorph;
        [SerializeField, Min(0f)] private float restrikeRate = 12f;
        [SerializeField, Range(0.01f, 1f)] private float transitionFraction = 0.18f;
        [SerializeField] private int seedOffset;

        private readonly List<BeamPoint> basePoints = new List<BeamPoint>(257);

        public override void Modify(in BeamPathContext context, BeamPathBuffer paths)
        {
            int pointCount = (1 << Mathf.Clamp(subdivisions, 1, 8)) + 1;
            for (int strandIndex = 0; strandIndex < paths.Count; strandIndex++)
            {
                BeamStrand strand = paths[strandIndex];
                if (strand.Count < 2)
                    continue;

                basePoints.Clear();
                for (int i = 0; i < pointCount; i++)
                    basePoints.Add(BeamPathUtility.Evaluate(strand, i / (pointCount - 1f)));

                int parent = strand.ParentStrandIndex;
                float parentPosition = strand.ParentPosition;
                int strandSeed = strand.Seed;
                int branchDepth = strand.BranchDepth;
                float deviation = Mathf.Min(amplitude, strand.Length * 0.35f) * Mathf.Pow(0.8f, branchDepth);
                // Parents are rebuilt first. Follow the measured junction through every restrike.
                Vector3 shift = parent >= 0 && parent < strandIndex
                    ? BeamPathUtility.Evaluate(paths[parent], parentPosition).Position - basePoints[0].Position
                    : Vector3.zero;
                strand.Clear();
                strand.ParentStrandIndex = parent;
                strand.ParentPosition = parentPosition;
                strand.Seed = strandSeed;
                strand.BranchDepth = branchDepth;

                ResolveAnimation(context.Time, out int currentStrike, out int nextStrike, out float strikeBlend);
                int seed = unchecked(context.Seed + seedOffset + strandSeed * 37);
                for (int i = 0; i < pointCount; i++)
                {
                    BeamPoint basis = basePoints[i];
                    basis.Position += shift;
                    float u = i / (pointCount - 1f);

                    if (i == 0 || i == pointCount - 1)
                    {
                        strand.Add(basis.Position);
                        continue;
                    }

                    float endpointEnvelope = Mathf.Sin(Mathf.PI * u);
                    float currentA = AngularNoise(u, seed + 11, currentStrike);
                    float currentB = AngularNoise(u, seed + 83, currentStrike);
                    float nextA = strikeBlend > 0f ? AngularNoise(u, seed + 11, nextStrike) : currentA;
                    float nextB = strikeBlend > 0f ? AngularNoise(u, seed + 83, nextStrike) : currentB;
                    float a = Mathf.LerpUnclamped(currentA, nextA, strikeBlend);
                    float b = Mathf.LerpUnclamped(currentB, nextB, strikeBlend);
                    Vector3 binormal = Vector3.Cross(basis.Tangent, basis.Normal).normalized;
                    Vector3 offset = (basis.Normal * a + binormal * b) * (deviation * endpointEnvelope);
                    strand.Add(basis.Position + offset);
                }

                BeamPathUtility.RecalculateMetrics(strand, context.Source.Forward);
            }
        }

        private void ResolveAnimation(float time, out int currentStrike, out int nextStrike, out float blend)
        {
            if (animationMode == BeamElectricalAnimationMode.Static || restrikeRate <= 0f)
            {
                currentStrike = 0;
                nextStrike = 0;
                blend = 0f;
                return;
            }

            float strikeTime = time * restrikeRate;
            currentStrike = Mathf.FloorToInt(strikeTime);
            nextStrike = currentStrike + 1;
            float phase = strikeTime - currentStrike;
            switch (animationMode)
            {
                case BeamElectricalAnimationMode.Snap:
                    blend = 0f;
                    break;
                case BeamElectricalAnimationMode.Morph:
                    blend = phase * phase * (3f - 2f * phase);
                    break;
                default:
                    float transitionStart = 1f - transitionFraction;
                    float transition = Mathf.InverseLerp(transitionStart, 1f, phase);
                    blend = transition * transition * (3f - 2f * transition);
                    break;
            }
        }

        private float AngularNoise(float u, int seed, int strike)
        {
            float value = 0f;
            float weight = 1f;
            float weightSum = 0f;
            for (int layer = 0; layer < detailLayers; layer++)
            {
                int cells = 3 << layer;
                float coordinate = u * cells;
                int cell = Mathf.FloorToInt(coordinate);
                float t = coordinate - cell;
                float a = HashSigned(cell, seed, strike, layer);
                float b = HashSigned(cell + 1, seed, strike, layer);
                value += Mathf.LerpUnclamped(a, b, t) * weight;
                weightSum += weight;
                weight *= roughness;
            }
            return weightSum > 0f ? value / weightSum : 0f;
        }

        private static float HashSigned(int cell, int seed, int strike, int layer)
        {
            unchecked
            {
                uint hash = (uint)seed ^ (uint)cell * 0x9e3779b9u ^ (uint)strike * 0x85ebca6bu ^ (uint)layer * 0xc2b2ae35u;
                hash ^= hash >> 16;
                hash *= 0x7feb352du;
                hash ^= hash >> 15;
                hash *= 0x846ca68bu;
                hash ^= hash >> 16;
                return (hash & 0xffffff) / 8388607.5f - 1f;
            }
        }

        private void OnValidate()
        {
            subdivisions = Mathf.Clamp(subdivisions, 1, 8);
            amplitude = Mathf.Max(0f, amplitude);
            detailLayers = Mathf.Clamp(detailLayers, 1, 7);
            roughness = Mathf.Clamp(roughness, 0.1f, 1f);
            restrikeRate = Mathf.Max(0f, restrikeRate);
            transitionFraction = Mathf.Clamp(transitionFraction, 0.01f, 1f);
        }
    }
}
