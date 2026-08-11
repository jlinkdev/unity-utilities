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
    }
}
