using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields
{
    public enum ForcefieldQuality
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    /// <summary>Reusable visual configuration for a <see cref="Forcefield"/>.</summary>
    [CreateAssetMenu(fileName = "Forcefield Preset", menuName = "jlinkdev/Forcefields/Forcefield Preset")]
    public sealed class ForcefieldPreset : ScriptableObject
    {
        [Header("Surface")]
        [SerializeField, ColorUsage(true, true)] private Color surfaceColor = new Color(0.015f, 0.32f, 0.55f, 1f);
        [SerializeField, Min(0f)] private float surfaceIntensity = 0.8f;
        [SerializeField, Range(0f, 1f)] private float opacity = 0.12f;
        [SerializeField, Range(0f, 1f)] private float backfaceOpacity = 0.35f;

        [Header("Fresnel")]
        [SerializeField, ColorUsage(true, true)] private Color fresnelColor = new Color(0.04f, 1.2f, 2.4f, 1f);
        [SerializeField, Min(0f)] private float fresnelIntensity = 1.5f;
        [SerializeField, Range(0.25f, 12f)] private float fresnelPower = 4f;

        [Header("Refraction")]
        [SerializeField] private bool refractionEnabled = true;
        [SerializeField, Range(0f, 0.1f)] private float refractionStrength = 0.018f;
        [SerializeField, Range(0f, 0.02f)] private float chromaticSplit = 0.0015f;

        [Header("Energy Noise")]
        [SerializeField] private bool noiseEnabled = true;
        [SerializeField, Min(0.01f)] private float noiseScale = 2.5f;
        [SerializeField] private Vector3 noiseVelocity = new Vector3(0.08f, 0.04f, -0.05f);
        [SerializeField, Range(0f, 1f)] private float noiseStrength = 0.3f;
        [SerializeField, Min(0f)] private float pulseSpeed = 0.65f;
        [SerializeField, Range(0f, 1f)] private float pulseStrength = 0.08f;

        [Header("Pattern")]
        [SerializeField] private bool patternEnabled = true;
        [SerializeField, ColorUsage(true, true)] private Color patternColor = new Color(0.03f, 0.75f, 1.7f, 1f);
        [SerializeField, Min(0.1f)] private float patternScale = 7f;
        [SerializeField, Range(0.001f, 0.2f)] private float patternWidth = 0.045f;
        [SerializeField, Min(0f)] private float patternIntensity = 0.35f;

        [Header("Impact Ripples")]
        [SerializeField, ColorUsage(true, true)] private Color impactColor = new Color(0.12f, 2.2f, 4f, 1f);
        [SerializeField, Min(0f)] private float impactIntensity = 2.5f;
        [SerializeField, Min(0.01f)] private float impactDuration = 1.35f;
        [SerializeField, Min(0f)] private float rippleSpeed = 2.8f;
        [SerializeField, Range(0.005f, 1f)] private float rippleWidth = 0.12f;
        [SerializeField, Range(0.1f, 8f)] private float rippleFadePower = 1.8f;
        [SerializeField, Range(0f, 0.1f)] private float rippleRefraction = 0.025f;

        [Header("Intersections")]
        [SerializeField] private bool intersectionEnabled = true;
        [SerializeField, ColorUsage(true, true)] private Color intersectionColor = new Color(0.08f, 1.4f, 2.8f, 1f);
        [SerializeField, Min(0f)] private float intersectionIntensity = 1.25f;
        [SerializeField, Range(0.001f, 2f)] private float intersectionWidth = 0.18f;

        [Header("Performance")]
        [SerializeField] private ForcefieldQuality quality = ForcefieldQuality.High;

        public Color SurfaceColor => surfaceColor;
        public float SurfaceIntensity => surfaceIntensity;
        public float Opacity => opacity;
        public float BackfaceOpacity => backfaceOpacity;
        public Color FresnelColor => fresnelColor;
        public float FresnelIntensity => fresnelIntensity;
        public float FresnelPower => fresnelPower;
        public bool RefractionEnabled => refractionEnabled;
        public float RefractionStrength => refractionStrength;
        public float ChromaticSplit => chromaticSplit;
        public bool NoiseEnabled => noiseEnabled;
        public float NoiseScale => noiseScale;
        public Vector3 NoiseVelocity => noiseVelocity;
        public float NoiseStrength => noiseStrength;
        public float PulseSpeed => pulseSpeed;
        public float PulseStrength => pulseStrength;
        public bool PatternEnabled => patternEnabled;
        public Color PatternColor => patternColor;
        public float PatternScale => patternScale;
        public float PatternWidth => patternWidth;
        public float PatternIntensity => patternIntensity;
        public Color ImpactColor => impactColor;
        public float ImpactIntensity => impactIntensity;
        public float ImpactDuration => impactDuration;
        public float RippleSpeed => rippleSpeed;
        public float RippleWidth => rippleWidth;
        public float RippleFadePower => rippleFadePower;
        public float RippleRefraction => rippleRefraction;
        public bool IntersectionEnabled => intersectionEnabled;
        public Color IntersectionColor => intersectionColor;
        public float IntersectionIntensity => intersectionIntensity;
        public float IntersectionWidth => intersectionWidth;
        public ForcefieldQuality Quality => quality;

        internal ForcefieldStyle Capture()
        {
            return new ForcefieldStyle
            {
                SurfaceColor = surfaceColor,
                SurfaceIntensity = surfaceIntensity,
                Opacity = opacity,
                BackfaceOpacity = backfaceOpacity,
                FresnelColor = fresnelColor,
                FresnelIntensity = fresnelIntensity,
                FresnelPower = fresnelPower,
                RefractionEnabled = refractionEnabled ? 1f : 0f,
                RefractionStrength = refractionStrength,
                ChromaticSplit = chromaticSplit,
                NoiseEnabled = noiseEnabled ? 1f : 0f,
                NoiseScale = noiseScale,
                NoiseVelocity = noiseVelocity,
                NoiseStrength = noiseStrength,
                PulseSpeed = pulseSpeed,
                PulseStrength = pulseStrength,
                PatternEnabled = patternEnabled ? 1f : 0f,
                PatternColor = patternColor,
                PatternScale = patternScale,
                PatternWidth = patternWidth,
                PatternIntensity = patternIntensity,
                ImpactColor = impactColor,
                ImpactIntensity = impactIntensity,
                ImpactDuration = impactDuration,
                RippleSpeed = rippleSpeed,
                RippleWidth = rippleWidth,
                RippleFadePower = rippleFadePower,
                RippleRefraction = rippleRefraction,
                IntersectionEnabled = intersectionEnabled ? 1f : 0f,
                IntersectionColor = intersectionColor,
                IntersectionIntensity = intersectionIntensity,
                IntersectionWidth = intersectionWidth,
                Quality = (float)quality
            };
        }
    }

    internal struct ForcefieldStyle
    {
        internal Color SurfaceColor;
        internal float SurfaceIntensity;
        internal float Opacity;
        internal float BackfaceOpacity;
        internal Color FresnelColor;
        internal float FresnelIntensity;
        internal float FresnelPower;
        internal float RefractionEnabled;
        internal float RefractionStrength;
        internal float ChromaticSplit;
        internal float NoiseEnabled;
        internal float NoiseScale;
        internal Vector3 NoiseVelocity;
        internal float NoiseStrength;
        internal float PulseSpeed;
        internal float PulseStrength;
        internal float PatternEnabled;
        internal Color PatternColor;
        internal float PatternScale;
        internal float PatternWidth;
        internal float PatternIntensity;
        internal Color ImpactColor;
        internal float ImpactIntensity;
        internal float ImpactDuration;
        internal float RippleSpeed;
        internal float RippleWidth;
        internal float RippleFadePower;
        internal float RippleRefraction;
        internal float IntersectionEnabled;
        internal Color IntersectionColor;
        internal float IntersectionIntensity;
        internal float IntersectionWidth;
        internal float Quality;

        internal static ForcefieldStyle Default => new ForcefieldStyle
        {
            SurfaceColor = new Color(0.015f, 0.32f, 0.55f, 1f),
            SurfaceIntensity = 0.8f,
            Opacity = 0.12f,
            BackfaceOpacity = 0.35f,
            FresnelColor = new Color(0.04f, 1.2f, 2.4f, 1f),
            FresnelIntensity = 1.5f,
            FresnelPower = 4f,
            RefractionEnabled = 1f,
            RefractionStrength = 0.018f,
            ChromaticSplit = 0.0015f,
            NoiseEnabled = 1f,
            NoiseScale = 2.5f,
            NoiseVelocity = new Vector3(0.08f, 0.04f, -0.05f),
            NoiseStrength = 0.3f,
            PulseSpeed = 0.65f,
            PulseStrength = 0.08f,
            PatternEnabled = 1f,
            PatternColor = new Color(0.03f, 0.75f, 1.7f, 1f),
            PatternScale = 7f,
            PatternWidth = 0.045f,
            PatternIntensity = 0.35f,
            ImpactColor = new Color(0.12f, 2.2f, 4f, 1f),
            ImpactIntensity = 2.5f,
            ImpactDuration = 1.35f,
            RippleSpeed = 2.8f,
            RippleWidth = 0.12f,
            RippleFadePower = 1.8f,
            RippleRefraction = 0.025f,
            IntersectionEnabled = 1f,
            IntersectionColor = new Color(0.08f, 1.4f, 2.8f, 1f),
            IntersectionIntensity = 1.25f,
            IntersectionWidth = 0.18f,
            Quality = (float)ForcefieldQuality.High
        };

        internal static ForcefieldStyle Lerp(ForcefieldStyle a, ForcefieldStyle b, float t)
        {
            t = Mathf.Clamp01(t);
            ForcefieldStyle result = a;
            result.SurfaceColor = Color.LerpUnclamped(a.SurfaceColor, b.SurfaceColor, t);
            result.SurfaceIntensity = Mathf.LerpUnclamped(a.SurfaceIntensity, b.SurfaceIntensity, t);
            result.Opacity = Mathf.LerpUnclamped(a.Opacity, b.Opacity, t);
            result.BackfaceOpacity = Mathf.LerpUnclamped(a.BackfaceOpacity, b.BackfaceOpacity, t);
            result.FresnelColor = Color.LerpUnclamped(a.FresnelColor, b.FresnelColor, t);
            result.FresnelIntensity = Mathf.LerpUnclamped(a.FresnelIntensity, b.FresnelIntensity, t);
            result.FresnelPower = Mathf.LerpUnclamped(a.FresnelPower, b.FresnelPower, t);
            result.RefractionEnabled = Mathf.LerpUnclamped(a.RefractionEnabled, b.RefractionEnabled, t);
            result.RefractionStrength = Mathf.LerpUnclamped(a.RefractionStrength, b.RefractionStrength, t);
            result.ChromaticSplit = Mathf.LerpUnclamped(a.ChromaticSplit, b.ChromaticSplit, t);
            result.NoiseEnabled = Mathf.LerpUnclamped(a.NoiseEnabled, b.NoiseEnabled, t);
            result.NoiseScale = Mathf.LerpUnclamped(a.NoiseScale, b.NoiseScale, t);
            result.NoiseVelocity = Vector3.LerpUnclamped(a.NoiseVelocity, b.NoiseVelocity, t);
            result.NoiseStrength = Mathf.LerpUnclamped(a.NoiseStrength, b.NoiseStrength, t);
            result.PulseSpeed = Mathf.LerpUnclamped(a.PulseSpeed, b.PulseSpeed, t);
            result.PulseStrength = Mathf.LerpUnclamped(a.PulseStrength, b.PulseStrength, t);
            result.PatternEnabled = Mathf.LerpUnclamped(a.PatternEnabled, b.PatternEnabled, t);
            result.PatternColor = Color.LerpUnclamped(a.PatternColor, b.PatternColor, t);
            result.PatternScale = Mathf.LerpUnclamped(a.PatternScale, b.PatternScale, t);
            result.PatternWidth = Mathf.LerpUnclamped(a.PatternWidth, b.PatternWidth, t);
            result.PatternIntensity = Mathf.LerpUnclamped(a.PatternIntensity, b.PatternIntensity, t);
            result.ImpactColor = Color.LerpUnclamped(a.ImpactColor, b.ImpactColor, t);
            result.ImpactIntensity = Mathf.LerpUnclamped(a.ImpactIntensity, b.ImpactIntensity, t);
            result.ImpactDuration = Mathf.LerpUnclamped(a.ImpactDuration, b.ImpactDuration, t);
            result.RippleSpeed = Mathf.LerpUnclamped(a.RippleSpeed, b.RippleSpeed, t);
            result.RippleWidth = Mathf.LerpUnclamped(a.RippleWidth, b.RippleWidth, t);
            result.RippleFadePower = Mathf.LerpUnclamped(a.RippleFadePower, b.RippleFadePower, t);
            result.RippleRefraction = Mathf.LerpUnclamped(a.RippleRefraction, b.RippleRefraction, t);
            result.IntersectionEnabled = Mathf.LerpUnclamped(a.IntersectionEnabled, b.IntersectionEnabled, t);
            result.IntersectionColor = Color.LerpUnclamped(a.IntersectionColor, b.IntersectionColor, t);
            result.IntersectionIntensity = Mathf.LerpUnclamped(a.IntersectionIntensity, b.IntersectionIntensity, t);
            result.IntersectionWidth = Mathf.LerpUnclamped(a.IntersectionWidth, b.IntersectionWidth, t);
            result.Quality = Mathf.LerpUnclamped(a.Quality, b.Quality, t);
            return result;
        }
    }
}
