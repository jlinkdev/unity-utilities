using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace jlinkdev.UnityUtilities.InteractiveFog
{
    public enum FogSimulationBackend { Volume3D, Planar2D }
    public enum FogRenderResolution { Full, Half, Quarter }

    /// <summary>Owns a bounded, world-aligned GPU fluid. Transform position is the bottom center.</summary>
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(200)]
    public sealed class FogVolume : MonoBehaviour
    {
        internal static readonly List<FogVolume> Active = new();
        public ComputeShader simulation;
        [Tooltip("Optional shared style. When assigned, its appearance and motion override the inline style fields. Bounds, resolution, simulation rate, ray steps, pressure iterations and lights stay local.")]
        public FogStyleProfile style;
        public FogStyleSettings ResolvedStyle => style ? style.settings : FogStyleSettings.FromVolume(this);
        public Vector3 size = new(16, 2, 16);
        [Tooltip("Volume3D simulates independent vertical motion. Planar2D simulates horizontal columns and reconstructs volumetric shape.")]
        public FogSimulationBackend simulationBackend = FogSimulationBackend.Volume3D;
        [Tooltip("X/Z control horizontal simulation resolution. In Planar2D, Y controls only the cached shape and lighting height samples.")]
        public Vector3Int resolution = new(128, 24, 128);
        [Range(10, 60)] public int simulationRate = 30;
        [Range(8, 64)] public int pressureIterations = 24;
        [Range(0, 5)] public float vorticity = 1.8f;
        [Tooltip("Strength of flow-driven eddies. Zero disables added small-scale stirring.")]
        [Range(0, 2)] public float turbulence = .5f;
        [Range(.25f, 2)] public float eddySize = 1.2f;
        [Range(0, 3)] public float velocityDamping = .16f;
        public Vector3 wind = new(.08f, 0, .03f);
        [Range(.1f, 2)] public float fogHeight = .62f;
        [Tooltip("Separates Ground Mist into curling banks with thinner channels between them. Zero keeps a continuous layer.")]
        [Range(0, 1)] public float groundPatchiness;
        [Range(0, 2)] public float replenishment = .015f;
        [Tooltip("Rate per second at which quiet fog recovers its initialized folds after a wake fades. Adds/removes density; zero disables interior recovery. Changing Fog Height rebuilds the recovery target; Reset Simulation applies the new resting shape immediately.")]
        [Range(0, 2)] public float restRecovery = .25f;
        public FogDensityShape densityShape;
        [Range(.5f,3)] public float puffSize=1.2f;
        [Tooltip("Fills the spaces between Cumulus cloud puffs. Zero keeps separated clusters; one adds overlapping smaller billows.")]
        [Range(0,1)] public float cloudCoverage;
        [Range(0,1)] public float plumeLift;
        [FogDensity, Tooltip("Optical density: higher values make fog thicker and less transparent. Use Cloud Coverage to fill spaces between cloud puffs.")]
        public float extinction = 5.2f;
        public Color albedo = new(.78f, .79f, .78f);
        public Color ambient = new(.19f, .21f, .23f);
        [Range(0, .8f)] public float anisotropy = .25f;
        [Range(32, 192)] public int raySteps = 96;
        [Tooltip("Fog integration resolution. Half/Quarter use depth-aware reconstruction; scene rendering stays full resolution.")]
        public FogRenderResolution renderResolution = FogRenderResolution.Full;
        [Range(0, 1)] public float detail = .7f;
        [Tooltip("Spatial scale multiplier for rendered folds. Larger values produce broader curved bands; independent of the simulated Eddy Size.")]
        [Range(.5f, 3)] public float foldSize = 1.5f;
        [Tooltip("Render-only displacement of density contours in metres. Zero disables wispy edge shaping.")]
        [Range(0, .25f)] public float wispDisplacement = .12f;
        [Tooltip("Opacity of Cumulus's separate curled ribbons. Zero hides the ribbons; cloud bodies remain.")]
        [Range(0,2)] public float wispOpacity=1;
        public FogPuffSettings puffs=FogPuffSettings.Default;
        public Light sun;
        [Tooltip("Up to eight point lights contribute to the fog. Other light types are ignored.")]
        public Light[] localLights = System.Array.Empty<Light>();
        [Tooltip("Automatically advance in LateUpdate. Disable for deterministic capture or external scheduling.")]
        public bool automaticSimulation = true;
        public RenderTexture Density => planar != null ? planar.Density : density;
        public RenderTexture Velocity => planar != null ? planar.Velocity : velocity;
        public RenderTexture SolidMask => planar != null ? planar.Solid : solid;
        public RenderTexture DetailCoordinates => planar != null ? planar.Coordinates : coordinatesA;
        public int StepCount { get; private set; }
        public bool IsReady => Density != null && Density.IsCreated();
        public bool UsesProceduralPuffs => ResolvedStyle.densityShape==FogDensityShape.DryIce;
        RenderTexture proceduralSources;
        public RenderTexture PuffDensity => puffDensity;
        public Bounds WorldBounds => new(transform.position + Vector3.up * size.y * .5f, size);
        public long EstimatedTextureBytes => IsReady ? (long)allocated.x * allocated.y * allocated.z * (planar != null ? 4 : 104) + (planar?.TextureBytes ?? 0) + 512L*512*4 + CumulusTextureBytes + (puffDensity?(long)allocated.x*allocated.y*allocated.z*8:0) + (proceduralSources?(long)allocated.x*allocated.y*allocated.z*8:0) : 0;

        long CumulusTextureBytes
        {
            get
            {
                if(!cumulusShape) return 0;
                long bytes=0;
                for(int i=0;i<cumulusShape.mipmapCount;i++)
                    bytes+=(long)Mathf.Max(1,cumulusShape.width>>i)*Mathf.Max(1,cumulusShape.height>>i)*Mathf.Max(1,cumulusShape.volumeDepth>>i)*8;
                return bytes;
            }
        }
        const int MaxSources = 16;
        ComputeShader compute;
        RenderTexture velocity, velocityNext, density, densityNext, predicted, pressure, pressureNext, divergence, curl, solid, lighting, velocityPredicted, coordinatesA, coordinatesANext, coordinatesB, coordinatesBNext;
        RenderTexture restDensity;
        RenderTexture cumulusShape;
        RenderTexture foldPattern;
        RenderTexture puffDensity,puffPrevious;
        FogPlanarSimulation planar;
        FogSimulationBackend allocatedBackend;
        Vector3Int allocated;
        Vector3 allocatedSize;
        float accumulator;
        readonly Vector4[] positions = new Vector4[MaxSources], previous = new Vector4[MaxSources], velocities = new Vector4[MaxSources], shapes = new Vector4[MaxSources];
        readonly Matrix4x4[] obstacles = new Matrix4x4[MaxSources];
        readonly Vector4[] lightPositions = new Vector4[8], lightColors = new Vector4[8];
        readonly List<Gust> gusts = new();
        struct Gust { public Vector3 position, previous, velocity; public float radius, remaining; }
        bool warnedSources;
        readonly Dictionary<string,int> kernels = new();
        bool obstacleMaskDirty = true;
        int maskObstacleCount = -1;
        Vector3 maskOrigin;
        Vector4 restingShape;
        Vector4 RestingShapeKey
        {
            get
            {
                var style = ResolvedStyle;
                if (style.densityShape == FogDensityShape.Cumulus)
                    return new Vector4(style.fogHeight, (float)style.densityShape, Mathf.Clamp(style.puffSize, .5f, 3f), Mathf.Clamp01(style.cloudCoverage));

                float patchiness = style.densityShape == FogDensityShape.GroundMist ? Mathf.Clamp01(style.groundPatchiness) : 0f;
                return new Vector4(style.fogHeight, (float)style.densityShape, patchiness, 0f);
            }
        }
        bool referenceWork;
        bool profileNoDetail;

        void OnEnable() { if (!Active.Contains(this)) Active.Add(this); Initialize(); }
        void OnDisable() { Active.Remove(this); Release(); }
        void OnValidate()
        {
            size = Vector3.Max(size, Vector3.one * .25f);
            resolution = new Vector3Int(Mathf.Clamp(resolution.x, 16, 256), Mathf.Clamp(resolution.y, 8, 128), Mathf.Clamp(resolution.z, 16, 256));
        }
        void LateUpdate()
        {
            if (Application.isPlaying && automaticSimulation) Advance(Mathf.Min(Time.deltaTime, .1f));
            else if (!Application.isPlaying && simulation)
            {
                if(!IsReady || allocated != resolution || allocatedSize != size || allocatedBackend != simulationBackend) Initialize();
                else if(restingShape != RestingShapeKey) ResetSimulation();
            }
        }
        /// <summary>Inject a local force over a finite duration, in world units. Zero velocity is a radial blast.</summary>
        public void AddGust(Vector3 worldPosition, Vector3 velocityMetresPerSecond, float radius = 1.5f, float duration = .18f)
        {
            if (gusts.Count >= MaxSources) gusts.RemoveAt(0);
            gusts.Add(new Gust { position = worldPosition, previous = worldPosition, velocity = Vector3.ClampMagnitude(velocityMetresPerSecond, 20), radius = Mathf.Max(.05f, radius), remaining = Mathf.Clamp(duration, .01f, 3) });
        }
        /// <summary>Inject a swept spherical force, suitable for a blade or moving prop. Does not remove density.</summary>
        public void AddSweep(Vector3 from, Vector3 to, Vector3 velocityMetresPerSecond, float radius = .3f, float duration = .06f)
        {
            AddGust(to, velocityMetresPerSecond, radius, duration);
            var gust = gusts[gusts.Count - 1]; gust.previous = from; gusts[gusts.Count - 1] = gust;
        }
        public void Advance(float seconds)
        {
            if (!IsReady || allocated != resolution || allocatedSize != size || allocatedBackend != simulationBackend) Initialize();
            if (!IsReady || seconds <= 0 || !float.IsFinite(seconds)) return;
            EnsurePuffs();
            // A style swap rebuilds only the quiet recovery target, preserving current wakes.
            if (restingShape != RestingShapeKey)
            {
                SetCommon(0); RebuildRestingVolume(); planar?.RebuildRest(restDensity); if(planar != null) obstacleMaskDirty=true; restingShape=RestingShapeKey;
            }
            float dt = 1f / Mathf.Clamp(simulationRate, 10, 60);
            accumulator = Mathf.Min(accumulator + seconds, dt * 3);
            while (accumulator >= dt - .000001f) { Step(dt); accumulator -= dt; }
        }
        public void ResetSimulation()
        {
            if (!IsReady || allocated != resolution || allocatedSize != size || allocatedBackend != simulationBackend) { Initialize(); return; }
            accumulator = 0; StepCount = 0; gusts.Clear(); ReleasePuffs(); EnsurePuffs(); SetCommon(0);
            if(planar != null)
            {
                RebuildRestingVolume(); planar.Reset(restDensity); restingShape=RestingShapeKey; obstacleMaskDirty=false; InitializeProceduralPuffs(); UpdateLighting(); return;
            }
            Dispatch("Initialize"); RebuildCumulusShape();
            restingShape=RestingShapeKey; obstacleMaskDirty=false;
            Swap(ref density, ref densityNext); Swap(ref velocity, ref velocityNext); Swap(ref pressure, ref pressureNext);
            Swap(ref coordinatesA, ref coordinatesANext); Swap(ref coordinatesB, ref coordinatesBNext);
            Graphics.CopyTexture(density, densityNext);
            Graphics.CopyTexture(coordinatesA, coordinatesANext); Graphics.CopyTexture(coordinatesB, coordinatesBNext); InitializeProceduralPuffs(); UpdateLighting();
        }
        public void Initialize()
        {
            Release(); OnValidate();
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            profileNoDetail=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--fog-profile-no-detail")>=0;
            referenceWork=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--fog-reference-work")>=0;
            #endif
            if (!simulation || !SystemInfo.supportsComputeShaders || !SystemInfo.supports3DRenderTextures) return;
            if (!SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.LoadStore) ||
                !SystemInfo.IsFormatSupported(GraphicsFormat.R32_SFloat, GraphicsFormatUsage.LoadStore) ||
                !SystemInfo.IsFormatSupported(GraphicsFormat.R16G16_SFloat, GraphicsFormatUsage.LoadStore) ||
                (simulationBackend==FogSimulationBackend.Planar2D && !SystemInfo.IsFormatSupported(GraphicsFormat.R32G32_SFloat, GraphicsFormatUsage.LoadStore)))
            { Debug.LogError("Interactive Fog requires 3D float UAV textures.", this); return; }
            compute = Instantiate(simulation); compute.hideFlags = HideFlags.HideAndDontSave;
            allocated = resolution; allocatedSize = size; allocatedBackend=simulationBackend;
            if(simulationBackend==FogSimulationBackend.Planar2D)
            {
                restDensity=Texture("Resting shape",GraphicsFormat.R32_SFloat);
                planar=new FogPlanarSimulation(compute,allocated.x,allocated.z,allocated.y);
                BuildPattern(); ResetSimulation(); return;
            }
            velocity = Texture("Velocity", GraphicsFormat.R16G16B16A16_SFloat);
            velocityNext = Texture("Velocity next", GraphicsFormat.R16G16B16A16_SFloat);
            curl = Texture("Vorticity", GraphicsFormat.R16G16B16A16_SFloat);
            density = Texture("Density", GraphicsFormat.R32_SFloat); densityNext = Texture("Density next", GraphicsFormat.R32_SFloat);
            restDensity = Texture("Resting density", GraphicsFormat.R32_SFloat);
            predicted = Texture("Density predictor", GraphicsFormat.R32_SFloat);
            pressure = Texture("Pressure", GraphicsFormat.R32_SFloat); pressureNext = Texture("Pressure next", GraphicsFormat.R32_SFloat);
            divergence = Texture("Divergence", GraphicsFormat.R32_SFloat); solid = Texture("Solid mask", GraphicsFormat.R32_SFloat);
            velocityPredicted = Texture("Velocity predictor", GraphicsFormat.R16G16B16A16_SFloat);
            coordinatesA = Texture("Detail A", GraphicsFormat.R16G16B16A16_SFloat); coordinatesANext = Texture("Detail A next", GraphicsFormat.R16G16B16A16_SFloat);
            coordinatesB = Texture("Detail B", GraphicsFormat.R16G16B16A16_SFloat); coordinatesBNext = Texture("Detail B next", GraphicsFormat.R16G16B16A16_SFloat);
            lighting = Texture("Lighting", GraphicsFormat.R16G16B16A16_SFloat);
            BuildPattern(); ResetSimulation();
        }
        void BuildPattern()
        {
            foldPattern = new RenderTexture(512,512,0) { name="Interactive Fog layered folds", graphicsFormat=GraphicsFormat.R16G16_SFloat,
                enableRandomWrite=true, wrapMode=TextureWrapMode.Repeat, filterMode=FilterMode.Bilinear, hideFlags=HideFlags.HideAndDontSave };
            foldPattern.Create();
            int patternKernel=compute.FindKernel("BuildFoldPattern");
            compute.SetTexture(patternKernel,"_FoldPatternOut",foldPattern); compute.Dispatch(patternKernel,64,64,1);
        }
        void ReleaseCumulusShape()
        {
            if(!cumulusShape) return;
            cumulusShape.Release();
            if(Application.isPlaying) Destroy(cumulusShape); else DestroyImmediate(cumulusShape);
            cumulusShape=null;
        }
        void EnsurePuffs()
        {
            if(!compute) return;
            if(ResolvedStyle.densityShape!=FogDensityShape.DryIce) { ReleasePuffs(); return; }
            if(puffDensity && puffPrevious && proceduralSources) return;
            ReleasePuffs();
            puffDensity=Texture("Billows",GraphicsFormat.R32_SFloat); puffPrevious=Texture("Billows previous",GraphicsFormat.R32_SFloat);
            proceduralSources=Texture("Procedural billow sources",GraphicsFormat.R16G16B16A16_SFloat);
            // Clear UAVs through compute writes before simulation/readback can reuse them.
            // Per-slice render-target clears can leave recycled 3D UAV contents visible.
            int clear=compute.FindKernel("ClearPuffs");
            compute.SetInts("_Resolution",allocated.x,allocated.y,allocated.z);
            foreach(var texture in new[]{puffDensity,puffPrevious})
            {
                compute.SetTexture(clear,"_PuffDensityOut",texture);
                compute.Dispatch(clear,(allocated.x+7)/8,(allocated.y+3)/4,(allocated.z+7)/8);
            }
        }
        void ReleasePuffs()
        {
            if(!puffDensity && !puffPrevious && !proceduralSources) return;
            foreach(var texture in new[]{puffDensity,puffPrevious,proceduralSources}) if(texture)
            { texture.Release(); if(Application.isPlaying) Destroy(texture); else DestroyImmediate(texture); }
            puffDensity=puffPrevious=proceduralSources=null;
        }
        void BuildPuffs()
        {
            if(!puffDensity) return;
            Swap(ref puffDensity,ref puffPrevious);
            int k=compute.FindKernel("BuildProceduralPuffs");
            compute.SetTexture(k,"_ProceduralSourcesOut",proceduralSources);
            compute.SetTexture(k,"_PuffDensityOut",puffDensity);
            compute.SetTexture(k,"_CoordinatesA",coordinatesA?coordinatesA:restDensity);
            compute.SetTexture(k,"_PCoordA",planar!=null?planar.Coordinates:foldPattern);
            compute.SetInt("_PuffPlanar",planar!=null?1:0);
            compute.Dispatch(k,(allocated.x+7)/8,(allocated.y+3)/4,(allocated.z+7)/8);
        }
        void InitializeProceduralPuffs()
        {
            if(!UsesProceduralPuffs) return;
            BuildPuffs(); Graphics.CopyTexture(puffDensity,puffPrevious);
        }
        void RebuildCumulusShape()
        {
            if(ResolvedStyle.densityShape!=FogDensityShape.Cumulus) { ReleaseCumulusShape(); return; }
            var r=new Vector3Int(Mathf.Clamp(allocated.x*2,64,256),Mathf.Clamp(allocated.y*2,32,96),Mathf.Clamp(allocated.z*2,64,256));
            if(!cumulusShape)
            {
                cumulusShape=new RenderTexture(r.x,r.y,0) { name="Interactive Fog cumulus detail", volumeDepth=r.z, useMipMap=true, autoGenerateMips=false,
                    dimension=TextureDimension.Tex3D, graphicsFormat=GraphicsFormat.R16G16B16A16_SFloat,
                    enableRandomWrite=true, wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear,
                    hideFlags=HideFlags.HideAndDontSave };
                cumulusShape.Create();
            }
            int k=compute.FindKernel("BuildCumulusShape");
            compute.SetInts("_CumulusDetailResolution",r.x,r.y,r.z);
            compute.SetTexture(k,"_CumulusShapeOut",cumulusShape);
            compute.SetTexture(k,"_CumulusRestProxy",restDensity);
            compute.Dispatch(k,(r.x+7)/8,(r.y+3)/4,(r.z+7)/8);
            cumulusShape.GenerateMips();
        }
        void RebuildRestingVolume()
        {
            int k=compute.FindKernel("BuildRestingDensity");
            compute.SetTexture(k,"_RestDensity",restDensity); compute.SetTexture(k,"_FoldPattern",foldPattern);
            compute.Dispatch(k,(allocated.x+7)/8,(allocated.y+3)/4,(allocated.z+7)/8);
            RebuildCumulusShape();
        }
        RenderTexture Texture(string label, GraphicsFormat format)
        {
            var texture = new RenderTexture(allocated.x, allocated.y, 0) { name = "Interactive Fog " + label, dimension = TextureDimension.Tex3D,
                volumeDepth = allocated.z, graphicsFormat = format, enableRandomWrite = true, wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            texture.Create(); return texture;
        }
        void SetCommon(float dt)
        {
            var settings=ResolvedStyle;
            var puff=settings.puffs;
            compute.SetInt("_DryIceEnabled",UsesProceduralPuffs?1:0);
            float minimum=Mathf.Clamp(Mathf.Min(puff.radiusRange.x,puff.radiusRange.y),.1f,2);
            float maximum=Mathf.Clamp(Mathf.Max(puff.radiusRange.x,puff.radiusRange.y),.1f,2);
            compute.SetVector("_ProceduralLayout",new Vector4(Mathf.Max(.2f,puff.spacing,maximum*1.7f),Mathf.Clamp01(puff.coverage),minimum,maximum));
            compute.SetVector("_ProceduralTimes",new Vector4(Mathf.Max(.1f,Mathf.Min(puff.growthRange.x,puff.growthRange.y)),Mathf.Max(.1f,Mathf.Max(puff.growthRange.x,puff.growthRange.y)),Mathf.Max(.1f,Mathf.Min(puff.settlingRange.x,puff.settlingRange.y)),Mathf.Max(.1f,Mathf.Max(puff.settlingRange.x,puff.settlingRange.y))));
            compute.SetVector("_ProceduralQuiet",new Vector4(Mathf.Max(0,Mathf.Min(puff.quietRange.x,puff.quietRange.y)),Mathf.Max(0,Mathf.Max(puff.quietRange.x,puff.quietRange.y)),(uint)puff.fieldSeed%4093,0));
            compute.SetVector("_ProceduralAmounts",new Vector4(Mathf.Max(0,puff.density),Mathf.Max(0,puff.mistAmount),Mathf.Max(0,puff.spreadSpeed),0));
            compute.SetInt("_ReferenceWork",referenceWork?1:0);
            compute.SetInts("_Resolution", allocated.x, allocated.y, allocated.z);
            compute.SetVector("_Size", size); compute.SetVector("_Origin", WorldBounds.min);
            compute.SetVector("_Cell", new Vector3(size.x / allocated.x, size.y / allocated.y, size.z / allocated.z));
            compute.SetFloat("_Dt", dt); compute.SetFloat("_SimTime", StepCount / (float)Mathf.Max(1, simulationRate)); compute.SetVector("_Wind", settings.wind);
            compute.SetVector("_Fluid", new Vector4(settings.vorticity, settings.velocityDamping, settings.fogHeight, settings.replenishment));
            compute.SetVector("_Cumulus", new Vector4((float)settings.densityShape,Mathf.Clamp(settings.puffSize,.5f,3),settings.plumeLift,Mathf.Max(0,settings.wispOpacity)));
            compute.SetFloat("_RestRecovery", Mathf.Max(0, settings.restRecovery));
            compute.SetFloat("_CloudCoverage",Mathf.Clamp01(settings.cloudCoverage));
            compute.SetFloat("_GroundPatchiness", Mathf.Clamp01(settings.groundPatchiness));
            compute.SetVector("_Turbulence", new Vector4(settings.turbulence, Mathf.Max(.25f,settings.eddySize), 0, 0));
            int period = Mathf.Max(1,simulationRate)*6;
            compute.SetInts("_ResetCoordinates", (StepCount+1)%period==0?1:0, (StepCount+1+period/2)%period==0?1:0);
            int count = Mathf.Min(MaxSources, FogObstacle.Active.Count);
            if(count!=maskObstacleCount || !maskOrigin.Equals(WorldBounds.min)) obstacleMaskDirty=true;
            for (int i = 0; i < count; i++)
            {
                var matrix=FogObstacle.Active[i].WorldToBox;
                if(!obstacles[i].Equals(matrix)) obstacleMaskDirty=true;
                obstacles[i]=matrix;
            }
            maskObstacleCount=count; maskOrigin=WorldBounds.min;
            compute.SetInt("_ObstacleCount", count); compute.SetMatrixArray("_Obstacles", obstacles);
            count = 0;
            foreach (var actor in FogInteractor.Active)
            {
                if (count == MaxSources) break;
                positions[count] = actor.transform.position; previous[count] = actor.Previous;
                velocities[count] = actor.Velocity;
                shapes[count++] = new Vector4(actor.radius, actor.height, actor.drag, actor.clearing);
            }
            foreach (var gust in gusts)
            {
                if (count == MaxSources) break;
                positions[count] = gust.position; previous[count] = gust.previous; velocities[count] = gust.velocity;
                shapes[count++] = new Vector4(gust.radius, 0, 5, -1);
            }
            if (!warnedSources && (FogInteractor.Active.Count + gusts.Count > MaxSources || FogObstacle.Active.Count > MaxSources))
            { Debug.LogWarning("Interactive Fog supports 16 interactors/gusts and 16 obstacles per volume. Extra entries are ignored.", this); warnedSources = true; }
            compute.SetInt("_SourceCount", count); compute.SetVectorArray("_Sources", positions); compute.SetVectorArray("_Previous", previous);
            compute.SetVectorArray("_Velocities", velocities); compute.SetVectorArray("_Shapes", shapes);
        }
        void Step(float dt)
        {
            SetCommon(dt);
            if(UsesProceduralPuffs) BuildPuffs();
            if(planar!=null) planar.ProceduralSources=proceduralSources;
            if(planar != null) { planar.Step(pressureIterations,obstacleMaskDirty); obstacleMaskDirty=false; }
            else
            {
                if(obstacleMaskDirty || referenceWork) { Dispatch("BuildObstacles"); obstacleMaskDirty=false; }
                Dispatch("AdvectVelocity"); Dispatch("CorrectVelocity"); Swap(ref velocity, ref velocityNext);
                Dispatch("Curl"); Dispatch("Forces"); Swap(ref velocity, ref velocityNext);
                Dispatch("Divergence"); Swap(ref pressure, ref pressureNext);
                for (int i = 0; i < pressureIterations; i++) { Dispatch("Pressure"); Swap(ref pressure, ref pressureNext); }
                Dispatch("Project"); Swap(ref velocity, ref velocityNext);
                Dispatch("BuildDensityFlux"); Dispatch("PredictDensity"); Dispatch("CorrectDensity"); Swap(ref density, ref densityNext);
                Dispatch("AdvectCoordinates"); Swap(ref coordinatesA, ref coordinatesANext); Swap(ref coordinatesB, ref coordinatesBNext);
            }
            StepCount++; UpdateLighting();
            for (int i = gusts.Count - 1; i >= 0; i--) { var g = gusts[i]; g.remaining -= dt; if (g.remaining <= 0) gusts.RemoveAt(i); else gusts[i] = g; }
        }
        void UpdateLighting()
        {
            // Lighting samples the newly advanced coordinate maps, not the preceding tick.
            compute.SetFloat("_SimTime",StepCount/(float)Mathf.Max(1,simulationRate));
            Vector3 direction = sun ? -sun.transform.forward : new Vector3(-.4f, .8f, -.3f).normalized;
            compute.SetVector("_LightDirection", direction); compute.SetFloat("_Extinction", ResolvedStyle.extinction);
            int count=0;
            if (localLights != null) foreach (var light in localLights)
            {
                if (!light || !light.isActiveAndEnabled || light.type != LightType.Point || count == 8) continue;
                var p=light.transform.position; lightPositions[count]=new Vector4(p.x,p.y,p.z,light.range);
                lightColors[count++]=light.color.linear*light.intensity;
            }
            compute.SetInt("_LocalLightCount",count); compute.SetVectorArray("_LocalLightPositions",lightPositions); compute.SetVectorArray("_LocalLightColors",lightColors);
            if(planar != null) planar.Light(cumulusShape,puffDensity); else Dispatch("LightVolume");
        }
        void Dispatch(string kernel)
        {
            int k;
            if(referenceWork) k=compute.FindKernel(kernel);
            else if(!kernels.TryGetValue(kernel,out k)) { k=compute.FindKernel(kernel); kernels.Add(kernel,k); }
            compute.SetTexture(k, "_Velocity", velocity); compute.SetTexture(k, "_VelocityOut", velocityNext);
            compute.SetTexture(k, "_Density", density); compute.SetTexture(k, "_DensityOut", densityNext);
            compute.SetTexture(k, "_RestDensity", restDensity);
            compute.SetTexture(k,"_ProceduralSources",proceduralSources?proceduralSources:velocity);
            compute.SetTexture(k, "_FoldPattern", foldPattern);
            compute.SetTexture(k, "_Predicted", predicted); compute.SetTexture(k, "_PredictRead", predicted); compute.SetTexture(k, "_Pressure", pressure); compute.SetTexture(k, "_PressureOut", pressureNext);
            compute.SetTexture(k, "_Divergence", divergence); compute.SetTexture(k, "_Curl", curl); compute.SetTexture(k, "_Solid", solid);
            compute.SetTexture(k, "_VelocityPredicted", velocityPredicted); compute.SetTexture(k, "_VelocityPredictRead", velocityPredicted);
            compute.SetTexture(k, "_CoordinatesA", coordinatesA); compute.SetTexture(k, "_CoordinatesAOut", coordinatesANext);
            if(kernel=="LightVolume") compute.SetTexture(k,"_CumulusShape",cumulusShape?cumulusShape:restDensity);
            if(kernel=="LightVolume") compute.SetTexture(k,"_PuffDensity",puffDensity?puffDensity:restDensity);
            compute.SetTexture(k, "_CoordinatesB", coordinatesB); compute.SetTexture(k, "_CoordinatesBOut", coordinatesBNext); compute.SetTexture(k, "_Lighting", lighting);
            compute.Dispatch(k, (allocated.x + 7) / 8, (allocated.y + 3) / 4, (allocated.z + 7) / 8);
        }
        internal void Apply(Material material)
        {
            var settings=ResolvedStyle;
            CoreUtils.SetKeyword(material,"FOG_DRY_ICE",settings.densityShape==FogDensityShape.DryIce && puffDensity);
            material.SetTexture("_FogPuffDensity",puffDensity); material.SetTexture("_FogPreviousPuffDensity",puffPrevious);
            CoreUtils.SetKeyword(material,"FOG_CUMULUS",settings.densityShape==FogDensityShape.Cumulus && cumulusShape);
            material.SetTexture("_FogCumulusShape",cumulusShape);
            if(cumulusShape) material.SetVector("_FogCumulusResolution",new Vector4(cumulusShape.width,cumulusShape.height,cumulusShape.volumeDepth,cumulusShape.mipmapCount-1));
            CoreUtils.SetKeyword(material,"FOG_PLANAR",planar != null);
            if(planar != null) { planar.Apply(material); material.SetTexture("_FogRestingVolume",restDensity); }
            material.SetTexture("_FogDensity", density); material.SetTexture("_FogPreviousDensity", densityNext);
            material.SetTexture("_FogCoordinatesA", coordinatesA); material.SetTexture("_FogCoordinatesB", coordinatesB);
            material.SetTexture("_FogPreviousCoordinatesA", coordinatesANext); material.SetTexture("_FogPreviousCoordinatesB", coordinatesBNext);
            float alpha = Mathf.Clamp01(accumulator * simulationRate);
            material.SetVector("_FogFlow", new Vector4(alpha, Mathf.Max(0,(StepCount-1+alpha) / simulationRate), 0, 0)); material.SetTexture("_FogLighting", lighting);
            material.SetVector("_FogMin", WorldBounds.min); material.SetVector("_FogSize", size);
            material.SetColor("_FogAlbedo", settings.albedo.linear); material.SetColor("_FogAmbient", settings.ambient.linear);
            material.SetVector("_FogLightDirection", sun ? -sun.transform.forward : new Vector3(-.4f, .8f, -.3f).normalized);
            material.SetColor("_FogLightColor", sun ? sun.color.linear * sun.intensity : Color.white);
            material.SetVector("_FogParams", new Vector4(settings.extinction, settings.anisotropy, settings.detail, Time.time));
            material.SetFloat("_FogWispDisplacement", settings.wispDisplacement);
            material.SetFloat("_FogCumulusWispOpacity",Mathf.Max(0,settings.wispOpacity));
            material.SetFloat("_FogCumulusRibbonWidth",Mathf.Max(.006f,Mathf.Clamp(settings.puffSize,.5f,3)*.028f));
            material.SetFloat("_FogFoldSize", Mathf.Max(.5f, settings.foldSize));
            material.SetTexture("_FogFoldPattern", foldPattern);
            material.SetInt("_FogSteps", raySteps);
            CoreUtils.SetKeyword(material,"FOG_PROFILE_NO_DETAIL",profileNoDetail);
        }
        static void Swap(ref RenderTexture a, ref RenderTexture b) { (a, b) = (b, a); }
        void Release()
        {
            planar?.Release(); planar=null; ReleaseCumulusShape(); ReleasePuffs();
            kernels.Clear(); obstacleMaskDirty=true; maskObstacleCount=-1;
            foreach (var t in new[] { velocity, velocityNext, density, densityNext, restDensity, foldPattern, predicted, pressure, pressureNext, divergence, curl, solid, lighting, velocityPredicted, coordinatesA, coordinatesANext, coordinatesB, coordinatesBNext })
                if (t) { t.Release(); if (Application.isPlaying) Destroy(t); else DestroyImmediate(t); }
            if (compute) { if (Application.isPlaying) Destroy(compute); else DestroyImmediate(compute); }
            velocity = velocityNext = density = densityNext = predicted = pressure = pressureNext = divergence = curl = solid = lighting = velocityPredicted = coordinatesA = coordinatesANext = coordinatesB = coordinatesBNext = null;
            compute = null;
            restDensity = null;
            foldPattern = null;
        }
        void OnDrawGizmosSelected() { Gizmos.color = new Color(.4f, .9f, 1); Gizmos.DrawWireCube(WorldBounds.center, WorldBounds.size); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active.Clear();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RestoreRegistrations()
        {
            foreach (var volume in FindObjectsByType<FogVolume>(FindObjectsSortMode.None))
                if (volume.isActiveAndEnabled && !Active.Contains(volume)) { Active.Add(volume); volume.Initialize(); }
        }
    }
}
