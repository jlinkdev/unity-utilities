using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain
{
    /// <summary>Drives glass, opaque wet surfaces, or ordinary Lit materials using local rain exposure.</summary>
    [DisallowMultipleComponent]
    public sealed class WetRainSurface : MonoBehaviour
    {
        [Tooltip("Renderers sharing this exposure. An empty list uses the renderer on this object.")]
        public Renderer[] surfaces;
        public bool glass = true;
        [Range(0, 1)] public float rainAmount = 1;
        [Tooltip("Retained wetness when the scene starts. Use the material's Wetness value to preview a pre-wet surface in Edit Mode.")]
        [Range(0, 1)] public float initialWetness;
        [Min(.1f)] public float wetSeconds = 8;
        [Min(.1f)] public float drySeconds = 55;
        [Range(0, 1)] public float wetDarkening = .15f;
        [Range(0, 1)] public float wetSmoothness = .48f;
        private readonly RainSurfaceState state = new();
        private Renderer[] ownRenderer;
        private float elapsed;

        public float Wetness => state.Wetness;

        private void OnEnable()
        {
            if (TryGetComponent<Renderer>(out var surface)) ownRenderer = new[] { surface };
        }

        private void Start() => state.SetWetness(initialWetness);

        private void Update()
        {
            float seconds = Time.deltaTime;
            elapsed += seconds;
            state.Advance(rainAmount, rainAmount > .001f, seconds, wetSeconds, drySeconds);
            state.Apply(surfaces == null || surfaces.Length == 0 ? ownRenderer : surfaces,
                glass, elapsed, wetDarkening, wetSmoothness);
        }

        private void OnDisable() => state.Restore();
    }
}
