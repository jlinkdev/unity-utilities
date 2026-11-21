using System;
using UnityEngine;

namespace jlinkdev.UnityUtilities.WaterSystem
{
    /// <summary>A directional, vertically displaced wave. Speed is metres per second; phase is radians.</summary>
    [Serializable]
    public struct WaveSettings
    {
        public float Direction;
        [Min(0f)] public float Amplitude;
        [Min(0.1f)] public float Wavelength;
        public float Speed;
        public float Phase;

        public WaveSettings(float direction, float amplitude, float wavelength, float speed, float phase = 0f)
        {
            Direction = direction; Amplitude = amplitude; Wavelength = wavelength;
            Speed = speed; Phase = phase;
        }
    }

    /// <summary>Shared settings for geometry, shading, underwater optics and gameplay queries.</summary>
    [CreateAssetMenu(menuName = "jlinkdev/Water System/Water Profile", fileName = "Water Profile")]
    public sealed class WaterProfile : ScriptableObject
    {
        public const int MaximumWaves = 8;

        [Header("Surface shape (first eight waves)")]
        public WaveSettings[] Waves = {
            new WaveSettings(15f, 0.22f, 9f, 1.3f),
            new WaveSettings(80f, 0.10f, 4.2f, 0.8f, 1.2f),
            new WaveSettings(135f, 0.045f, 2f, 0.5f, 2f)
        };

        [Header("Surface optics")]
        public Color ShallowColor = new Color(0.12f, 0.63f, 0.62f, 1f);
        public Color DeepColor = new Color(0.015f, 0.13f, 0.22f, 1f);
        [Tooltip("RGB absorption coefficients per metre.")]
        public Vector3 Absorption = new Vector3(0.28f, 0.08f, 0.045f);
        [Range(0f, 0.05f)] public float Refraction = 0.012f;
        [Range(0f, 1f)] public float Smoothness = 0.88f;
        [Range(0f, 0.5f)] public float DetailNormalStrength = 0.10f;
        public Color FoamColor = new Color(0.82f, 0.97f, 0.96f, 1f);
        [Min(0f)] public float FoamWidth = 0.35f;
        [Range(0f, 1f)] public float FoamStrength = 0.65f;

        [Header("Underwater")]
        public Color FogColor = new Color(0.045f, 0.30f, 0.36f, 1f);
        [Min(0f)] public float Density = 0.075f;
        [Range(0f, 1f)] public float UnderwaterSaturation = 0.65f;
        [Range(0f, 0.02f)] public float Distortion = 0.0015f;

        [Header("Waterline")]
        public Color WaterlineColor = new Color(0.55f, 0.86f, 0.83f, 1f);
        [Range(0f, 20f), Tooltip("Boundary band width in render-target pixels.")]
        public float WaterlineWidth = 3f;
        [Range(0f, 1f)] public float WaterlineStrength = 0.45f;

        [Header("Caustics")]
        [Range(0f, 3f)] public float CausticsStrength = 0.45f;
        [Min(0.01f)] public float CausticsScale = 0.5f;
        public float CausticsSpeed = 0.35f;

        public float MaximumDisplacement
        {
            get
            {
                float sum = 0f;
                if (Waves == null) return sum;
                for (int i = 0; i < Mathf.Min(Waves.Length, MaximumWaves); i++)
                    sum += Mathf.Clamp(WaterWaves.Finite(Waves[i].Amplitude), 0f, 1000f);
                return sum;
            }
        }
    }
}
