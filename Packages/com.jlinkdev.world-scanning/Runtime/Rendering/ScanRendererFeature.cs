using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.WorldScanning.Rendering
{
    [DisallowMultipleRendererFeature("World Scan Renderer Feature")]
    public sealed class ScanRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public sealed class Settings
        {
            [Tooltip("The point in URP at which the scan composite runs. Before post-processing allows Bloom to affect emissive scans.")]
            public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;
            [Tooltip("Render scans in the Scene view while authoring.")]
            public bool sceneView = true;
            [Tooltip("Render scans in Preview cameras used by inspectors.")]
            public bool previewCameras;
            [Tooltip("Render scans in reflection cameras.")]
            public bool reflectionCameras;
            [Range(0f, 1f), Tooltip("Final opacity applied to all scan layers.")]
            public float opacity = 1f;
        }

        private sealed class ScanRenderPass : ScriptableRenderPass
        {
            private static readonly int OpacityId = Shader.PropertyToID("_WorldScanGlobalOpacity");
            private readonly Material material;

            public ScanRenderPass(Material material)
            {
                this.material = material;
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
                requiresIntermediateTexture = true;
            }

            public void Setup(float opacity)
            {
                material.SetFloat(OpacityId, opacity);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer)
                    return;

                TextureHandle source = resources.activeColorTexture;
                TextureDesc descriptor = renderGraph.GetTextureDesc(source);
                descriptor.name = "World Scan Composite";
                descriptor.clearBuffer = false;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = MSAASamples.None;
                TextureHandle destination = renderGraph.CreateTexture(descriptor);

                RenderGraphUtils.BlitMaterialParameters parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0);
                renderGraph.AddBlitPass(parameters, "World Scan Composite");
                resources.cameraColor = destination;
            }
        }

        [SerializeField] private Settings settings = new Settings();
        [SerializeField, HideInInspector] private Shader shader;

        private Material material;
        private ScanRenderPass pass;

        public Settings FeatureSettings => settings;

        public override void Create()
        {
            if (shader == null)
                shader = Shader.Find("Hidden/jlinkdev/World Scanning/Fullscreen Scan");
            CoreUtils.Destroy(material);
            material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;
            pass = material != null ? new ScanRenderPass(material) : null;
            if (pass != null)
                pass.renderPassEvent = settings.injectionPoint;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass == null || material == null || ScanSystem.ActiveCount == 0)
                return;

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.SceneView && !settings.sceneView)
                return;
            if (cameraType == CameraType.Preview && !settings.previewCameras)
                return;
            if (cameraType == CameraType.Reflection && !settings.reflectionCameras)
                return;

            ScanShaderBridge.UploadGlobals();
            pass.renderPassEvent = settings.injectionPoint;
            pass.Setup(settings.opacity);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
            pass = null;
        }
    }
}
