using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain
{
    public enum RainRenderMode { RainAndFog, RainOnly, FogOnly }

    public enum FogDistanceMode { RainLinked, Independent }

    public enum BlendDiscreteSettings { AtEnd, AtStart }

    public enum RainExtent { Unbounded, VolumesOnly }

    public enum RainDebugView { Composite, Streaks, Haze, TraversalCost }

    [CreateAssetMenu(menuName = "jlinkdev/Volumetric Fog and Rain/Profile", fileName = "Rain Profile")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "jlinkdev.UnityUtilities.VolumetricRain", "jlinkdev.VolumetricRain", null)]
    public sealed class RainProfile : ScriptableObject
    {
        public RainRenderMode renderMode;

        [Tooltip("Volumes Only renders the union of enabled Rain Volume boxes. Exclusion boxes apply in either mode.")]
        public RainExtent extent;

        [Header("Field (world units are metres)")]
        [Range(0, 1)] public float density = 0.8f;
        [Min(0.1f), Tooltip("Smaller cells yield more streaks and more cell visits. Independent of density.")]
        public float cellSize = 0.65f;
        [Min(0.001f)] public float streakLength = 0.35f;
        [Min(0.0001f), Tooltip("Physical capsule radius, not diameter.")] public float streakWidth = 0.008f;
        [Min(0)] public float fallSpeed = 12;
        public Vector3 direction = Vector3.down;
        [Tooltip("Additional world-space velocity in metres/second.")] public Vector3 wind = new Vector3(1, 0, 0);
        public int seed = 1234;

        [Header("Distance and integration")]
        [Min(0)] public float nearFade = 0.3f;
        [Min(0.1f)] public float midDistance = 12;
        [Min(0.2f)] public float farDistance = 30;
        [Min(1)] public float maxDistance = 100;
        [Range(16, 256), Tooltip("Maximum visits per grid (two grids). Insufficient budgets shorten the streak range; haze takes over smoothly.")]
        public int maxCellSteps = 96;
        [Range(4, 32)] public int hazeSteps = 12;

        [Header("Large-scale density")]
        [Min(0.001f)] public float noiseScale = 0.025f;
        [Range(0, 1)] public float noiseStrength = 0.35f;

        [Header("Appearance")]
        [ColorUsage(false, true)] public Color rainColor = new Color(0.65f, 0.75f, 0.85f, 1);
        [Min(0)] public float brightness = 1.5f;
        [Min(0)] public float streakOpacity = 0.7f;
        [Min(0)] public float hazeExtinction = 0.015f;
        [Min(0)] public float scattering = 0.65f;

        [Header("Independent fog appearance")]
        [Tooltip("In Rain And Fog mode, use separate fog controls instead of the original rain-linked haze. Fog Only always uses these controls.")]
        public bool independentFogSettings;
        [Min(0), Tooltip("Fog density multiplier; independent of streak occupancy.")]
        public float fogDensity = 0.8f;
        [ColorUsage(false, true)] public Color fogColor = new Color(0.65f, 0.75f, 0.85f, 1);
        [Min(0)] public float fogBrightness = 1.5f;
        [Min(0)] public float fogScattering = 0.65f;

        [Header("Fog distribution")]
        [Tooltip("Independent fog ignores the streak transition and cell budget. Fog Only is always independent.")]
        public FogDistanceMode fogDistanceMode;
        [Min(0)] public float fogStartDistance;
        [Min(0)] public float fogDistanceFade;
        [Min(1)] public float fogMaxDistance = 100;
        public float fogBaseHeight;
        [Min(0), Tooltip("Exponential density falloff per metre above Base Height. Zero disables height falloff.")]
        public float fogHeightFalloff;
        [Tooltip("World-space fog noise velocity in metres/second, independent of rain motion.")]
        public Vector3 fogNoiseVelocity;
        [Header("Directional scattering (optional)")]
        [Min(0), Tooltip("Contribution from the URP main directional light. Zero disables lighting; shadows are not sampled.")]
        public float directionalScattering;
        [Range(-0.9f, 0.9f), Tooltip("Positive values glow toward the light. Zero is isotropic.")]
        public float scatteringAnisotropy = 0.4f;

        public bool UsesIndependentFog => renderMode == RainRenderMode.FogOnly || independentFogSettings;
        public bool HasRain => renderMode != RainRenderMode.FogOnly && density > 0 && streakOpacity > 0;
        public bool HasFog => renderMode != RainRenderMode.RainOnly && hazeExtinction > 0 &&
            (UsesIndependentFog ? fogDensity > 0 : density > 0);

        public Vector3 Velocity => (direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.down) * fallSpeed + wind;

        private void ClampValues()
        {
            density = Mathf.Clamp01(density);
            cellSize = Mathf.Max(0.1f, cellSize);
            // Containment leaves room for spatial filtering. The grid is 3x taller along velocity.
            streakWidth = Mathf.Clamp(streakWidth, 0.0001f, cellSize * 0.04f);
            streakLength = Mathf.Clamp(streakLength, 0.001f, cellSize * 1.5f);
            fallSpeed = Mathf.Max(0, fallSpeed);
            nearFade = Mathf.Max(0, nearFade);
            midDistance = Mathf.Max(0.1f, midDistance);
            farDistance = Mathf.Max(0.2f, farDistance);
            maxDistance = Mathf.Max(1, maxDistance);
            maxCellSteps = Mathf.Clamp(maxCellSteps, 16, 256);
            hazeSteps = Mathf.Clamp(hazeSteps, 4, 32);
            noiseScale = Mathf.Max(0.001f, noiseScale);
            noiseStrength = Mathf.Clamp01(noiseStrength);
            brightness = Mathf.Max(0, brightness);
            streakOpacity = Mathf.Max(0, streakOpacity);
            hazeExtinction = Mathf.Max(0, hazeExtinction);
            scattering = Mathf.Max(0, scattering);
            fogDensity = Mathf.Max(0, fogDensity);
            fogBrightness = Mathf.Max(0, fogBrightness);
            fogScattering = Mathf.Max(0, fogScattering);
            fogStartDistance = Mathf.Max(0, fogStartDistance);
            fogDistanceFade = Mathf.Max(0, fogDistanceFade);
            fogMaxDistance = Mathf.Max(1, fogMaxDistance);
            fogHeightFalloff = Mathf.Max(0, fogHeightFalloff);
            directionalScattering = Mathf.Max(0, directionalScattering);
            scatteringAnisotropy = Mathf.Clamp(scatteringAnisotropy, -0.9f, 0.9f);
        }

        /// <summary>Explicitly normalize programmatic settings, including distance ordering.</summary>
        public void Sanitize()
        {
            ClampValues();
            if (renderMode == RainRenderMode.FogOnly) return;
            midDistance = Mathf.Max(nearFade + 0.1f, midDistance);
            farDistance = Mathf.Max(midDistance + 0.1f, farDistance);
            maxDistance = Mathf.Max(farDistance, maxDistance);
        }

        // Authoring may temporarily have unordered distances while several fields are edited.
        // The renderer safely resolves them without rewriting the user's asset.
        private void OnValidate() => ClampValues();
    }
}
