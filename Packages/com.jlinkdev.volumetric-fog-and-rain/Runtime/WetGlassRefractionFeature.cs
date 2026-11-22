using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain
{
    /// <summary>Captures scene color and coverage for the Wet Glass shader before transparent panes draw.</summary>
    [CreateAssetMenu(menuName = "jlinkdev/Volumetric Fog and Rain/Wet Glass Capture")]
    public class WetGlassRefractionFeature : ScriptableRendererFeature
    {
        public const int GlassQueue = 3010;
        private CapturePass capture;
        private TransparentPass background, foreground;

        // A renderer that owns the entire transparent queue can bracket the capture with
        // these draws. Ordinary URP renderers keep their built-in pass and draw each object once.
        protected virtual bool DrawTransparentQueues => false;
        protected virtual bool ShouldCapture(Camera camera) => true;

        public override void Create()
        {
            int transparentEvent = (int)RenderPassEvent.BeforeRenderingTransparents;
            // In ordinary URP, features enqueue before the built-in transparent draw. A
            // preceding BeforeRenderingTransparents fog feature is included in the capture.
            capture = new CapturePass((RenderPassEvent)(DrawTransparentQueues ? transparentEvent - 1 : transparentEvent));
            background = new TransparentPass("Transparency before wet glass", transparentEvent - 2, 2501, GlassQueue - 1);
            foreground = new TransparentPass("Wet glass and foreground transparency", transparentEvent, GlassQueue, 5000);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            bool split = DrawTransparentQueues;
            if (split) renderer.EnqueuePass(background);
            if (!camera.TryGetComponent<WetGlassCameraPass>(out var local) || !local.HandlesCamera)
                Enqueue(renderer, camera);
            if (split) renderer.EnqueuePass(foreground);
        }

        // Camera-local features request just the capture. Queue ownership belongs to the renderer.
        internal void Enqueue(ScriptableRenderer renderer, Camera camera)
        {
            if (ShouldCapture(camera)) renderer.EnqueuePass(capture);
        }

        private sealed class CapturePass : ScriptableRenderPass
        {
            private static readonly int SceneColor = Shader.PropertyToID("_JlinkWetGlassSceneColor");
            private static readonly int CaptureAvailable = Shader.PropertyToID("_JlinkWetGlassCaptureAvailable");
            private sealed class Data { public TextureHandle source; }

            public CapturePass(RenderPassEvent timing)
            {
                renderPassEvent = timing;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frame)
            {
                var resources = frame.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;
                var source = resources.activeColorTexture;
                var desc = graph.GetTextureDesc(source);
                desc.name = "Wet glass scene color";
                desc.msaaSamples = MSAASamples.None;
                desc.bindTextureMS = false;
                desc.depthBufferBits = DepthBits.None;
                desc.clearBuffer = false;
                var destination = graph.CreateTexture(desc);
                using var builder = graph.AddRasterRenderPass<Data>("Capture scene for wet glass", out var data);
                data.source = source;
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(destination, SceneColor);
                // Render Graph resets global textures to black after this camera finishes.
                builder.SetGlobalTextureAfterPass(graph.defaultResources.whiteTexture, CaptureAvailable);
                builder.SetRenderFunc((Data d, RasterGraphContext ctx) =>
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), 0, false));
            }
        }

        // For projects with the renderer's built-in transparent layer mask set to Nothing.
        private sealed class TransparentPass : ScriptableRenderPass
        {
            private static readonly List<ShaderTagId> Tags = new()
            {
                new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly"), new ShaderTagId("SRPDefaultUnlit")
            };
            private readonly string label;
            private readonly RenderQueueRange queues;
            private sealed class Data { public RendererListHandle list; }

            public TransparentPass(string label, int timing, int first, int last)
            {
                this.label = label;
                renderPassEvent = (RenderPassEvent)timing;
                queues = new RenderQueueRange(first, last);
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frame)
            {
                var camera = frame.Get<UniversalCameraData>();
                var resources = frame.Get<UniversalResourceData>();
                var rendering = frame.Get<UniversalRenderingData>();
                var drawing = RenderingUtils.CreateDrawingSettings(Tags, rendering, camera,
                    frame.Get<UniversalLightData>(), SortingCriteria.CommonTransparent);
                var filtering = new FilteringSettings(queues, camera.camera.cullingMask);
                var parameters = new RendererListParams(rendering.cullResults, drawing, filtering);
                using var builder = graph.AddRasterRenderPass<Data>(label, out var data);
                data.list = graph.CreateRendererList(parameters);
                builder.UseRendererList(data.list);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
                builder.UseAllGlobalTextures(true);
                if (resources.mainShadowsTexture.IsValid()) builder.UseTexture(resources.mainShadowsTexture);
                if (resources.additionalShadowsTexture.IsValid()) builder.UseTexture(resources.additionalShadowsTexture);
                if (resources.ssaoTexture.IsValid()) builder.UseTexture(resources.ssaoTexture);
                builder.SetRenderFunc((Data d, RasterGraphContext ctx) => ctx.cmd.DrawRendererList(d.list));
            }
        }
    }
}
