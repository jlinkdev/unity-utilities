using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.ObjectHighlighting
{
    [DisallowMultipleRendererFeature("Object Highlighting")]
    public sealed class ObjectHighlightRendererFeature : ScriptableRendererFeature
    {
        public enum GlowResolution { Off = 0, Full = 1, Half = 2, Quarter = 4 }

        [Tooltip("Only the glow uses this resolution. The contour always uses the camera render resolution.")]
        public GlowResolution glowResolution = GlowResolution.Half;
        [Tooltip("Optional replacement for the included cyan hover contour.")]
        public HighlightStyle defaultHover;
        [Tooltip("Optional replacement for the included amber selection halo.")]
        public HighlightStyle defaultSelection;
        [Tooltip("Also draw active game highlights in the Scene view.")]
        public bool showInSceneView;

        private Material maskMaterial, filterMaterial;
        private HighlightPass pass;

        public override void Create()
        {
            Dispose(true);
            maskMaterial = CoreUtils.CreateEngineMaterial(Resources.Load<Shader>("JLinkHighlight/Mask"));
            filterMaterial = CoreUtils.CreateEngineMaterial(Resources.Load<Shader>("JLinkHighlight/Filter"));
            pass = new HighlightPass(this, maskMaterial, filterMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var data = renderingData.cameraData;
            if (ObjectHighlight.Active.Count == 0 || data.renderType != CameraRenderType.Base ||
                data.xrRendering || data.camera.stereoEnabled) return;
            if (data.cameraType != CameraType.Game && !(showInSceneView && data.isSceneViewCamera)) return;
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(maskMaterial);
            CoreUtils.Destroy(filterMaterial);
        }

        private sealed class HighlightPass : ScriptableRenderPass
        {
            private readonly ObjectHighlightRendererFeature settings;
            private readonly Material maskMaterial, filterMaterial;
            private readonly HighlightStyle hover, selection;
            private readonly Plane[] planes = new Plane[6];
            private readonly List<Material> slots = new List<Material>();
            private readonly List<Group> groups = new List<Group>();
            private int groupCount;

            private struct DrawItem
            {
                public Renderer renderer;
                public int submesh;
            }

            private sealed class Group
            {
                public HighlightStyle style;
                public TextureHandle mask;
                public readonly List<DrawItem> items = new List<DrawItem>();
            }

            public HighlightPass(ObjectHighlightRendererFeature settings, Material maskMaterial, Material filterMaterial)
            {
                this.settings = settings;
                this.maskMaterial = maskMaterial;
                this.filterMaterial = filterMaterial;
                hover = Resources.Load<HighlightStyle>("JLinkHighlight/Hover");
                selection = Resources.Load<HighlightStyle>("JLinkHighlight/Selected");
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                // The capture binds the camera's depth alongside an offscreen color mask.
                requiresIntermediateTexture = true;
            }

            private Group GetGroup(HighlightStyle style)
            {
                for (int i = 0; i < groupCount; i++)
                    if (groups[i].style == style) return groups[i];
                if (groupCount == groups.Count) groups.Add(new Group());
                var group = groups[groupCount++];
                group.style = style;
                return group;
            }

            private void Gather(Camera camera)
            {
                for (int i = 0; i < groupCount; i++) groups[i].items.Clear();
                groupCount = 0;
                GeometryUtility.CalculateFrustumPlanes(camera, planes);
                var active = ObjectHighlight.Active;
                for (int i = 0; i < active.Count; i++)
                {
                    var target = active[i];
                    var style = target.IsSelected ? target.selectedStyle : target.hoverStyle;
                    if (!style) style = target.IsSelected ? settings.defaultSelection : settings.defaultHover;
                    if (!style) style = target.IsSelected ? selection : hover;
                    if (!style || (style.Width == 0 && (style.Radius == 0 || settings.glowResolution == GlowResolution.Off))) continue;
                    Group group = null;
                    foreach (var renderer in target.GetRenderers())
                    {
                        if (!renderer || !renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy ||
                            renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly ||
                            (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0 ||
                            !GeometryUtility.TestPlanesAABB(planes, renderer.bounds) ||
                            renderer.GetComponentInParent<ObjectHighlight>() != target) continue;
                        Mesh mesh;
                        if (renderer is SkinnedMeshRenderer skin) mesh = skin.sharedMesh;
                        else if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter)) mesh = filter.sharedMesh;
                        else continue;
                        if (!mesh) continue;
                        renderer.GetSharedMaterials(slots);
                        for (int s = 0; s < Mathf.Min(mesh.subMeshCount, slots.Count); s++)
                        {
                            if (!slots[s] || slots[s].renderQueue > (int)RenderQueue.GeometryLast) continue;
                            group ??= GetGroup(style);
                            group.items.Add(new DrawItem { renderer = renderer, submesh = s });
                        }
                    }
                }
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frame)
            {
                var cameraData = frame.Get<UniversalCameraData>();
                Gather(cameraData.camera);
                if (groupCount == 0) return;
                var resources = frame.Get<UniversalResourceData>();
                var depth = resources.activeDepthTexture;
                var capture = graph.GetTextureDesc(depth);
                capture.format = GraphicsFormat.R8_UNorm;
                capture.bindTextureMS = false;
                capture.memoryless = RenderTextureMemoryless.None;
                capture.clearBuffer = true;
                capture.clearColor = Color.clear;
                capture.filterMode = FilterMode.Bilinear;
                capture.name = "Highlight combined silhouette";
                var union = groupCount > 1 ? graph.CreateTexture(capture) : TextureHandle.nullHandle;
                capture.name = "Highlight style silhouette";
                for (int i = 0; i < groupCount; i++)
                {
                    var group = groups[i];
                    group.mask = graph.CreateTexture(capture);
                    using var builder = graph.AddRasterRenderPass<CaptureData>("Highlight silhouettes", out var data);
                    data.items = group.items;
                    data.material = maskMaterial;
                    builder.SetRenderAttachment(group.mask, 0);
                    if (union.IsValid()) builder.SetRenderAttachment(union, 1, AccessFlags.ReadWrite);
                    builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
                    builder.SetRenderFunc(static (CaptureData d, RasterGraphContext ctx) =>
                    {
                        foreach (var item in d.items)
                            ctx.cmd.DrawRenderer(item.renderer, d.material, item.submesh, 0);
                    });
                }

                if (!union.IsValid()) union = groups[0].mask;
                var descriptor = cameraData.cameraTargetDescriptor;
                int width = descriptor.width, height = descriptor.height;
                var full = new TextureDesc(width, height) { format = GraphicsFormat.R8_UNorm, filterMode = FilterMode.Bilinear };
                for (int i = 0; i < groupCount; i++)
                {
                    var group = groups[i];
                    var style = group.style;
                    TextureHandle distance = group.mask, glow = group.mask;
                    if (style.Width > 0)
                    {
                        full.name = "Highlight horizontal distance";
                        distance = graph.CreateTexture(full);
                        Filter(graph, "Highlight outline distance", 0, group.mask, distance, new Vector4(1f / width, 0, style.Width, 0));
                    }
                    int divisor = (int)settings.glowResolution;
                    bool blur = divisor > 0 && style.Radius > 0;
                    if (blur)
                    {
                        var reduced = new TextureDesc(Mathf.Max(1, (width + divisor - 1) / divisor),
                            Mathf.Max(1, (height + divisor - 1) / divisor)) { format = GraphicsFormat.R8_UNorm, filterMode = FilterMode.Bilinear };
                        var source = group.mask;
                        if (divisor > 1)
                        {
                            reduced.name = "Highlight glow coverage";
                            source = graph.CreateTexture(reduced);
                            Filter(graph, "Highlight glow downsample", 1, group.mask, source,
                                new Vector4(divisor / (4f * width), divisor / (4f * height), 0, 0));
                        }
                        reduced.name = "Highlight horizontal glow";
                        var horizontal = graph.CreateTexture(reduced);
                        reduced.name = "Highlight glow";
                        glow = graph.CreateTexture(reduced);
                        Filter(graph, "Highlight glow horizontal", 2, source, horizontal,
                            BlurParameters(1f / reduced.width, 0, style.Radius * reduced.width / width));
                        Filter(graph, "Highlight glow vertical", 2, horizontal, glow,
                            BlurParameters(0, 1f / reduced.height, style.Radius * reduced.height / height));
                    }

                    using var builder = graph.AddRasterRenderPass<CompositeData>("Highlight exterior composite", out var data);
                    data.mask = union;
                    data.distance = distance;
                    data.glow = glow;
                    data.material = filterMaterial;
                    data.parameters = new Vector4(0, 1f / height, style.Width, blur ? Mathf.Clamp(style.glowIntensity, 0, 4) : 0);
                    bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    data.outline = linear ? style.outlineColor.linear : style.outlineColor;
                    data.halo = linear ? style.glowColor.linear : style.glowColor;
                    builder.UseTexture(union);
                    if (!distance.Equals(union)) builder.UseTexture(distance);
                    if (!glow.Equals(union) && !glow.Equals(distance)) builder.UseTexture(glow);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (CompositeData d, RasterGraphContext ctx) =>
                    {
                        ctx.cmd.SetGlobalTexture(Source, d.mask);
                        ctx.cmd.SetGlobalTexture(Distance, d.distance);
                        ctx.cmd.SetGlobalTexture(Glow, d.glow);
                        ctx.cmd.SetGlobalVector(Parameters, d.parameters);
                        ctx.cmd.SetGlobalVector(Outline, d.outline);
                        ctx.cmd.SetGlobalVector(Halo, d.halo);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, d.material, 3, MeshTopology.Triangles, 3);
                    });
                }
            }

            private static readonly int Source = Shader.PropertyToID("_JlhSource");
            private static readonly int Distance = Shader.PropertyToID("_JlhDistance");
            private static readonly int Glow = Shader.PropertyToID("_JlhGlow");
            private static readonly int Parameters = Shader.PropertyToID("_JlhParameters");
            private static readonly int Outline = Shader.PropertyToID("_JlhOutline");
            private static readonly int Halo = Shader.PropertyToID("_JlhHalo");

            private sealed class CaptureData
            {
                public List<DrawItem> items;
                public Material material;
            }

            private sealed class CompositeData
            {
                public TextureHandle mask, distance, glow;
                public Material material;
                public Vector4 parameters, outline, halo;
            }

            private sealed class FilterData
            {
                public TextureHandle source;
                public Material material;
                public int pass;
                public Vector4 parameters;
            }

            private static Vector4 BlurParameters(float stepX, float stepY, float radius)
            {
                float sigma = Mathf.Max(0.5f, radius * 0.45f);
                return new Vector4(stepX, stepY, Mathf.Ceil(radius), Mathf.Exp(-0.5f / (sigma * sigma)));
            }

            private void Filter(RenderGraph graph, string name, int shaderPass, TextureHandle source, TextureHandle destination, Vector4 parameters)
            {
                using var builder = graph.AddRasterRenderPass<FilterData>(name, out var data);
                data.source = source;
                data.material = filterMaterial;
                data.pass = shaderPass;
                data.parameters = parameters;
                builder.UseTexture(source);
                builder.SetRenderAttachment(destination, 0, AccessFlags.WriteAll);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (FilterData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalTexture(Source, d.source);
                    ctx.cmd.SetGlobalVector(Parameters, d.parameters);
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3);
                });
            }
        }
    }
}
