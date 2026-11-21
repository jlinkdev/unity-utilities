using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.WaterSystem
{
    /// <summary>Renders the displaced boundary and closes the water volume at each camera's near plane.</summary>
    [DisallowMultipleRendererFeature("Water System")]
    public sealed class WaterRendererFeature : ScriptableRendererFeature
    {
        [Tooltip("Show underwater effects and waterline in Scene view.")]
        public bool RenderSceneView = true;

        private Material boundaryMaterial;
        private Material compositeMaterial;
        private SurfaceInputPass surfaceInputPass;
        private WaterPass pass;
        private readonly Plane[] frustum = new Plane[6];

        public override void Create()
        {
            DisposeMaterials();
            Shader boundary = Resources.Load<Shader>("WaterBoundary");
            Shader composite = Resources.Load<Shader>("WaterComposite");
            if (boundary == null || composite == null) return;
            boundaryMaterial = CoreUtils.CreateEngineMaterial(boundary);
            compositeMaterial = CoreUtils.CreateEngineMaterial(composite);
            surfaceInputPass = new SurfaceInputPass();
            pass = new WaterPass(this, boundaryMaterial, compositeMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (pass == null || cameraData.renderType == CameraRenderType.Overlay || cameraData.xr.enabled) return;
            if (cameraData.cameraType != CameraType.Game && cameraData.cameraType != CameraType.SceneView) return;
            if (cameraData.isSceneViewCamera && !RenderSceneView) return;
            if (SelectBody(cameraData.camera) == null) return;
            renderer.EnqueuePass(surfaceInputPass);
            renderer.EnqueuePass(pass);
        }

        private WaterBody SelectBody(Camera camera)
        {
            var waterCamera = camera.GetComponent<WaterCamera>();
            if (waterCamera != null && waterCamera.isActiveAndEnabled && waterCamera.BodyOverride != null)
                return IsRenderable(waterCamera.BodyOverride, camera) ? waterCamera.BodyOverride : null;

            GeometryUtility.CalculateFrustumPlanes(camera.cullingMatrix, frustum);
            WaterBody selected = null;
            float nearest = float.PositiveInfinity;
            foreach (var body in WaterBody.ActiveBodies)
            {
                if (!IsRenderable(body, camera) || !GeometryUtility.TestPlanesAABB(frustum, body.WorldBounds)) continue;
                float distance = body.WorldBounds.SqrDistance(camera.transform.position);
                // Stable registry order breaks ties for overlapping volumes; an override resolves ambiguity.
                if (distance >= nearest) continue;
                nearest = distance;
                selected = body;
            }
            return selected;
        }

        private static bool IsRenderable(WaterBody body, Camera camera)
        {
            return body != null && body.isActiveAndEnabled && body.HasSupportedTransform && body.SurfaceMesh != null &&
                   body.SurfaceRenderer != null && body.SurfaceRenderer.enabled &&
                   !body.SurfaceRenderer.forceRenderingOff &&
                   (camera.cullingMask & (1 << body.gameObject.layer)) != 0;
        }

        protected override void Dispose(bool disposing) => DisposeMaterials();

        private void DisposeMaterials()
        {
            CoreUtils.Destroy(boundaryMaterial);
            CoreUtils.Destroy(compositeMaterial);
            boundaryMaterial = null;
            compositeMaterial = null;
            surfaceInputPass = null;
            pass = null;
        }

        private sealed class SurfaceInputPass : ScriptableRenderPass
        {
            public SurfaceInputPass()
            {
                // The MeshRenderer surface samples scene depth DURING transparent rendering. Declaring
                // this only on the later composite lets URP postpone its copy until after that surface,
                // making absorption/refraction/foam read an uninitialized fallback depth texture.
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color);
            }

            // Input declarations schedule URP's own copies; the ordinary transparent pass draws the mesh.
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData) { }
        }

        private sealed class WaterPass : ScriptableRenderPass
        {
            private static readonly int SourceId = Shader.PropertyToID("_WaterSource");
            private static readonly int SceneDepthId = Shader.PropertyToID("_WaterSceneDepth");
            private static readonly int BoundaryId = Shader.PropertyToID("_WaterBoundary");
            private static readonly int DebugId = Shader.PropertyToID("_WaterDebugMode");
            private readonly WaterRendererFeature owner;
            private readonly Material boundary;
            private readonly Material composite;

            private sealed class BoundaryData
            {
                public Mesh mesh;
                public Matrix4x4 matrix;
                public Material material;
                public readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
            }

            private sealed class CompositeData
            {
                public TextureHandle source, sceneDepth, boundary;
                public Material material;
                public readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
            }

            public WaterPass(WaterRendererFeature owner, Material boundary, Material composite)
            {
                this.owner = owner;
                this.boundary = boundary;
                this.composite = composite;
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                // Color ensures the surface can refract opaque geometry without changing the URP asset.
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color);
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                WaterBody body = owner.SelectBody(cameraData.camera);
                if (body == null || resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid()) return;

                TextureDesc descriptor = graph.GetTextureDesc(resources.activeColorTexture);
                descriptor.name = "Water Surface Boundary";
                descriptor.colorFormat = GraphicsFormat.R32_SFloat;
                descriptor.depthBufferBits = DepthBits.None;
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.bindTextureMS = false;
                descriptor.clearBuffer = true;
                descriptor.clearColor = Color.clear;
                descriptor.filterMode = FilterMode.Point;
                TextureHandle surfaceBoundary = graph.CreateTexture(descriptor);

                descriptor.name = "Water Boundary Depth";
                descriptor.colorFormat = GraphicsFormat.None;
                descriptor.depthBufferBits = DepthBits.Depth32;
                TextureHandle boundaryDepth = graph.CreateTexture(descriptor);

                using (var builder = graph.AddRasterRenderPass<BoundaryData>("Water displaced surface boundary", out var data))
                {
                    data.mesh = body.SurfaceMesh;
                    // Body geometry and CPU sampling share an explicitly translation-only coordinate system.
                    data.matrix = Matrix4x4.Translate(body.transform.position);
                    data.material = boundary;
                    data.properties.Clear();
                    body.ApplyShaderProperties(data.properties);
                    builder.SetRenderAttachment(surfaceBoundary, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(boundaryDepth, AccessFlags.Write);
                    builder.SetRenderFunc((BoundaryData d, RasterGraphContext context) =>
                        context.cmd.DrawMesh(d.mesh, d.matrix, d.material, 0, 0, d.properties));
                }

                descriptor = graph.GetTextureDesc(resources.activeColorTexture);
                descriptor.name = "Water Underwater Composite";
                descriptor.clearBuffer = false;
                descriptor.depthBufferBits = DepthBits.None;
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.bindTextureMS = false;
                TextureHandle destination = graph.CreateTexture(descriptor);
                using (var builder = graph.AddRasterRenderPass<CompositeData>("Water near-plane closure and underwater", out var data))
                {
                    data.material = composite;
                    data.source = resources.activeColorTexture;
                    data.sceneDepth = resources.cameraDepthTexture;
                    data.boundary = surfaceBoundary;
                    data.properties.Clear();
                    body.ApplyShaderProperties(data.properties);
                    var camera = cameraData.camera.GetComponent<WaterCamera>();
                    data.properties.SetInt(DebugId, camera != null && camera.isActiveAndEnabled ? (int)camera.DebugMode : 0);
                    builder.UseTexture(data.source, AccessFlags.Read);
                    builder.UseTexture(data.sceneDepth, AccessFlags.Read);
                    builder.UseTexture(data.boundary, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CompositeData d, RasterGraphContext context) =>
                    {
                        // TextureHandles resolve only during graph execution. Draw-local state avoids cross-camera globals.
                        d.properties.SetTexture(SourceId, d.source);
                        d.properties.SetTexture(SceneDepthId, d.sceneDepth);
                        d.properties.SetTexture(BoundaryId, d.boundary);
                        context.cmd.DrawProcedural(Matrix4x4.identity, d.material, 0, MeshTopology.Triangles, 3, 1, d.properties);
                    });
                }
                resources.cameraColor = destination;
            }
        }
    }
}
