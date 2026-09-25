using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain
{
    [DisallowMultipleComponent, AddComponentMenu("jlinkdev/Volumetric Fog and Rain/Rain Volume")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "jlinkdev.UnityUtilities.VolumetricRain", "jlinkdev.VolumetricRain", null)]
    public sealed class RainVolume : RainBoxVolume
    {
        internal override bool ExcludesRain => false;
    }
}
