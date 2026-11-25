using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.ProceduralNebulae
{
    public sealed class NebulaRendererFeature : ScriptableRendererFeature
    {
        [Tooltip("Preview nebulae in the Scene view when Game cameras use a separate renderer override.")]
        public bool sceneViewOnly;

        NebulaPass pass;
        public override void Create() => pass = new NebulaPass();
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var data = renderingData.cameraData;
            if (data.camera.TryGetComponent<NebulaCameraPass>(out var local) && local.HandlesCamera) return;
            if (data.renderType == CameraRenderType.Overlay || data.xr.enabled) return;
            if (data.cameraType != CameraType.Game && data.cameraType != CameraType.SceneView) return;
            Enqueue(renderer, data.camera);
        }

        internal void Enqueue(ScriptableRenderer renderer, Camera camera)
        {
            if (sceneViewOnly && camera.cameraType != CameraType.SceneView) return;
            foreach (var volume in NebulaVolume.Active)
            {
                if (!volume || (camera.cullingMask & (1 << volume.gameObject.layer)) == 0) continue;
                if (!volume.PrepareForRendering()) continue;
                pass.volume = volume;
                renderer.EnqueuePass(pass);
                break; // One active volume per camera in v1.
            }
        }

        sealed class NebulaPass : ScriptableRenderPass
        {
            internal NebulaVolume volume;

            sealed class PassData
            {
                public Material material;
                public TextureHandle source, lowCloud, lowDepth;
                public Vector4 lowSize;
            }

            public NebulaPass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                ConfigureInput(ScriptableRenderPassInput.Depth);
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frame)
            {
                var resources = frame.Get<UniversalResourceData>();
                var camera = frame.Get<UniversalCameraData>();
                if (resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid()) return;
                Draw(graph,resources,camera,volume);
            }

            static void Draw(RenderGraph graph, UniversalResourceData resources, UniversalCameraData camera, NebulaVolume volume)
            {
                var source = resources.activeColorTexture;
                var desc = graph.GetTextureDesc(source);
                desc.name = "Nebula composite";
                desc.depthBufferBits = 0;
                desc.clearBuffer = false;
                var output = graph.CreateTexture(desc);
                TextureHandle cloud = default, depth = default;
                Vector4 lowSize = default;
                if (volume.halfResolution)
                {
                    int w = Mathf.Max(1,(camera.cameraTargetDescriptor.width+1)/2);
                    int h = Mathf.Max(1,(camera.cameraTargetDescriptor.height+1)/2);
                    lowSize = new Vector4(w,h,1f/w,1f/h);
                    var low = new TextureDesc(w,h) { name="Nebula radiance and transmission", colorFormat=GraphicsFormat.R16G16B16A16_SFloat, filterMode=FilterMode.Point };
                    cloud = graph.CreateTexture(low);
                    low.name = "Nebula sample depth";
                    low.colorFormat = GraphicsFormat.R32_SFloat;
                    depth = graph.CreateTexture(low);
                    using var builder = graph.AddRasterRenderPass<PassData>("Nebula half-resolution march",out var data);
                    data.source = source;
                    data.material = volume.Material;
                    builder.UseTexture(source);
                    builder.UseTexture(resources.cameraDepthTexture);
                    builder.SetRenderAttachment(cloud,0);
                    builder.SetRenderAttachment(depth,1);
                    builder.SetRenderFunc((PassData d,RasterGraphContext ctx) =>
                        Blitter.BlitTexture(ctx.cmd,d.source,new Vector4(1,1,0,0),d.material,1));
                }

                using (var builder = graph.AddRasterRenderPass<PassData>("Nebula composite",out var data))
                {
                    data.source = source;
                    data.material = volume.Material;
                    data.lowSize = lowSize;
                    data.lowCloud = cloud;
                    data.lowDepth = depth;
                    builder.UseTexture(source);
                    builder.UseTexture(resources.cameraDepthTexture);
                    if (cloud.IsValid()) { builder.UseTexture(cloud); builder.UseTexture(depth); }
                    builder.SetRenderAttachment(output,0);
                    builder.SetRenderFunc((PassData d,RasterGraphContext ctx) =>
                    {
                        int shaderPass = 0;
                        if (d.lowCloud.IsValid())
                        {
                            d.material.SetTexture("_NebulaLow",d.lowCloud);
                            d.material.SetTexture("_NebulaDepth",d.lowDepth);
                            d.material.SetVector("_LowSize",d.lowSize);
                            shaderPass = 2;
                        }
                        Blitter.BlitTexture(ctx.cmd,d.source,new Vector4(1,1,0,0),d.material,shaderPass);
                    });
                }
                resources.cameraColor = output;
            }
        }
    }
}
