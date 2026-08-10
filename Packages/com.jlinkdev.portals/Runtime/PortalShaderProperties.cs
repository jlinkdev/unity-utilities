using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals
{
    public static class PortalShaderProperties
    {
        public static readonly int PortalTexture = Shader.PropertyToID("_PortalTexture");
        public static readonly int ClipPlane = Shader.PropertyToID("_PortalClipPlane");
        public static readonly int ClipEnabled = Shader.PropertyToID("_PortalClipEnabled");
    }
}
