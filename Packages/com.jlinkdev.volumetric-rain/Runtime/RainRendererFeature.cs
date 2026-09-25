using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.VolumetricRain
{
    [DisallowMultipleRendererFeature("Volumetric Rain")]
    public sealed class RainRendererFeature : ScriptableRendererFeature
    {
        [SerializeField, HideInInspector] private Shader shader;
        [Tooltip("Optional profile used only by Scene View cameras.")]
        public RainProfile sceneViewProfile;
        private Material material;
        private RainPass pass;
        private bool warnedVolumeLimit;
        private readonly Plane[] frustum = new Plane[6];

        public override void Create()
        {
            CoreUtils.Destroy(material);
            if (shader == null) shader = Shader.Find("Hidden/jlinkdev/Volumetric Rain");
            material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;
            pass = material != null ? new RainPass(material, this) : null;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (pass == null || cameraData.renderType == CameraRenderType.Overlay || cameraData.xr.enabled) return;
            if (cameraData.cameraType != CameraType.Game && cameraData.cameraType != CameraType.SceneView) return;
            var rain = cameraData.camera.GetComponent<VolumetricRainCamera>();
            var profile = cameraData.isSceneViewCamera ? sceneViewProfile :
                (rain != null && rain.isActiveAndEnabled ? rain.profile : null);
            if (profile == null || (!profile.HasRain && !profile.HasFog) || (!cameraData.isSceneViewCamera && rain.intensity <= 0)) return;
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
            pass = null;
        }

        private bool CollectVolumes(Camera camera, bool bounded, PassVolumeData data)
        {
            data.rainCount = data.dryCount = 0;
            GeometryUtility.CalculateFrustumPlanes(camera.cullingMatrix, frustum);
            foreach (var box in RainBoxVolume.Active)
            {
                if (box == null || !box.isActiveAndEnabled || (!bounded && !box.ExcludesRain)) continue;
                if ((camera.cullingMask & (1 << box.gameObject.layer)) == 0) continue;
                if (!GeometryUtility.TestPlanesAABB(frustum, box.WorldBounds)) continue;
                var matrix = box.BoxToWorld;
                if (Mathf.Abs(matrix.determinant) < 0.000000001f) continue;
                if (box.ExcludesRain)
                {
                    if (data.dryCount < 8) data.dryBoxes[data.dryCount] = PadExclusion(matrix.inverse);
                    data.dryCount++;
                }
                else
                {
                    if (data.rainCount < 4) data.rainBoxes[data.rainCount] = matrix.inverse;
                    data.rainCount++;
                }
            }
            bool overflow = data.rainCount > 4 || data.dryCount > 8;
            if (overflow && !warnedVolumeLimit)
                Debug.LogWarning("Volumetric Rain supports at most 4 visible rain boxes and 8 visible exclusion boxes per camera. Rain is skipped for this camera until the limit is respected, so dry regions are never silently ignored.", this);
            warnedVolumeLimit |= overflow;
            return !overflow && (!bounded || data.rainCount > 0);
        }

        private static Matrix4x4 PadExclusion(Matrix4x4 worldToBox)
        {
            // Expand each dry plane by 1 mm in world space, including nonuniform scale/shear.
            // This seals precision gaps against flush scene-depth surfaces. Bake the padding
            // into the inverse matrix per camera, avoiding extra math in every shaded pixel.
            const float padding = 0.001f;
            for (int axis = 0; axis < 3; axis++)
            {
                Vector4 row = worldToBox.GetRow(axis);
                float normalLength = new Vector3(row.x, row.y, row.z).magnitude;
                worldToBox.SetRow(axis, row / (1 + 2 * padding * normalLength));
            }
            return worldToBox;
        }
        private sealed class PassVolumeData
        {
            public readonly Matrix4x4[] rainBoxes = new Matrix4x4[4];
            public readonly Matrix4x4[] dryBoxes = new Matrix4x4[8];
            public int rainCount, dryCount;
        }
        private sealed class RainPass : ScriptableRenderPass
        {
            private readonly Material material;
            private readonly RainRendererFeature owner;
            private readonly PassVolumeData volumeScratch = new PassVolumeData();
            private sealed class PassData
            {
                public TextureHandle source;
                public Material material;
                public Vector4 field, distances, appearance, noise, offset, offsetCells, right, up, forward, color;
                public int steps, hazeSteps, debug, seed;
                public readonly PassVolumeData volumes = new PassVolumeData();
                public bool bounded, useVolumes, fogOnly, rainOnly;
                public Vector4 fogColor, fogAppearance;
            }

            public RainPass(Material material, RainRendererFeature owner)
            {
                this.material = material;
                this.owner = owner;
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                ConfigureInput(ScriptableRenderPassInput.Depth);
                requiresIntermediateTexture = true;
            }

            // Wrapping by an integer hash period prevents long-running animation from losing sub-cell precision.
            private static float WholeOffset(double value) => (float)(System.Math.Floor(value) - System.Math.Floor(value / 65536.0) * 65536.0);

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var camera = frameData.Get<UniversalCameraData>();
                var rain = camera.camera.GetComponent<VolumetricRainCamera>();
                var p = camera.isSceneViewCamera ? owner.sceneViewProfile :
                    (rain != null && rain.isActiveAndEnabled ? rain.profile : null);
                if (p == null || resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid()) return;

                bool bounded = p.extent == RainExtent.VolumesOnly;
                if (!owner.CollectVolumes(camera.camera, bounded, volumeScratch)) return;

                var desc = graph.GetTextureDesc(resources.activeColorTexture);
                desc.name = "Volumetric Rain Composite";
                desc.clearBuffer = false;
                desc.depthBufferBits = 0;
                desc.msaaSamples = MSAASamples.None;
                var destination = graph.CreateTexture(desc);
                using (var builder = graph.AddRasterRenderPass<PassData>("Volumetric Rain", out var data))
                {
                    data.fogOnly = p.renderMode == RainRenderMode.FogOnly;
                    data.rainOnly = p.renderMode == RainRenderMode.RainOnly;
                    float intensity = camera.isSceneViewCamera ? 1 : Mathf.Clamp01(rain.intensity);
                    bool separateFog = p.UsesIndependentFog;
                    data.fogColor = separateFog ? p.fogColor : p.rainColor;
                    data.fogAppearance = new Vector4(Mathf.Max(0, separateFog ? p.fogBrightness : p.brightness),
                        Mathf.Max(0, p.hazeExtinction), Mathf.Max(0, separateFog ? p.fogScattering : p.scattering),
                        (separateFog ? Mathf.Max(0, p.fogDensity) : Mathf.Clamp01(p.density)) * intensity);
                    data.bounded = bounded;
                    data.volumes.rainCount = volumeScratch.rainCount;
                    data.volumes.dryCount = volumeScratch.dryCount;
                    System.Array.Copy(volumeScratch.rainBoxes, data.volumes.rainBoxes, volumeScratch.rainCount);
                    System.Array.Copy(volumeScratch.dryBoxes, data.volumes.dryBoxes, volumeScratch.dryCount);
                    data.useVolumes = data.bounded || data.volumes.dryCount > 0;
                    data.source = resources.activeColorTexture;
                    data.material = material;
                    float cell = Mathf.Max(0.1f, p.cellSize);
                    Vector3 velocity = p.Velocity;
                    Vector3 up = velocity.sqrMagnitude > 0.000001f ? velocity.normalized : Vector3.down;
                    Vector3 right = Vector3.Cross(Mathf.Abs(up.y) > 0.95f ? Vector3.forward : Vector3.up, up).normalized;
                    Vector3 forward = Vector3.Cross(right, up);
                    double time = camera.isSceneViewCamera ? Time.realtimeSinceStartupAsDouble : rain.EvaluationTime;
                    data.right = right; data.up = up; data.forward = forward;
                    double x = Vector3.Dot(velocity, right) * time / cell; double y = Vector3.Dot(velocity, up) * time / (cell * 3); double z = Vector3.Dot(velocity, forward) * time / cell;
                    data.offsetCells = new Vector4(WholeOffset(x), WholeOffset(y), WholeOffset(z), 0);
                    data.offset = new Vector4((float)(x - System.Math.Floor(x)),
                        (float)(y - System.Math.Floor(y)),
                        (float)(z - System.Math.Floor(z)), 0);
                    data.field = new Vector4(cell, Mathf.Clamp(p.streakLength, 0.001f, cell * 1.5f),
                        Mathf.Clamp(p.streakWidth, 0.0001f, cell * 0.04f),
                        Mathf.Clamp01(p.density) * (camera.isSceneViewCamera ? 1 : Mathf.Clamp01(rain.intensity)));
                    float near = Mathf.Max(0, p.nearFade);
                    float mid = Mathf.Max(near + 0.1f, p.midDistance);
                    float far = Mathf.Max(mid + 0.1f, p.farDistance);
                    data.distances = new Vector4(near, mid, far, data.fogOnly ? Mathf.Max(1, p.maxDistance) : Mathf.Max(far, p.maxDistance));
                    data.appearance = new Vector4(Mathf.Max(0, p.brightness), Mathf.Max(0, p.streakOpacity),
                        Mathf.Max(0, p.hazeExtinction), Mathf.Max(0, p.scattering));
                    data.noise = new Vector4(Mathf.Max(0.001f, p.noiseScale), Mathf.Clamp01(p.noiseStrength), 0, 0);
                    data.color = p.rainColor;
                    data.steps = Mathf.Clamp(p.maxCellSteps, 16, 256);
                    data.hazeSteps = Mathf.Clamp(p.hazeSteps, 4, 32);
                    data.debug = camera.isSceneViewCamera ? 0 : (int)rain.debugView;
                    data.seed = p.seed;
                    builder.UseTexture(data.source, AccessFlags.Read);
                    builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                    builder.UseAllGlobalTextures(true);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc((PassData d, RasterGraphContext context) =>
                    {
                        // Set parameters at execution time: each camera gets its recorded snapshot.
                        var m = d.material;
                        CoreUtils.SetKeyword(m, "_RAIN_VOLUMES", d.useVolumes);
                        CoreUtils.SetKeyword(m, "_FOG_ONLY", d.fogOnly);
                        CoreUtils.SetKeyword(m, "_RAIN_ONLY", d.rainOnly);
                        m.SetVector("_FogColor", d.fogColor); m.SetVector("_FogAppearance", d.fogAppearance);
                        m.SetInt("_RainBounded", d.bounded ? 1 : 0);
                        m.SetInt("_RainBoxCount", d.volumes.rainCount);
                        m.SetInt("_RainDryBoxCount", d.volumes.dryCount);
                        if (d.useVolumes)
                        {
                            m.SetMatrixArray("_RainBoxes", d.volumes.rainBoxes);
                            m.SetMatrixArray("_RainDryBoxes", d.volumes.dryBoxes);
                        }
                        m.SetVector("_RainField", d.field); m.SetVector("_RainDistances", d.distances);
                        m.SetVector("_RainAppearance", d.appearance); m.SetVector("_RainNoise", d.noise);
                        m.SetVector("_RainOffset", d.offset); m.SetVector("_RainOffsetCells", d.offsetCells); m.SetVector("_RainRight", d.right);
                        m.SetVector("_RainUp", d.up); m.SetVector("_RainForward", d.forward);
                        m.SetVector("_RainColor", d.color);
                        m.SetInt("_RainSteps", d.steps); m.SetInt("_RainHazeSteps", d.hazeSteps);
                        m.SetInt("_RainDebug", d.debug); m.SetInt("_RainSeed", d.seed);
                        Blitter.BlitTexture(context.cmd, d.source, new Vector4(1, 1, 0, 0), m, 0);
                    });
                }
                resources.cameraColor = destination;
            }
        }
    }
}
