using UnityEngine;
using UnityEngine.Serialization;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain
{
    /// <summary>Couples one group's wetness and new surface impacts to the viewing camera's rain.</summary>
    [DefaultExecutionOrder(-100), DisallowMultipleComponent]
    public class RainSurfaceExposure : MonoBehaviour
    {
        public VolumetricRainCamera weather;
        [FormerlySerializedAs("stone")] public WetRainSurface surface;
        [Tooltip("Parent rain emitters. Their collision sub-emitters keep their authored rates.")]
        public ParticleSystem[] impacts;

        private float authoredExposure;
        private float[] authoredRates;

        protected void OnEnable()
        {
            if (surface) authoredExposure = surface.rainAmount;
            authoredRates = new float[impacts?.Length ?? 0];
            for (int i = 0; i < authoredRates.Length; i++)
                if (impacts[i]) authoredRates[i] = impacts[i].emission.rateOverTimeMultiplier;
        }

        protected void Update()
        {
            var settings = weather && weather.isActiveAndEnabled ? weather.ResolveSettings() : null;
            Apply(settings != null && settings.HasRain ? Mathf.Clamp01(weather.intensity) : 0f);
        }

        protected void OnDisable() => Apply(1f);

        private void Apply(float rainfall)
        {
            if (surface) surface.rainAmount = authoredExposure * rainfall;
            for (int i = 0; i < authoredRates.Length && i < (impacts?.Length ?? 0); i++)
            {
                if (!impacts[i]) continue;
                var emission = impacts[i].emission;
                emission.rateOverTimeMultiplier = authoredRates[i] * rainfall;
            }
        }
    }
}
