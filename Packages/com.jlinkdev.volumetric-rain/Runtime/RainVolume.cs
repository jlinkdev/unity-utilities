using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricRain
{
    [DisallowMultipleComponent, AddComponentMenu("jlinkdev/Volumetric Rain/Rain Volume")]
    public sealed class RainVolume : RainBoxVolume
    {
        internal override bool ExcludesRain => false;
    }
}
