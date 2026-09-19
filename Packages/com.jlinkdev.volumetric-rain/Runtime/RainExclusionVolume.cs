using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricRain
{
    [DisallowMultipleComponent, AddComponentMenu("jlinkdev/Volumetric Rain/Rain Exclusion Volume")]
    public sealed class RainExclusionVolume : RainBoxVolume
    {
        internal override bool ExcludesRain => true;
    }
}
