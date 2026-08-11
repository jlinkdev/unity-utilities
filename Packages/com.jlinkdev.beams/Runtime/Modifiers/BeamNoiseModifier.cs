using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    [AddComponentMenu("jlinkdev/Beams/Modifiers/Beam Noise Modifier")]
    public sealed class BeamNoiseModifier : BeamPathModifier
    {
        [SerializeField, Min(0f)] private float amplitude = 0.15f;
        [SerializeField, Min(0.001f)] private float frequencyPerMeter = 1.5f;
        [SerializeField, Range(1, 6)] private int octaves = 3;
        [SerializeField, Range(0f, 1f)] private float roughness = 0.5f;
        [SerializeField, Min(0.01f)] private float endpointPinPower = 0.8f;
        [SerializeField] private float animationSpeed = 0.6f;
        [SerializeField] private int seedOffset;

        public float Amplitude
        {
            get => amplitude;
            set => amplitude = Mathf.Max(0f, value);
        }

        public override void Modify(in BeamPathContext context, BeamPathBuffer paths)
        {
            if (amplitude <= 0f)
                return;

            for (int strandIndex = 0; strandIndex < paths.Count; strandIndex++)
            {
                BeamStrand strand = paths[strandIndex];
                int seed = context.Seed + seedOffset + strand.Seed * 31;
                for (int i = 1; i < strand.Count - 1; i++)
                {
                    BeamPoint point = strand[i];
                    float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * point.NormalizedDistance), endpointPinPower);
                    float coordinate = point.Distance * frequencyPerMeter;
                    float time = context.Time * animationSpeed;
                    float a = FractalNoise(coordinate + seed * 0.173f, time + seed * 0.071f);
                    float b = FractalNoise(coordinate + seed * 0.311f + 19.19f, time + 47.47f);
                    Vector3 binormal = Vector3.Cross(point.Tangent, point.Normal).normalized;
                    point.Position += (point.Normal * a + binormal * b) * (amplitude * envelope);
                    strand[i] = point;
                }

                BeamPathUtility.RecalculateMetrics(strand, context.Source.Forward);
            }
        }

        private float FractalNoise(float x, float y)
        {
            float value = 0f;
            float weight = 1f;
            float weightSum = 0f;
            for (int octave = 0; octave < octaves; octave++)
            {
                value += (Mathf.PerlinNoise(x, y) * 2f - 1f) * weight;
                weightSum += weight;
                x *= 2f;
                y *= 2f;
                weight *= roughness;
            }

            return weightSum > 0f ? value / weightSum : 0f;
        }

        private void OnValidate()
        {
            amplitude = Mathf.Max(0f, amplitude);
            frequencyPerMeter = Mathf.Max(0.001f, frequencyPerMeter);
            octaves = Mathf.Clamp(octaves, 1, 6);
            roughness = Mathf.Clamp01(roughness);
            endpointPinPower = Mathf.Max(0.01f, endpointPinPower);
        }
    }
}
