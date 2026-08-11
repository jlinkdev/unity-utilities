using System;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields
{
    public enum ForcefieldPropagationMode
    {
        SurfaceDistance = 0,
        Spherical = 1
    }

    public enum ForcefieldImpactCapacity
    {
        Four = 4,
        Eight = 8,
        Sixteen = 16,
        ThirtyTwo = 32
    }

    /// <summary>Drives a per-renderer forcefield effect and its allocation-free impact history.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("jlinkdev/Forcefields/Forcefield")]
    public sealed class Forcefield : MonoBehaviour
    {
        public const string ShaderName = "jlinkdev/Forcefields/Forcefield";

        [Header("Rendering")]
        [SerializeField] private Renderer[] targetRenderers = Array.Empty<Renderer>();
        [SerializeField] private ForcefieldPreset preset;
        [SerializeField] private ForcefieldPropagationMode propagationMode = ForcefieldPropagationMode.Spherical;
        [SerializeField, Range(0f, 2f)] private float intensity = 1f;

        [Header("Impacts")]
        [SerializeField] private ForcefieldImpactCapacity impactCapacity = ForcefieldImpactCapacity.Sixteen;
        [SerializeField, Min(0f)] private float defaultImpactRadius = 0.04f;

        [Header("Authoring")]
        [SerializeField] private bool drawGizmos = true;

        [NonSerialized] private ForcefieldImpactBuffer impactBuffer;
        [NonSerialized] private MaterialPropertyBlock propertyBlock;
        [NonSerialized] private Matrix4x4[] rendererMatrices;
        [NonSerialized] private Matrix4x4 rootMatrix;
        [NonSerialized] private ForcefieldStyle currentStyle;
        [NonSerialized] private ForcefieldStyle blendStartStyle;
        [NonSerialized] private ForcefieldStyle blendTargetStyle;
        [NonSerialized] private ForcefieldPreset blendTargetPreset;
        [NonSerialized] private float blendDuration;
        [NonSerialized] private float blendElapsed;
        [NonSerialized] private bool isBlending;
        [NonSerialized] private bool propertiesDirty = true;

        /// <summary>Raised after a visual impact is accepted by the ring buffer.</summary>
        public event Action<Forcefield, ForcefieldImpact> ImpactAdded;

        public Renderer[] TargetRenderers
        {
            get => targetRenderers;
            set
            {
                targetRenderers = value ?? Array.Empty<Renderer>();
                rendererMatrices = null;
                propertiesDirty = true;
            }
        }

        public ForcefieldPreset Preset
        {
            get => preset;
            set => ApplyPreset(value);
        }

        public ForcefieldPropagationMode PropagationMode
        {
            get => propagationMode;
            set
            {
                propagationMode = value;
                propertiesDirty = true;
            }
        }

        public float Intensity
        {
            get => intensity;
            set
            {
                intensity = Mathf.Clamp(value, 0f, 2f);
                propertiesDirty = true;
            }
        }

        public int ActiveImpactCount => impactBuffer != null ? impactBuffer.Count : 0;
        public int ImpactCapacity => (int)impactCapacity;
        public bool IsBlendingPreset => isBlending;

        public ForcefieldImpactCapacity ImpactBufferCapacity
        {
            get => impactCapacity;
            set
            {
                impactCapacity = value;
                EnsureRuntimeState();
                impactBuffer.SetCapacity((int)impactCapacity);
                propertiesDirty = true;
            }
        }

        private void Reset()
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
            currentStyle = preset != null ? preset.Capture() : ForcefieldStyle.Default;
            propertiesDirty = true;
        }

        private void OnEnable()
        {
            EnsureRuntimeState();
            currentStyle = preset != null ? preset.Capture() : ForcefieldStyle.Default;
            propertiesDirty = true;
            ApplyProperties();
        }

        private void OnDisable()
        {
            isBlending = false;
        }

        private void OnValidate()
        {
            intensity = Mathf.Clamp(intensity, 0f, 2f);
            defaultImpactRadius = Mathf.Max(0f, defaultImpactRadius);
            EnsureRuntimeState();
            impactBuffer.SetCapacity((int)impactCapacity);
            currentStyle = preset != null ? preset.Capture() : ForcefieldStyle.Default;
            isBlending = false;
            propertiesDirty = true;
        }

        private void Update()
        {
            EnsureRuntimeState();

            if (isBlending)
            {
                blendElapsed += Application.isPlaying ? Time.deltaTime : 0f;
                float t = blendDuration <= 0f ? 1f : Mathf.Clamp01(blendElapsed / blendDuration);
                currentStyle = ForcefieldStyle.Lerp(blendStartStyle, blendTargetStyle, t);
                propertiesDirty = true;
                if (t >= 1f)
                {
                    preset = blendTargetPreset;
                    isBlending = false;
                }
            }

            if (RendererTransformsChanged())
                propertiesDirty = true;

            if (propertiesDirty)
                ApplyProperties();
        }

        /// <summary>Immediately applies a preset. Pass null to use the built-in default style.</summary>
        public void ApplyPreset(ForcefieldPreset newPreset)
        {
            preset = newPreset;
            currentStyle = preset != null ? preset.Capture() : ForcefieldStyle.Default;
            isBlending = false;
            propertiesDirty = true;
        }

        /// <summary>Blends all visual values to another preset over time.</summary>
        public void BlendToPreset(ForcefieldPreset newPreset, float duration)
        {
            EnsureRuntimeState();
            if (!Application.isPlaying || duration <= 0f)
            {
                ApplyPreset(newPreset);
                return;
            }

            blendStartStyle = currentStyle;
            blendTargetStyle = newPreset != null ? newPreset.Capture() : ForcefieldStyle.Default;
            blendTargetPreset = newPreset;
            blendDuration = duration;
            blendElapsed = 0f;
            isBlending = true;
        }

        /// <summary>Adds an impact using a radial normal and the configured default radius.</summary>
        public void AddImpact(Vector3 worldPosition)
        {
            AddImpact(worldPosition, DefaultNormal(worldPosition), 1f, defaultImpactRadius);
        }

        /// <summary>Adds an impact using a radial normal and the configured default radius.</summary>
        public void AddImpact(Vector3 worldPosition, float strength)
        {
            AddImpact(worldPosition, DefaultNormal(worldPosition), strength, defaultImpactRadius);
        }

        /// <summary>Adds a visual impact to the fixed-size ring buffer.</summary>
        public void AddImpact(Vector3 worldPosition, Vector3 worldNormal, float strength, float radius)
        {
            EnsureRuntimeState();
            ForcefieldImpact impact = new ForcefieldImpact(worldPosition, worldNormal, strength, radius);
            Vector3 localPosition = transform.InverseTransformPoint(impact.Position);
            Vector3 localNormal = transform.InverseTransformDirection(impact.Normal);
            float startTime = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;

            impactBuffer.Add(
                localPosition,
                localNormal,
                startTime,
                impact.Strength,
                impact.Radius,
                currentStyle.ImpactDuration);

            propertiesDirty = true;
            ApplyProperties();
            ImpactAdded?.Invoke(this, impact);
        }

        /// <summary>Clears every active impact immediately.</summary>
        public void ClearImpacts()
        {
            EnsureRuntimeState();
            impactBuffer.Clear();
            propertiesDirty = true;
            ApplyProperties();
        }

        /// <summary>Re-uploads all properties after external renderer or material changes.</summary>
        public void Refresh()
        {
            if (!isBlending)
                currentStyle = preset != null ? preset.Capture() : ForcefieldStyle.Default;
            rendererMatrices = null;
            propertiesDirty = true;
            ApplyProperties();
        }

        public bool UsesForcefieldShader(Renderer renderer)
        {
            if (renderer == null)
                return false;

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].shader != null && materials[i].shader.name == ShaderName)
                    return true;
            }

            return false;
        }

        private void EnsureRuntimeState()
        {
            if (targetRenderers == null)
                targetRenderers = Array.Empty<Renderer>();
            if (impactBuffer == null)
                impactBuffer = new ForcefieldImpactBuffer((int)impactCapacity);
            else
                impactBuffer.SetCapacity((int)impactCapacity);
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
            if (rendererMatrices == null || rendererMatrices.Length != targetRenderers.Length)
            {
                rendererMatrices = new Matrix4x4[targetRenderers.Length];
                for (int i = 0; i < rendererMatrices.Length; i++)
                    rendererMatrices[i] = targetRenderers[i] != null ? targetRenderers[i].transform.localToWorldMatrix : Matrix4x4.zero;
            }
        }

        private bool RendererTransformsChanged()
        {
            if (transform.localToWorldMatrix != rootMatrix)
                return true;

            if (rendererMatrices == null || rendererMatrices.Length != targetRenderers.Length)
                return true;

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                Matrix4x4 current = renderer != null ? renderer.transform.localToWorldMatrix : Matrix4x4.zero;
                if (current != rendererMatrices[i])
                    return true;
            }

            return false;
        }

        private void ApplyProperties()
        {
            EnsureRuntimeState();
            Matrix4x4 rootLocalToWorld = transform.localToWorldMatrix;

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetMatrix(ForcefieldShaderProperties.RootLocalToWorld, rootLocalToWorld);
                propertyBlock.SetFloat(ForcefieldShaderProperties.Intensity, intensity);
                propertyBlock.SetFloat(ForcefieldShaderProperties.PropagationMode, (float)propagationMode);
                propertyBlock.SetFloat(ForcefieldShaderProperties.SphereRadius, MaxExtent(renderer.bounds));
                propertyBlock.SetInt(ForcefieldShaderProperties.ImpactCount, impactBuffer.Count);
                propertyBlock.SetVectorArray(ForcefieldShaderProperties.ImpactPositions, impactBuffer.PositionsAndTimes);
                propertyBlock.SetVectorArray(ForcefieldShaderProperties.ImpactNormals, impactBuffer.NormalsAndStrengths);
                propertyBlock.SetVectorArray(ForcefieldShaderProperties.ImpactRadii, impactBuffer.RadiiAndDurations);
                ApplyStyle(propertyBlock, currentStyle);
                renderer.SetPropertyBlock(propertyBlock);
                rendererMatrices[i] = renderer.transform.localToWorldMatrix;
            }

            rootMatrix = rootLocalToWorld;
            propertiesDirty = false;
        }

        private static void ApplyStyle(MaterialPropertyBlock block, ForcefieldStyle style)
        {
            block.SetColor(ForcefieldShaderProperties.SurfaceColor, style.SurfaceColor);
            block.SetFloat(ForcefieldShaderProperties.SurfaceIntensity, style.SurfaceIntensity);
            block.SetFloat(ForcefieldShaderProperties.Opacity, style.Opacity);
            block.SetFloat(ForcefieldShaderProperties.BackfaceOpacity, style.BackfaceOpacity);
            block.SetColor(ForcefieldShaderProperties.FresnelColor, style.FresnelColor);
            block.SetFloat(ForcefieldShaderProperties.FresnelIntensity, style.FresnelIntensity);
            block.SetFloat(ForcefieldShaderProperties.FresnelPower, style.FresnelPower);
            block.SetFloat(ForcefieldShaderProperties.RefractionEnabled, style.RefractionEnabled);
            block.SetFloat(ForcefieldShaderProperties.RefractionStrength, style.RefractionStrength);
            block.SetFloat(ForcefieldShaderProperties.ChromaticSplit, style.ChromaticSplit);
            block.SetFloat(ForcefieldShaderProperties.NoiseEnabled, style.NoiseEnabled);
            block.SetFloat(ForcefieldShaderProperties.NoiseScale, style.NoiseScale);
            block.SetVector(ForcefieldShaderProperties.NoiseVelocity, style.NoiseVelocity);
            block.SetFloat(ForcefieldShaderProperties.NoiseStrength, style.NoiseStrength);
            block.SetFloat(ForcefieldShaderProperties.PulseSpeed, style.PulseSpeed);
            block.SetFloat(ForcefieldShaderProperties.PulseStrength, style.PulseStrength);
            block.SetFloat(ForcefieldShaderProperties.PatternEnabled, style.PatternEnabled);
            block.SetColor(ForcefieldShaderProperties.PatternColor, style.PatternColor);
            block.SetFloat(ForcefieldShaderProperties.PatternScale, style.PatternScale);
            block.SetFloat(ForcefieldShaderProperties.PatternWidth, style.PatternWidth);
            block.SetFloat(ForcefieldShaderProperties.PatternIntensity, style.PatternIntensity);
            block.SetColor(ForcefieldShaderProperties.ImpactColor, style.ImpactColor);
            block.SetFloat(ForcefieldShaderProperties.ImpactIntensity, style.ImpactIntensity);
            block.SetFloat(ForcefieldShaderProperties.RippleSpeed, style.RippleSpeed);
            block.SetFloat(ForcefieldShaderProperties.RippleWidth, style.RippleWidth);
            block.SetFloat(ForcefieldShaderProperties.RippleFadePower, style.RippleFadePower);
            block.SetFloat(ForcefieldShaderProperties.RippleRefraction, style.RippleRefraction);
            block.SetFloat(ForcefieldShaderProperties.IntersectionEnabled, style.IntersectionEnabled);
            block.SetColor(ForcefieldShaderProperties.IntersectionColor, style.IntersectionColor);
            block.SetFloat(ForcefieldShaderProperties.IntersectionIntensity, style.IntersectionIntensity);
            block.SetFloat(ForcefieldShaderProperties.IntersectionWidth, style.IntersectionWidth);
            block.SetFloat(ForcefieldShaderProperties.Quality, style.Quality);
        }

        private Vector3 DefaultNormal(Vector3 worldPosition)
        {
            Vector3 radial = worldPosition - transform.position;
            return radial.sqrMagnitude > 0.000001f ? radial.normalized : transform.up;
        }

        private static float MaxExtent(Bounds bounds)
        {
            return Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            Gizmos.color = new Color(0.1f, 0.85f, 1f, 0.55f);
            if (targetRenderers == null)
                return;
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] != null)
                    Gizmos.DrawWireCube(targetRenderers[i].bounds.center, targetRenderers[i].bounds.size);
            }
        }
    }
}
