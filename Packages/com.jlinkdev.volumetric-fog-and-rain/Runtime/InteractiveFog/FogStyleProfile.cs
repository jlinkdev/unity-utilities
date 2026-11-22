using UnityEngine;

namespace jlinkdev.UnityUtilities.InteractiveFog
{
    /// <summary>Shared appearance and motion settings. Quality, bounds and scene lights stay on the volume.</summary>
    [CreateAssetMenu(fileName = "Fog Style", menuName = "jlinkdev/Volumetric Fog and Rain/Interactive Fog/Style Profile")]
    public sealed class FogStyleProfile : ScriptableObject
    {
        public FogStyleSettings settings = FogStyleSettings.Default;
    }

    public enum FogDensityShape { GroundMist = 0, Cumulus = 1, DryIce = 2 }

    /// <summary>Displays the serialized optical extinction as a Density slider.</summary>
    public sealed class FogDensityAttribute : PropertyAttribute { }

    [System.Serializable]
    public struct FogStyleSettings
    {
        [Range(0,5)] public float vorticity;
        [Range(0,2)] public float turbulence;
        [Range(.25f,2)] public float eddySize;
        [Range(0,3)] public float velocityDamping;
        public Vector3 wind;
        public FogDensityShape densityShape;
        [Tooltip("Horizontal size of clustered lobes in metres; used by Cumulus.")]
        [Range(.5f,3)] public float puffSize;
        [Tooltip("Fills the spaces between cloud puffs. Zero preserves separated clusters; one adds overlapping smaller billows. Used by Cumulus only.")]
        [Range(0,1)] public float cloudCoverage;
        [Tooltip("Gentle vertical circulation in the 3D Cumulus style. Planar2D cannot transport density vertically.")]
        [Range(0,1)] public float plumeLift;
        [Range(.1f,2)] public float fogHeight;
        [Tooltip("Separates Ground Mist into curling banks with thinner channels between them. Zero keeps a continuous layer.")]
        [Range(0, 1)] public float groundPatchiness;
        [Tooltip("Adds density near the outer floor rim. Use Rest Recovery to restore the interior pattern.")]
        [Range(0,2)] public float replenishment;
        [Tooltip("Recovery rate per second in quiet areas. Zero disables return to the resting pattern; active wakes suppress recovery.")]
        [Range(0,2)] public float restRecovery;
        [FogDensity, Tooltip("Optical density: higher values make fog thicker and less transparent. Does not fill empty space; use Cloud Coverage to connect cloud puffs.")]
        public float extinction;
        public Color albedo, ambient;
        [Range(0,.8f)] public float anisotropy;
        [Range(0,1)] public float detail;
        [Range(.5f,3)] public float foldSize;
        [Range(0,.25f)] public float wispDisplacement;
        [Tooltip("Opacity of the separate curled ribbons in Cumulus; zero hides them without changing the cloud bodies.")]
        [Range(0,2)] public float wispOpacity;
        public FogPuffSettings puffs;

        public static FogStyleSettings Default => new()
        {
            puffSize=1.2f, puffs=FogPuffSettings.Default,
            vorticity=1.8f, turbulence=.5f, eddySize=1.2f, velocityDamping=.16f,
            wind=new Vector3(.08f,0,.03f), fogHeight=.62f, replenishment=.015f, restRecovery=.25f,
            extinction=5.2f, albedo=new Color(.78f,.79f,.78f), ambient=new Color(.19f,.21f,.23f),
            anisotropy=.25f, detail=.7f, foldSize=1.5f, wispDisplacement=.12f, wispOpacity=1
        };

        public static FogStyleSettings Cumulus
        {
            get
            {
                var s=Default;
                s.densityShape=FogDensityShape.Cumulus; s.puffSize=1.25f; s.plumeLift=.2f; s.cloudCoverage=.75f;
                s.fogHeight=1.6f; s.replenishment=0; s.restRecovery=.32f;
                s.extinction=12f; s.albedo=new Color(.68f,.63f,.76f);
                s.ambient=new Color(.4f,.384f,.448f); s.anisotropy=.15f;
                s.detail=.25f; s.foldSize=1.2f; s.wispDisplacement=.08f; s.wispOpacity=0;
                s.wind=new Vector3(.025f,0,.015f); s.velocityDamping=.35f;
                return s;
            }
        }

        public static FogStyleSettings DryIce
        {
            get
            {
                var s=Default; s.densityShape=FogDensityShape.DryIce;
                s.fogHeight=.55f; s.extinction=6.5f; s.restRecovery=.18f; s.replenishment=0;
                s.wind=new Vector3(.035f,0,.015f); s.velocityDamping=.45f;
                s.turbulence=.4f; s.detail=.65f; s.wispDisplacement=.18f; s.wispOpacity=0;
                s.albedo=new Color(.72f,.7f,.8f); s.ambient=new Color(.32f,.33f,.39f);
                s.anisotropy=.15f; s.puffs=FogPuffSettings.Default;
                return s;
            }
        }

        public static FogStyleSettings FromVolume(FogVolume v) => new()
        {
            densityShape=v.densityShape, puffSize=v.puffSize, cloudCoverage=v.cloudCoverage, plumeLift=v.plumeLift,
            vorticity=v.vorticity, turbulence=v.turbulence, eddySize=v.eddySize, velocityDamping=v.velocityDamping,
            wind=v.wind, fogHeight=v.fogHeight, groundPatchiness=v.groundPatchiness, replenishment=v.replenishment, restRecovery=v.restRecovery,
            extinction=v.extinction, albedo=v.albedo, ambient=v.ambient, anisotropy=v.anisotropy,
            detail=v.detail, foldSize=v.foldSize, wispDisplacement=v.wispDisplacement, wispOpacity=v.wispOpacity, puffs=v.puffs
        };
    }
}
