using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace jlinkdev.ProceduralNebulae
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class NebulaVolume : MonoBehaviour
    {
        internal static readonly List<NebulaVolume> Active = new();
        public NebulaStyleProfile style;
        public bool overrideFormSeed, overrideDetailSeed;
        public int detailSeed = 1049;
        [Range(0,1)] public float variationStrength = 1;
        public bool varyStarPositions;
        public int starSeed = 1;
        [Tooltip("Maximum distance from each authored anchor in world units."), Min(0)] public float starOffsetRadius = 12;
        public int FormSeed => style && !overrideFormSeed ? style.defaultFormSeed : seed;
        public int DetailSeed => style && !overrideDetailSeed ? style.defaultDetailSeed : detailSeed;
        public NebulaStyleSettings ResolvedStyle => style ? style.settings : NebulaStyleSettings.FromVolume(this);
        [Header("Gas and dust")]
        public int seed = 99;
        [Tooltip("World-space width of the cubic field. Object rotation is supported."), Min(1)]
        public float width = 240;
        [InspectorName("Fold Frequency"), Range(2, 12)] public float banksAcross = 4.8f;
        [Range(0, 1)] public float coverage = .60f;
        [Tooltip("Blends narrow dust lanes into broad, overlapping gas masses."), Range(0, 1)]
        public float cloudBody = .94f;
        [Tooltip("Shape the field around a hollow emission region with a separate pocket of cool gas.")]
        public bool emissionCavity;
        [Range(0, 1)] public float wisps = .98f;
        [Header("Clearing")]
        [Tooltip("Optional transform specifying the centre of the clearing. Defaults to the volume centre.")]
        public Transform clearing;
        [Min(0)] public float clearingRadius = 14;
        [Min(.01f)] public float clearingSoftness = 27;
        [Header("Starlight")]
        public NebulaStar star;
        [Tooltip("Optional second star with independent scattering and dust shadows.")]
        public NebulaStar companion;
        [Tooltip("Optical thickness across one box width at full density."), Range(0, 80)]
        public float extinction = 38;
        [ColorUsage(false, true)] public Color shadowFill = new(.27f,.23f,.41f);
        [ColorUsage(false, true)] public Color litDust = new(1f,.88f,.76f);
        [Header("Excited gas")]
        [ColorUsage(false, true)] public Color outerGas = new(.28f,.27f,.55f);
        [ColorUsage(false, true)] public Color innerGas = new(.65f,.55f,.74f);
        [Range(0, 4)] public float gasBrightness = .48f;
        [Header("Rendering")]
        [Range(32, 192)] public int raySteps = 160;
        public bool halfResolution = true;

        const int DensitySize = 128;
        const int LightSize = 64;
        const int DetailSize = 64;
        RenderTexture density, detail, opticalDepth, companionDepth;
        ComputeShader compute;
        Material material;
        (int, Vector3, float, float, float, float, NebulaShape, float, float, bool, bool, Texture3D, float, Vector3, float, float, float) lastShape;
        (Vector3, float, float) lastDetail;
        (int mode, float amount, Vector3 phase, bool hasPattern) lastBakedWarp;
        (int mode, float amount, Vector3 phase) lastPattern;
        (int profile, int selected, float strength) lastBankLayout;
        Vector3 lastStar, lastCompanion;
        bool haveDetail, haveShape, haveLight, haveCompanionLight, failed;
        internal Material Material => material;
        internal RenderTexture PrimaryOpticalDepth => haveLight ? opticalDepth : null;
        float Width => Mathf.Max(1, width);
        Matrix4x4 WorldToBox => Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * Width).inverse;
        bool HasStar => star && star.isActiveAndEnabled;

        // Local hash streams keep changes reproducible without touching Unity's global random state.
        static float Sample(uint key)
        {
            unchecked
            {
                key ^= key >> 16; key *= 0x7feb352du;
                key ^= key >> 15; key *= 0x846ca68bu;
                return ((key ^ (key >> 16)) >> 8) / 16777216f;
            }
        }

        static Vector3 SeedVector(int value, uint stream)
        {
            uint key = unchecked((uint)value + stream);
            return new Vector3(Sample(key),Sample(key ^ 0xb5297a4du),Sample(key ^ 0x68e31da4u));
        }

        static Vector3 SeedShift(int canonical,int profileSeed,int selected,uint stream,float strength)
        {
            Vector3 origin = SeedVector(canonical,stream);
            Vector3 baseline = SeedVector(profileSeed,stream);
            return baseline-origin + (SeedVector(selected,stream)-baseline)*strength;
        }

        // Seed 91 is the hand-authored arrangement. Each bank has independent
        // streams for its orbit, local orientation, proportions and displacement.
        static Vector3 BankVariation(int seed, uint stream) =>
            seed==91 ? Vector3.zero : 2*SeedVector(seed,stream)-Vector3.one;

        static Vector3 BankVariation((int profile,int selected,float strength) layout, uint stream) =>
            Vector3.Lerp(BankVariation(layout.profile,stream),BankVariation(layout.selected,stream),layout.strength);

        static Matrix4x4 BankSamplingMatrix((int profile,int selected,float strength) layout,
                                             uint stream, Vector3 pivot)
        {
            Vector3 offset = BankVariation(layout,stream);
            Vector3 angles = BankVariation(layout,stream+1);
            Vector3 size = BankVariation(layout,stream+2);
            var orbit = Quaternion.AngleAxis(angles.z*180,Vector3.forward);
            Vector3 centre = orbit*pivot + Vector3.Scale(offset,new Vector3(.078f,.075f,.058f));
            var rotation = orbit*Quaternion.Euler(angles.x*32,angles.y*36,offset.z*38);
            var scale = Vector3.one + Vector3.Scale(size,new Vector3(.50f,.52f,.40f));
            return Matrix4x4.Translate(pivot)*Matrix4x4.TRS(centre,rotation,scale).inverse;
        }

        public Vector3 GetStarPosition(NebulaStar source)
        {
            Vector3 anchor = source.transform.position;
            if (!varyStarPositions) return anchor;
            uint stream = source == companion ? 0x942a3f15u : 0x312f438bu;
            Vector3 offset = Vector3.ClampMagnitude(2*SeedVector(starSeed,stream)-Vector3.one,1);
            return anchor + transform.rotation*offset*Mathf.Max(0,starOffsetRadius);
        }

        void OnEnable() { Active.Add(this); failed = false; }
        void OnDisable()
        {
            Active.Remove(this);
            ReleaseResources();
        }

        void ReleaseResources()
        {
            Release(ref density);
            Release(ref detail);
            Release(ref opticalDepth);
            Release(ref companionDepth);
            CoreUtils.Destroy(material);
            material = null;
            compute = null;
            haveDetail = haveShape = haveLight = haveCompanionLight = false;
        }

        static void Release(ref RenderTexture texture)
        {
            if (texture) { texture.Release(); CoreUtils.Destroy(texture); }
            texture = null;
        }

        static RenderTexture Allocate(int size, string label, GraphicsFormat format = GraphicsFormat.R16_SFloat)
        {
            var texture = new RenderTexture(size, size, 0, format)
            {
                name = label, dimension = TextureDimension.Tex3D, volumeDepth = size,
                enableRandomWrite = true, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (texture.Create()) return texture;
            CoreUtils.Destroy(texture);
            return null;
        }

        static bool EnsureCreated(RenderTexture texture) => texture.IsCreated() || texture.Create();

        bool Initialize()
        {
            if (failed) return false;
            var field = Resources.Load<ComputeShader>("NebulaField");
            var shader = Resources.Load<Shader>("NebulaComposite");
            if (field && shader && shader.isSupported && SystemInfo.supportsComputeShaders &&
                SystemInfo.supports3DTextures && SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, GraphicsFormatUsage.LoadStore) &&
                SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.LoadStore))
            {
                compute = field;
                material = CoreUtils.CreateEngineMaterial(shader);
                density = Allocate(DensitySize,"Nebula dust, gas and flow",GraphicsFormat.R16G16B16A16_SFloat);
                detail = Allocate(DetailSize,"Nebula edge detail");
                opticalDepth = Allocate(LightSize,"Nebula stellar optical depth");
                if (density && opticalDepth && detail)
                {
                    detail.wrapMode = TextureWrapMode.Repeat;
                    return true;
                }
            }
            Debug.LogError("Procedural Nebulae needs its package shaders and 3D R16 and RGBA16 floating-point compute textures.", this);
            ReleaseResources();
            failed = true;
            return false;
        }

        internal bool PrepareForRendering()
        {
            if (failed || (!material && !Initialize())) return false;
            if (!density.IsCreated() || !opticalDepth.IsCreated() || !detail.IsCreated())
            {
                haveDetail = haveShape = haveLight = haveCompanionLight = false;
                if (!EnsureCreated(density) || !EnsureCreated(opticalDepth) || !EnsureCreated(detail)) return false;
            }
            if (!haveDetail)
            {
                compute.SetInt("_Seed",1049);
                Dispatch(compute.FindKernel("BuildDetail"),detail,DetailSize);
                haveDetail = true;
            }
            var matrix = WorldToBox;
            var centre = Vector3.one * .5f;
            var cavity = clearing ? matrix.MultiplyPoint3x4(clearing.position) + centre : centre;
            float radius = Mathf.Max(0,clearingRadius)/Width;
            float softness = Mathf.Max(.01f,clearingSoftness)/Width;
            var look = ResolvedStyle;
            float strength = Mathf.Clamp01(variationStrength);
            int noiseSeed = style ? 91 : seed;
            Vector3 formShift = style ? SeedShift(91,style.defaultFormSeed,FormSeed,11,strength) : Vector3.zero;
            Vector3 detailShift = SeedShift(1049,style ? style.defaultDetailSeed : 1049,DetailSeed,23,strength);
            var detailSettings = (detailShift,look.surfaceDetail,look.foldRelief);
            bool detailChanged = !lastDetail.Equals(detailSettings);
            compute.SetVector("_DetailShift",detailShift);
            compute.SetFloat("_SurfaceDetail",look.surfaceDetail);
            compute.SetFloat("_FoldRelief",look.foldRelief);
            lastDetail = detailSettings;
            var shape = (noiseSeed,formShift,look.foldFrequency,look.coverage,look.wisps,look.cloudBody,
                         look.shape,look.plumeScale,look.edgeSoftness,look.layeredCurtain,look.foregroundShelf,look.shapeTexture,
                         look.shapeDisplacement,cavity,radius,softness,look.rearCrease);
            bool varyBanks = style && look.varyBankLayout &&
                             (look.shape==NebulaShape.SculptedBank || look.shape==NebulaShape.SweptArc);
            Vector3 fieldShift = varyBanks ? .35f*formShift : formShift;
            (int profile,int selected,float strength) layout = varyBanks
                ? (style.defaultFormSeed,FormSeed,strength) : (91,91,0f);
            var warp = ResolveWarp(look, strength);
            var bakedWarp = (warp.baseMode, warp.baseStrength, warp.baseMode == 0 ? Vector3.zero : warp.phase, warp.pattern != 0);
            var pattern = (warp.pattern, warp.patternStrength, warp.pattern == 0 ? Vector3.zero : warp.phase);
            bool patternChanged = !lastPattern.Equals(pattern);
            // Compute assets share uniforms across volumes. Bind the complete sampling state
            // before either density or shadow dispatch, including on a shadow-only update.
            compute.SetFloat("_Wisps",look.wisps);
            compute.SetFloat("_CloudBody",look.cloudBody);
            compute.SetInt("_NebulaShape",(int)look.shape);
            compute.SetFloat("_EdgeSoftness",look.edgeSoftness);
            compute.SetInt("_LayeredCurtain",look.layeredCurtain ? 1 : 0);
            compute.SetVector("_FormShift",fieldShift);
            compute.SetInt("_StructureWarp", warp.baseMode);
            compute.SetFloat("_StructureWarpStrength", warp.baseStrength);
            compute.SetVector("_Clearing",new Vector4(cavity.x,cavity.y,cavity.z,radius));
            compute.SetFloat("_ClearingSoftness",softness);
            compute.SetInt("_DomainPattern", warp.pattern);
            compute.SetFloat("_DomainStrength", warp.patternStrength);
            compute.SetVector("_WarpPhase", warp.phase);
            lastPattern = pattern;
            bool rebuild = !haveShape || !lastShape.Equals(shape) || !lastBankLayout.Equals(layout)
                || !lastBakedWarp.Equals(bakedWarp);
            if (rebuild)
            {
                int kernel = compute.FindKernel("BuildDensity");
                compute.SetInt("_Seed",noiseSeed);
                SetBankLayout(layout, look.shape);
                compute.SetFloat("_BankScale",look.foldFrequency);
                compute.SetFloat("_Coverage",look.coverage);
                compute.SetFloat("_PlumeScale",look.plumeScale);
                compute.SetInt("_ForegroundShelf",look.foregroundShelf ? 1 : 0);
                compute.SetFloat("_RearCrease",look.rearCrease);
                compute.SetTexture(kernel,"_ShapeTexture",look.shapeTexture ? (Texture)look.shapeTexture : detail);
                compute.SetFloat("_ShapeDisplacement",look.shapeTexture ? look.shapeDisplacement : 0);
                Dispatch(kernel,density,DensitySize,"_MediumOutput");
                lastShape = shape;
                lastBakedWarp = bakedWarp;
                lastBankLayout = layout;
                haveShape = true;
            }

            bool mediumChanged = rebuild || detailChanged || patternChanged;
            Vector3 position = HasStar ? matrix.MultiplyPoint3x4(GetStarPosition(star)) + centre : centre;
            if (HasStar && (mediumChanged || !haveLight || position != lastStar))
            {
                UpdateShadow(position,opticalDepth);
                lastStar = position;
                haveLight = true;
            }
            if (!HasStar) haveLight = false;
            bool hasCompanion = companion && companion != star && companion.isActiveAndEnabled;
            if (hasCompanion && !companionDepth)
                companionDepth = Allocate(LightSize,"Nebula companion optical depth");
            if (!hasCompanion && companionDepth) Release(ref companionDepth);
            hasCompanion = hasCompanion && companionDepth;
            if (hasCompanion && !companionDepth.IsCreated())
            {
                haveCompanionLight = false;
                hasCompanion = companionDepth.Create();
            }
            Vector3 second = hasCompanion ? matrix.MultiplyPoint3x4(GetStarPosition(companion))+centre : centre;
            if (hasCompanion && (mediumChanged || !haveCompanionLight || second != lastCompanion))
            {
                UpdateShadow(second,companionDepth);
                lastCompanion = second;
            }
            haveCompanionLight = hasCompanion;
            material.SetTexture("_CompanionDepth",hasCompanion ? companionDepth : density);
            material.SetVector("_CompanionUVW",second);
            material.SetFloat("_CompanionRadius",hasCompanion ? Mathf.Max(.01f,companion.radius)/Width : 0);
            material.SetColor("_CompanionColor",hasCompanion ? companion.color.linear*Mathf.Max(0,companion.intensity) : Color.black);
            material.SetMatrix("_WorldToBox",matrix);
            material.SetVector("_StarUVW",position);
            material.SetFloat("_StarRadius",HasStar ? Mathf.Max(.01f,star.radius)/Width : 0);
            material.SetColor("_StarColor",HasStar ? star.color.linear * Mathf.Max(0,star.intensity) : Color.black);
            material.SetColor("_ShadowFill",look.shadowFill.linear);
            material.SetColor("_LitDust",look.litDust.linear);
            material.SetVector("_OuterGas",look.outerGas.linear);
            material.SetVector("_InnerGas",look.innerGas.linear);
            material.SetVector("_TransitionGas",look.transitionGas.linear);
            material.SetFloat("_GasBrightness",look.gasBrightness);
            material.SetFloat("_Extinction",look.extinction);
            material.SetInt("_RaySteps",Mathf.Clamp(raySteps,32,192));
            material.SetTexture("_Density",density);
            material.SetTexture("_Detail",detail);
            material.SetFloat("_Wisps",look.wisps);
            material.SetFloat("_SurfaceDetail",look.surfaceDetail);
            material.SetFloat("_FoldRelief",look.foldRelief);
            material.SetFloat("_EdgeSoftness",look.edgeSoftness);
            material.SetFloat("_CloudBody",look.cloudBody);
            material.SetVector("_DetailShift",detailShift);
            material.SetInt("_NebulaShape",(int)look.shape);
            material.SetInt("_StructureWarp",warp.baseMode);
            material.SetVector("_FormShift",fieldShift);
            material.SetFloat("_StructureWarpStrength",warp.baseStrength);
            material.SetVector("_WarpPhase",warp.phase);
            material.SetInt("_DomainPattern",warp.pattern);
            material.SetFloat("_DomainStrength",warp.patternStrength);
            material.SetVector("_Clearing",new Vector4(cavity.x,cavity.y,cavity.z,radius));
            material.SetFloat("_ClearingSoftness",softness);
            material.SetInt("_LayeredCurtain",look.layeredCurtain ? 1 : 0);
            material.SetTexture("_OpticalDepth",PrimaryOpticalDepth ?? density);
            return true;
        }

        (int baseMode, float baseStrength, int pattern, float patternStrength, Vector3 phase)
            ResolveWarp(NebulaStyleSettings look, float variation)
        {
            if (look.structureWarp == NebulaStructureWarp.None ||
                look.shape is not (NebulaShape.PainterlyCavity or NebulaShape.SculptedBank or NebulaShape.SweptArc))
                return default;

            bool patterned = (int)look.structureWarp >= (int)NebulaStructureWarp.OverturningBillows;
            float amount = Mathf.Clamp(look.structureWarpStrength, -2, 2);
            float bend = patterned ? Mathf.Clamp(look.baseWarpStrength, -2, 2) : amount;
            int mode = patterned ? (int)look.baseWarp : (int)look.structureWarp;
            int pattern = patterned && amount != 0 ? (int)look.structureWarp : 0;
            if (bend == 0) mode = 0;
            if (mode == 0 && pattern == 0) return default;

            const uint stream = 991;
            Vector3 phase = look.overrideWarpSeed ? SeedVector(look.warpSeed, stream) :
                Vector3.Lerp(SeedVector(style.defaultFormSeed, stream), SeedVector(FormSeed, stream), variation);
            return (mode, mode == 0 ? 0 : bend, pattern, pattern == 0 ? 0 : amount, phase);
        }

        void SetBankLayout((int profile, int selected, float strength) layout, NebulaShape shape)
        {
            bool transformed = (layout.profile!=91 && layout.strength<1) || (layout.selected!=91 && layout.strength>0);
            compute.SetInt("_VaryBankLayout",transformed ? 1 : 0);
            if (transformed && shape==NebulaShape.SweptArc)
            {
                compute.SetMatrix("_ArcSweep",BankSamplingMatrix(layout,503,new Vector3(-.038f,.063f,-.045f)));
                compute.SetMatrix("_ArcShoulder",BankSamplingMatrix(layout,601,new Vector3(.163f,0,-.083f)));
                compute.SetMatrix("_ArcHook",BankSamplingMatrix(layout,701,new Vector3(.13f,.045f,.16f)));
                compute.SetMatrix("_ArcCanopy",BankSamplingMatrix(layout,809,new Vector3(-.21f,.244f,.097f)));
                compute.SetVector("_ArcShape",BankVariation(layout,907));
            }
            else if (transformed)
            {
                compute.SetMatrix("_FrontBank",BankSamplingMatrix(layout,101,new Vector3(.18f,-.04f,.012f)));
                compute.SetMatrix("_RearBank",BankSamplingMatrix(layout,211,new Vector3(-.075f,.105f,.24f)));
                compute.SetMatrix("_ShelfBank",BankSamplingMatrix(layout,307,new Vector3(-.15f,-.15f,-.087f)));
                compute.SetVector("_BankBend",.068f*BankVariation(layout,419));
            }
        }

        void UpdateShadow(Vector3 position, RenderTexture target)
        {
            int kernel = compute.FindKernel("StarOpticalDepth");
            compute.SetVector("_StarUVW",position);
            compute.SetTexture(kernel,"_Density",density);
            compute.SetTexture(kernel,"_Detail",detail);
            Dispatch(kernel,target,LightSize);
        }

        void Dispatch(int kernel, RenderTexture target, int size, string output = "_Output")
        {
            compute.SetInt("_Resolution",size);
            compute.SetTexture(kernel,output,target);
            compute.Dispatch(kernel,size/4,size/4,size/4);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position,transform.rotation,Vector3.one);
            Gizmos.color = new Color(.47f,.65f,1);
            Gizmos.DrawWireCube(Vector3.zero,Vector3.one*Width);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(clearing ? clearing.position : transform.position,clearingRadius);
            if (varyStarPositions)
            {
                if (star) DrawOffset(star);
                if (companion && companion != star) DrawOffset(companion);
            }
        }

        void DrawOffset(NebulaStar source)
        {
            Vector3 position = GetStarPosition(source);
            Gizmos.color = source.color;
            Gizmos.DrawLine(source.transform.position,position);
            Gizmos.DrawWireSphere(position,source.radius);
        }
    }
}
