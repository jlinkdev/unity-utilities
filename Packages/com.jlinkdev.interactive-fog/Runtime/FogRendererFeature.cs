using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.InteractiveFog
{
    /// <summary>Depth-clipped fog compositing for URP Render Graph, before transparent objects.</summary>
    public sealed class FogRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] Shader shader;
        public static bool EnableProfiling { get; set; }
        static readonly ProfilingSampler marchSampler = new("Interactive Fog Raymarch");
        static readonly ProfilingSampler resolveSampler = new("Interactive Fog Reconstruction");
        public static float ReconstructionGpuMilliseconds => resolveSampler.gpuElapsedTime;
        public static float RaymarchGpuMilliseconds => marchSampler.gpuElapsedTime;
        Material material;
        FogPass pass;
        public override void Create()
        {
            CoreUtils.Destroy(material);
            if (!shader) shader = Shader.Find("Hidden/jlinkdev/InteractiveFog");
            if (shader) material = CoreUtils.CreateEngineMaterial(shader);
            pass = new FogPass(material) { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!material || renderingData.cameraData.renderType == CameraRenderType.Overlay || renderingData.cameraData.xr.enabled) return;
            if (renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView) return;
            if (FogVolume.Active.Count > 0) renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing) { CoreUtils.Destroy(material); }
        sealed class FogPass : ScriptableRenderPass
        {
            readonly Material material;
            readonly Plane[] frustum = new Plane[6];
            sealed class Data { public TextureHandle source, fog, depth; public Material material; public FogVolume volume; public int shaderPass, width, height; }
            public FogPass(Material material) { this.material = material; ConfigureInput(ScriptableRenderPassInput.Depth); requiresIntermediateTexture = true; }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                marchSampler.enableRecording = EnableProfiling; resolveSampler.enableRecording = EnableProfiling;
                var resources = frameData.Get<UniversalResourceData>();
                var camera = frameData.Get<UniversalCameraData>().camera;
                if (resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid()) return;
                GeometryUtility.CalculateFrustumPlanes(camera,frustum);
                foreach (var volume in FogVolume.Active)
                {
                    if (!volume || !volume.IsReady || volume.ResolvedStyle.extinction <= 0 || (camera.cullingMask & (1 << volume.gameObject.layer)) == 0) continue;
                    if(!GeometryUtility.TestPlanesAABB(frustum,volume.WorldBounds)) continue;
                    var desc = graph.GetTextureDesc(resources.activeColorTexture);
                    desc.name = "Interactive Fog Composite"; desc.clearBuffer = false; desc.depthBufferBits = 0;
                    var output = graph.CreateTexture(desc);
                    int divisor=volume.renderResolution==FogRenderResolution.Half?2:volume.renderResolution==FogRenderResolution.Quarter?4:1;
                    TextureHandle lowFog=default, lowDepth=default;
                    int width=0,height=0;
                    if(divisor>1)
                    {
                        var cameraDesc=frameData.Get<UniversalCameraData>().cameraTargetDescriptor;
                        width=Mathf.Max(1,(cameraDesc.width+divisor-1)/divisor); height=Mathf.Max(1,(cameraDesc.height+divisor-1)/divisor);
                        var lowDesc=new TextureDesc(width,height) { name="Interactive Fog Low", colorFormat=GraphicsFormat.R16G16B16A16_SFloat, filterMode=FilterMode.Point, clearBuffer=false };
                        lowFog=graph.CreateTexture(lowDesc);
                        lowDesc.name="Interactive Fog Low Depth"; lowDesc.colorFormat=GraphicsFormat.R32_SFloat;
                        lowDepth=graph.CreateTexture(lowDesc);
                        using(var builder=graph.AddRasterRenderPass<Data>("Interactive Fog Integration",out var data,marchSampler))
                        {
                            data.source=resources.activeColorTexture; data.material=material; data.volume=volume;
                            builder.UseTexture(data.source,AccessFlags.Read); builder.UseTexture(resources.cameraDepthTexture,AccessFlags.Read);
                            builder.UseAllGlobalTextures(true); builder.SetRenderAttachment(lowFog,0,AccessFlags.Write); builder.SetRenderAttachment(lowDepth,1,AccessFlags.Write);
                            builder.SetRenderFunc((Data d,RasterGraphContext context)=>
                            {
                                if(!d.volume || !d.volume.IsReady) return;
                                d.volume.Apply(d.material);
                                Blitter.BlitTexture(context.cmd,d.source,new Vector4(1,1,0,0),d.material,1);
                            });
                        }
                    }
                    using (var builder = graph.AddRasterRenderPass<Data>("Interactive Ground Fog", out var data, divisor==1?marchSampler:resolveSampler))
                    {
                        data.source = resources.activeColorTexture; data.material = material; data.volume = volume;
                        data.shaderPass=divisor==1?0:2; data.fog=lowFog; data.depth=lowDepth; data.width=width; data.height=height;
                        if(divisor>1) { builder.UseTexture(lowFog,AccessFlags.Read); builder.UseTexture(lowDepth,AccessFlags.Read); }
                        builder.UseTexture(data.source, AccessFlags.Read); builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                        builder.UseAllGlobalTextures(true); builder.SetRenderAttachment(output, 0, AccessFlags.Write);
                        builder.SetRenderFunc((Data d, RasterGraphContext context) =>
                        {
                            if (!d.volume || !d.volume.IsReady) return;
                            d.volume.Apply(d.material);
                            if(d.shaderPass==2)
                            {
                                d.material.SetTexture("_FogLow",d.fog); d.material.SetTexture("_FogLowDepth",d.depth);
                                d.material.SetVector("_FogLowSize",new Vector4(d.width,d.height,1f/d.width,1f/d.height));
                            }
                            Blitter.BlitTexture(context.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, d.shaderPass);
                        });
                    }
                    resources.cameraColor = output;
                }
            }
        }
    }
}
