using jlinkdev.UnityUtilities.VolumetricFogAndRain;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace jlinkdev.UnityUtilities.InteractiveFog
{
    /// <summary>An optional snapshot of the atmosphere already composited for this camera.</summary>
    internal readonly struct CameraAtmosphere
    {
        private static readonly int atlasId = Shader.PropertyToID("_FogAtmosphereAtlas");
        private static readonly int layoutId = Shader.PropertyToID("_FogAtmosphereLayout");
        private static readonly int premultipliedId = Shader.PropertyToID("_FogAtmospherePremultiplied");
        private readonly TextureHandle atlas;
        private readonly Vector4 layout;
        private readonly bool premultiplied;

        private CameraAtmosphere(TextureHandle atlas, Vector4 layout, bool premultiplied)
        {
            this.atlas = atlas;
            this.layout = layout;
            this.premultiplied = premultiplied;
        }

        internal static CameraAtmosphere From(ContextContainer frame)
        {
            if (WeatherAtmosphere.TryGetAtmosphere(frame, out var atlas, out var layout, out var premultiplied))
                return new CameraAtmosphere(atlas, layout, premultiplied);
            return default;
        }

        internal void DeclareRead(IRasterRenderGraphBuilder builder)
        {
            if (atlas.IsValid()) builder.UseTexture(atlas, AccessFlags.Read);
        }

        internal void Bind(Material material)
        {
            bool enabled = atlas.IsValid();
            CoreUtils.SetKeyword(material, "FOG_CAMERA_ATMOSPHERE", enabled);
            if (!enabled) return;
            material.SetTexture(atlasId, atlas);
            material.SetVector(layoutId, layout);
            material.SetInt(premultipliedId, premultiplied ? 1 : 0);
        }
    }
}
