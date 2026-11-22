using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain.Demo
{
    /// <summary>Reads the feature from a pipeline assigned by older laboratory scenes.</summary>
    [AddComponentMenu("")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "jlinkdev.UnityUtilities.VolumetricRain.Demo", "jlinkdev.VolumetricRain", null)]
    public sealed class RainDemoPipeline : RainCameraPass
    {
        [SerializeField] private UniversalRenderPipelineAsset pipeline;
        public UniversalRenderPipelineAsset Pipeline { get => pipeline; set => pipeline = value; }

        protected override void OnEnable()
        {
            if (!feature && pipeline)
                foreach (var renderer in pipeline.rendererDataList)
                {
                    if (!renderer) continue;
                    feature = renderer.rendererFeatures.Find(item => item is RainRendererFeature) as RainRendererFeature;
                    if (feature) break;
                }
            base.OnEnable();
        }
    }
}
