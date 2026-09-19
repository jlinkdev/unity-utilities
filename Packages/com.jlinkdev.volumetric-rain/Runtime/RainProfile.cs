using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricRain
{
    public enum RainExtent { Unbounded, VolumesOnly }

    public enum RainDebugView { Composite, Streaks, Haze, TraversalCost }

    [CreateAssetMenu(menuName = "jlinkdev/Volumetric Rain/Profile", fileName = "Rain Profile")]
    public sealed class RainProfile : ScriptableObject
    {
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
        }

        /// <summary>Explicitly normalize programmatic settings, including distance ordering.</summary>
        public void Sanitize()
        {
            ClampValues();
            midDistance = Mathf.Max(nearFade + 0.1f, midDistance);
            farDistance = Mathf.Max(midDistance + 0.1f, farDistance);
            maxDistance = Mathf.Max(farDistance, maxDistance);
        }

        // Authoring may temporarily have unordered distances while several fields are edited.
        // The renderer safely resolves them without rewriting the user's asset.
        private void OnValidate() => ClampValues();
    }
}
