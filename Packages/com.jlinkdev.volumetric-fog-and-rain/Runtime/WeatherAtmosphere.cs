using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain
{
    /// <summary>Camera-local, depth-resolved atmospheric light for subsequent Render Graph passes.</summary>
    public sealed class WeatherAtmosphere : ContextItem
    {
        private TextureHandle atlas;
        private Vector4 layout;
        private Camera camera;
        private bool premultiplied;

        /// <summary>
        /// Obtain this camera's transient texture during Render Graph recording and declare it as a read.
        /// RGB stores accumulated in-scattering and A stores remaining transmission from the camera near plane.
        /// Layout is (tile width, tile height, maximum ray distance in metres, slice count).
        /// Slice i, packed at (i % 8, i / 8), represents maxDistance * (i / (sliceCount - 1))².
        /// </summary>
        [Preserve]
        public static bool TryGetAtmosphere(ContextContainer frameData, out TextureHandle atlas, out Vector4 layout, out bool premultiplied)
        {
            if (frameData.Contains<WeatherAtmosphere>())
            {
                var atmosphere = frameData.Get<WeatherAtmosphere>();
                if (atmosphere.atlas.IsValid() && atmosphere.camera == frameData.Get<UniversalCameraData>().camera)
                {
                    atlas = atmosphere.atlas;
                    layout = atmosphere.layout;
                    premultiplied = atmosphere.premultiplied;
                    return true;
                }
            }
            atlas = default;
            layout = default;
            premultiplied = false;
            return false;
        }

        internal static void Publish(ContextContainer frameData, Camera camera, TextureHandle atlas, Vector4 layout, bool premultiplied)
        {
            var atmosphere = frameData.GetOrCreate<WeatherAtmosphere>();
            atmosphere.camera = camera;
            atmosphere.atlas = atlas;
            atmosphere.layout = layout;
            atmosphere.premultiplied = premultiplied;
        }

        public override void Reset()
        {
            atlas = default;
            layout = default;
            camera = null;
            premultiplied = false;
        }
    }
}
