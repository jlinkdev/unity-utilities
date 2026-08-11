using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields
{
    internal static class ForcefieldShaderProperties
    {
        internal static readonly int RootLocalToWorld = Shader.PropertyToID("_ForcefieldRootLocalToWorld");
        internal static readonly int Intensity = Shader.PropertyToID("_ForcefieldIntensity");
        internal static readonly int PropagationMode = Shader.PropertyToID("_ForcefieldPropagationMode");
        internal static readonly int SphereRadius = Shader.PropertyToID("_ForcefieldSphereRadius");
        internal static readonly int ImpactCount = Shader.PropertyToID("_ForcefieldImpactCount");
        internal static readonly int ImpactPositions = Shader.PropertyToID("_ForcefieldImpactPositionTime");
        internal static readonly int ImpactNormals = Shader.PropertyToID("_ForcefieldImpactNormalStrength");
        internal static readonly int ImpactRadii = Shader.PropertyToID("_ForcefieldImpactRadiusDuration");
        internal static readonly int SurfaceColor = Shader.PropertyToID("_SurfaceColor");
        internal static readonly int SurfaceIntensity = Shader.PropertyToID("_SurfaceIntensity");
        internal static readonly int Opacity = Shader.PropertyToID("_Opacity");
        internal static readonly int BackfaceOpacity = Shader.PropertyToID("_BackfaceOpacity");
        internal static readonly int FresnelColor = Shader.PropertyToID("_FresnelColor");
        internal static readonly int FresnelIntensity = Shader.PropertyToID("_FresnelIntensity");
        internal static readonly int FresnelPower = Shader.PropertyToID("_FresnelPower");
        internal static readonly int RefractionEnabled = Shader.PropertyToID("_RefractionEnabled");
        internal static readonly int RefractionStrength = Shader.PropertyToID("_RefractionStrength");
        internal static readonly int ChromaticSplit = Shader.PropertyToID("_ChromaticSplit");
        internal static readonly int NoiseEnabled = Shader.PropertyToID("_NoiseEnabled");
        internal static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
        internal static readonly int NoiseVelocity = Shader.PropertyToID("_NoiseVelocity");
        internal static readonly int NoiseStrength = Shader.PropertyToID("_NoiseStrength");
        internal static readonly int PulseSpeed = Shader.PropertyToID("_PulseSpeed");
        internal static readonly int PulseStrength = Shader.PropertyToID("_PulseStrength");
        internal static readonly int PatternEnabled = Shader.PropertyToID("_PatternEnabled");
        internal static readonly int PatternColor = Shader.PropertyToID("_PatternColor");
        internal static readonly int PatternScale = Shader.PropertyToID("_PatternScale");
        internal static readonly int PatternWidth = Shader.PropertyToID("_PatternWidth");
        internal static readonly int PatternIntensity = Shader.PropertyToID("_PatternIntensity");
        internal static readonly int ImpactColor = Shader.PropertyToID("_ImpactColor");
        internal static readonly int ImpactIntensity = Shader.PropertyToID("_ImpactIntensity");
        internal static readonly int RippleSpeed = Shader.PropertyToID("_RippleSpeed");
        internal static readonly int RippleWidth = Shader.PropertyToID("_RippleWidth");
        internal static readonly int RippleFadePower = Shader.PropertyToID("_RippleFadePower");
        internal static readonly int RippleRefraction = Shader.PropertyToID("_RippleRefraction");
        internal static readonly int IntersectionEnabled = Shader.PropertyToID("_IntersectionEnabled");
        internal static readonly int IntersectionColor = Shader.PropertyToID("_IntersectionColor");
        internal static readonly int IntersectionIntensity = Shader.PropertyToID("_IntersectionIntensity");
        internal static readonly int IntersectionWidth = Shader.PropertyToID("_IntersectionWidth");
        internal static readonly int Quality = Shader.PropertyToID("_Quality");
    }
}
