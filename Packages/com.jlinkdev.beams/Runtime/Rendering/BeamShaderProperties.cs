using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Stable shader property names supplied by compatible beam renderers.</summary>
    public static class BeamShaderProperties
    {
        public static readonly int Color = Shader.PropertyToID("_BeamColor");
        public static readonly int Intensity = Shader.PropertyToID("_BeamIntensity");
        public static readonly int Length = Shader.PropertyToID("_BeamLength");
        public static readonly int Time = Shader.PropertyToID("_BeamTime");
        public static readonly int Age = Shader.PropertyToID("_BeamAge");
        public static readonly int Seed = Shader.PropertyToID("_BeamSeed");
        public static readonly int PulsePosition = Shader.PropertyToID("_BeamPulsePosition");
        public static readonly int Activation = Shader.PropertyToID("_BeamActivation");
    }
}
