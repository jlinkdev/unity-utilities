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
        [SerializeField, Min(0f)] private float amplitude = 0.35f;
        [SerializeField, Range(1, 7)] private int detailLayers = 5;
        [SerializeField, Range(0.1f, 1f)] private float roughness = 0.55f;
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
                strand.Clear();
                strand.ParentStrandIndex = parent;
                strand.ParentPosition = parentPosition;
                strand.Seed = strandSeed;
                strand.BranchDepth = branchDepth;

                ResolveAnimation(context.Time, out int currentStrike, out int nextStrike, out float strikeBlend);
                float seed = context.Seed + seedOffset + strandSeed * 37.17f;
                for (int i = 0; i < pointCount; i++)
                {
                    BeamPoint basis = basePoints[i];
                    float u = i / (pointCount - 1f);

                    if (i == 0 || i == pointCount - 1)
                    {
                        strand.Add(basis.Position);
                        continue;
                    }

                    float endpointEnvelope = Mathf.Sin(Mathf.PI * u);
                    float currentA = AngularNoise(u, seed + 11.3f, currentStrike);
                    float currentB = AngularNoise(u, seed + 83.7f, currentStrike);
                    float nextA = AngularNoise(u, seed + 11.3f, nextStrike);
                    float nextB = AngularNoise(u, seed + 83.7f, nextStrike);
                    float a = Mathf.LerpUnclamped(currentA, nextA, strikeBlend);
                    float b = Mathf.LerpUnclamped(currentB, nextB, strikeBlend);
                    Vector3 binormal = Vector3.Cross(basis.Tangent, basis.Normal).normalized;
                    Vector3 offset = (basis.Normal * a + binormal * b) * (amplitude * endpointEnvelope);
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

        private float AngularNoise(float u, float seed, int strike)
        {
            float value = 0f;
            float weight = 1f;
            float weightSum = 0f;
            for (int layer = 0; layer < detailLayers; layer++)
            {
                int cells = 1 << (layer + 1);
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

        private static float HashSigned(int cell, float seed, int strike, int layer)
        {
            float value = Mathf.Sin(cell * 12.9898f + seed * 78.233f + strike * 37.719f + layer * 19.913f) * 43758.5453f;
            return (value - Mathf.Floor(value)) * 2f - 1f;
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
