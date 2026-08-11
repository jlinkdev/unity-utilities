using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals
{
    public static class PortalShaderProperties
    {
        public static readonly int PortalTexture = Shader.PropertyToID("_PortalTexture");
        public static readonly int PortalTerminal = Shader.PropertyToID("_PortalTerminal");
        public static readonly int TerminalColor = Shader.PropertyToID("_TerminalColor");
        public static readonly int TerminalGlowColor = Shader.PropertyToID("_TerminalGlowColor");
        public static readonly int TerminalGlowIntensity = Shader.PropertyToID("_TerminalGlowIntensity");
        public static readonly int ClipPlane = Shader.PropertyToID("_PortalClipPlane");
        public static readonly int ClipEnabled = Shader.PropertyToID("_PortalClipEnabled");
        internal static readonly int PortalWorldToLocal = Shader.PropertyToID("_PortalWorldToLocal");
        internal static readonly int PortalBounds = Shader.PropertyToID("_PortalBounds");
        internal static readonly int PortalPlane = Shader.PropertyToID("_PortalPlane");
        internal static readonly int CameraForward = Shader.PropertyToID("_CameraForward");
        internal static readonly int CapDistance = Shader.PropertyToID("_CapDistance");
        internal static readonly int Tint = Shader.PropertyToID("_Tint");
        internal static readonly int EdgeColor = Shader.PropertyToID("_EdgeColor");
        internal static readonly int EdgeWidth = Shader.PropertyToID("_EdgeWidth");
    }
}
