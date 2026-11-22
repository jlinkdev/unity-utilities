using jlinkdev.UnityUtilities.VolumetricFogAndRain;

namespace jlinkdev.UnityUtilities.VolumetricRain.EditableSamples
{
    /// <summary>Preserves references from existing Rain Laboratory scenes.</summary>
    public sealed class CourtyardRainExposure : RainSurfaceExposure
    {
        public WetRainSurface stone { get => surface; set => surface = value; }
    }
}
