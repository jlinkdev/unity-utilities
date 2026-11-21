using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.BlueprintRendering
{
    [DisallowMultipleRendererFeature("Blueprint Rendering")]
    public sealed class BlueprintRendererFeature : ScriptableRendererFeature
    {
        private Material capture;
        private Material composite;
        private DrawingPass pass;

        public override void Create()
        {
            Dispose(true);
            capture = CoreUtils.CreateEngineMaterial(Resources.Load<Shader>("JLinkBlueprint/Capture"));
            composite = CoreUtils.CreateEngineMaterial(Resources.Load<Shader>("JLinkBlueprint/Composite"));
            pass = new DrawingPass(capture, composite);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData;
            if (camera.renderType == CameraRenderType.Base && camera.camera.TryGetComponent<BlueprintCamera>(out var settings)
                && settings.isActiveAndEnabled)
                renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(capture);
            CoreUtils.Destroy(composite);
        }

        private sealed class DrawingPass : ScriptableRenderPass
        {
            private static readonly ShaderTagId[] ShaderTags =
            {
                new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("UniversalGBuffer"), new ShaderTagId("SRPDefaultUnlit")
            };
            private static readonly int Previous = Shader.PropertyToID("_JlbPrevious");
            private static readonly int[] LayerIds =
            {
                Shader.PropertyToID("_JlbLayer0"), Shader.PropertyToID("_JlbLayer1"),
                Shader.PropertyToID("_JlbLayer2"), Shader.PropertyToID("_JlbLayer3")
            };
            private readonly Material capture;
            private readonly Material composite;

            public DrawingPass(Material capture, Material composite)
            {
                this.capture = capture;
                this.composite = composite;
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            private sealed class CaptureData
            {
                public RendererListHandle geometry;
                public TextureHandle previous;
            }

            private sealed class CompositeData
            {
                public TextureHandle[] layers;
                public Material material;
                public Color paper, ink;
                public Vector4 style;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frame)
            {
                var camera = frame.Get<UniversalCameraData>().camera;
                var settings = camera.GetComponent<BlueprintCamera>();
                var culling = frame.Get<UniversalRenderingData>().cullResults;
                var target = frame.Get<UniversalResourceData>().activeColorTexture;
                var desc = graph.GetTextureDesc(target);
                desc.msaaSamples = MSAASamples.None;
                desc.bindTextureMS = false;
                desc.clearBuffer = true;
                desc.clearColor = Color.clear;
                desc.filterMode = FilterMode.Point;
                desc.format = GraphicsFormat.R32G32B32A32_SFloat;
                var surfaces = new TextureHandle[Mathf.Clamp(settings.surfaceLayers, 1, 4)];

                for (int i = 0; i < surfaces.Length; i++)
                {
                    desc.name = "Blueprint surface " + (i + 1);
                    surfaces[i] = graph.CreateTexture(desc);
                    var depthDesc = desc;
                    depthDesc.name = "Blueprint depth test";
                    depthDesc.depthBufferBits = DepthBits.Depth32;
                    var depth = graph.CreateTexture(depthDesc);
                    var list = new RendererListDesc(ShaderTags, culling, camera)
                    {
                        layerMask = settings.layers, renderQueueRange = RenderQueueRange.opaque,
                        sortingCriteria = SortingCriteria.CommonOpaque, overrideMaterial = capture,
                        overrideMaterialPassIndex = i == 0 ? 0 : 1
                    };
                    using var builder = graph.AddRasterRenderPass<CaptureData>(desc.name, out var data);
                    data.geometry = graph.CreateRendererList(list);
                    builder.UseRendererList(data.geometry);
                    if (i > 0)
                    {
                        data.previous = surfaces[i - 1];
                        builder.UseTexture(data.previous);
                    }
                    else data.previous = TextureHandle.nullHandle;
                    builder.SetRenderAttachment(surfaces[i], 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (CaptureData d, RasterGraphContext ctx) =>
                    {
                        if (d.previous.IsValid()) ctx.cmd.SetGlobalTexture(Previous, d.previous);
                        ctx.cmd.DrawRendererList(d.geometry);
                    });
                }

                using var draw = graph.AddRasterRenderPass<CompositeData>("Blueprint ink", out var ink);
                ink.layers = surfaces;
                ink.material = composite;
                bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
                ink.paper = linear ? settings.paperColor.linear : settings.paperColor;
                ink.ink = linear ? settings.inkColor.linear : settings.inkColor;
                ink.style = new Vector4(surfaces.Length, Mathf.Clamp01(settings.hiddenInk),
                    Mathf.Clamp(settings.lineWidth, 1, 3), Mathf.Cos(Mathf.Clamp(settings.creaseAngle, 1f, 89f) * Mathf.Deg2Rad));
                foreach (var surface in surfaces) draw.UseTexture(surface);
                draw.SetRenderAttachment(target, 0, AccessFlags.Write);
                draw.AllowGlobalStateModification(true);
                draw.SetRenderFunc(static (CompositeData d, RasterGraphContext ctx) =>
                {
                    for (int i = 0; i < LayerIds.Length; i++)
                        ctx.cmd.SetGlobalTexture(LayerIds[i], d.layers[Mathf.Min(i, d.layers.Length - 1)]);
                    ctx.cmd.SetGlobalVector("_JlbStyle", d.style);
                    ctx.cmd.SetGlobalVector("_JlbPaper", d.paper);
                    ctx.cmd.SetGlobalVector("_JlbInk", d.ink);
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0, MeshTopology.Triangles, 3, 1);
                });
            }
        }
    }
}
