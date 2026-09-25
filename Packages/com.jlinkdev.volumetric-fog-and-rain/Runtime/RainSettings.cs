using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain
{
    /// <summary>Camera-owned runtime values. Copy or blend profiles without modifying assets.</summary>
    [System.Serializable]
    public sealed class RainSettings
    {
        public RainRenderMode renderMode;
        public RainExtent extent;
        public float density = 0.8f;
        public float cellSize = 0.65f;
        public float streakLength = 0.35f;
        public float streakWidth = 0.008f;
        public float fallSpeed = 12;
        public Vector3 direction = Vector3.down;
        public Vector3 wind = new Vector3(1, 0, 0);
        public int seed = 1234;
        public float nearFade = 0.3f;
        public float midDistance = 12;
        public float farDistance = 30;
        public float maxDistance = 100;
        public int maxCellSteps = 96;
        public int hazeSteps = 12;
        public float noiseScale = 0.025f;
        public float noiseStrength = 0.35f;
        public Color rainColor = new Color(0.65f, 0.75f, 0.85f, 1);
        public float brightness = 1.5f;
        public float streakOpacity = 0.7f;
        public float hazeExtinction = 0.015f;
        public float scattering = 0.65f;
        public bool independentFogSettings;
        public float fogDensity = 0.8f;
        public Color fogColor = new Color(0.65f, 0.75f, 0.85f, 1);
        public float fogBrightness = 1.5f;
        public float fogScattering = 0.65f;
        public FogDistanceMode fogDistanceMode;
        public float fogStartDistance;
        public float fogDistanceFade;
        public float fogMaxDistance = 100;
        public float fogBaseHeight;
        public float fogHeightFalloff;
        public Vector3 fogNoiseVelocity;
        public float directionalScattering;
        public float scatteringAnisotropy = 0.4f;

        public bool UsesIndependentFog => renderMode == RainRenderMode.FogOnly || independentFogSettings;
        public bool HasRain => renderMode != RainRenderMode.FogOnly && density > 0 && streakOpacity > 0;
        public bool HasFog => renderMode != RainRenderMode.RainOnly && hazeExtinction > 0 &&
            (UsesIndependentFog ? fogDensity > 0 : density > 0);

        public Vector3 Velocity => (direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.down) * fallSpeed + wind;


        public RainSettings() { }
        public RainSettings(RainProfile source) => CopyFrom(source);
        public void CopyFrom(RainProfile source)
        {
            if (source == null) throw new System.ArgumentNullException(nameof(source));
            renderMode = source.renderMode;
            extent = source.extent;
            density = source.density;
            cellSize = source.cellSize;
            streakLength = source.streakLength;
            streakWidth = source.streakWidth;
            fallSpeed = source.fallSpeed;
            direction = source.direction;
            wind = source.wind;
            seed = source.seed;
            nearFade = source.nearFade;
            midDistance = source.midDistance;
            farDistance = source.farDistance;
            maxDistance = source.maxDistance;
            maxCellSteps = source.maxCellSteps;
            hazeSteps = source.hazeSteps;
            noiseScale = source.noiseScale;
            noiseStrength = source.noiseStrength;
            rainColor = source.rainColor;
            brightness = source.brightness;
            streakOpacity = source.streakOpacity;
            hazeExtinction = source.hazeExtinction;
            scattering = source.scattering;
            independentFogSettings = source.independentFogSettings;
            fogDensity = source.fogDensity;
            fogColor = source.fogColor;
            fogBrightness = source.fogBrightness;
            fogScattering = source.fogScattering;
            fogDistanceMode = source.fogDistanceMode;
            fogStartDistance = source.fogStartDistance;
            fogDistanceFade = source.fogDistanceFade;
            fogMaxDistance = source.fogMaxDistance;
            fogBaseHeight = source.fogBaseHeight;
            fogHeightFalloff = source.fogHeightFalloff;
            fogNoiseVelocity = source.fogNoiseVelocity;
            directionalScattering = source.directionalScattering;
            scatteringAnisotropy = source.scatteringAnisotropy;
        }
        public void CopyFrom(RainSettings source)
        {
            if (source == null) throw new System.ArgumentNullException(nameof(source));
            renderMode = source.renderMode;
            extent = source.extent;
            density = source.density;
            cellSize = source.cellSize;
            streakLength = source.streakLength;
            streakWidth = source.streakWidth;
            fallSpeed = source.fallSpeed;
            direction = source.direction;
            wind = source.wind;
            seed = source.seed;
            nearFade = source.nearFade;
            midDistance = source.midDistance;
            farDistance = source.farDistance;
            maxDistance = source.maxDistance;
            maxCellSteps = source.maxCellSteps;
            hazeSteps = source.hazeSteps;
            noiseScale = source.noiseScale;
            noiseStrength = source.noiseStrength;
            rainColor = source.rainColor;
            brightness = source.brightness;
            streakOpacity = source.streakOpacity;
            hazeExtinction = source.hazeExtinction;
            scattering = source.scattering;
            independentFogSettings = source.independentFogSettings;
            fogDensity = source.fogDensity;
            fogColor = source.fogColor;
            fogBrightness = source.fogBrightness;
            fogScattering = source.fogScattering;
            fogDistanceMode = source.fogDistanceMode;
            fogStartDistance = source.fogStartDistance;
            fogDistanceFade = source.fogDistanceFade;
            fogMaxDistance = source.fogMaxDistance;
            fogBaseHeight = source.fogBaseHeight;
            fogHeightFalloff = source.fogHeightFalloff;
            fogNoiseVelocity = source.fogNoiseVelocity;
            directionalScattering = source.directionalScattering;
            scatteringAnisotropy = source.scatteringAnisotropy;
        }
        /// <summary>Blend appearance; modes, seeds, grid geometry and rain motion switch using the explicit policy.</summary>
        public void BlendAppearance(RainProfile from, RainProfile to, float t,
            BlendDiscreteSettings discrete = BlendDiscreteSettings.AtEnd)
        {
            if (from == null || to == null) throw new System.ArgumentNullException();
            t = Mathf.Clamp01(t);
            CopyFrom(discrete == BlendDiscreteSettings.AtStart || t >= 1 ? to : from);
            density = Mathf.Lerp(from.density, to.density, t);
            noiseStrength = Mathf.Lerp(from.noiseStrength, to.noiseStrength, t);
            brightness = Mathf.Lerp(from.brightness, to.brightness, t);
            streakOpacity = Mathf.Lerp(from.streakOpacity, to.streakOpacity, t);
            hazeExtinction = Mathf.Lerp(from.hazeExtinction, to.hazeExtinction, t);
            scattering = Mathf.Lerp(from.scattering, to.scattering, t);
            fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, t);
            fogBrightness = Mathf.Lerp(from.fogBrightness, to.fogBrightness, t);
            fogScattering = Mathf.Lerp(from.fogScattering, to.fogScattering, t);
            fogStartDistance = Mathf.Lerp(from.fogStartDistance, to.fogStartDistance, t);
            fogDistanceFade = Mathf.Lerp(from.fogDistanceFade, to.fogDistanceFade, t);
            fogMaxDistance = Mathf.Lerp(from.fogMaxDistance, to.fogMaxDistance, t);
            fogBaseHeight = Mathf.Lerp(from.fogBaseHeight, to.fogBaseHeight, t);
            fogHeightFalloff = Mathf.Lerp(from.fogHeightFalloff, to.fogHeightFalloff, t);
            directionalScattering = Mathf.Lerp(from.directionalScattering, to.directionalScattering, t);
            scatteringAnisotropy = Mathf.Lerp(from.scatteringAnisotropy, to.scatteringAnisotropy, t);
            rainColor = Color.LerpUnclamped(from.rainColor, to.rainColor, t);
            fogColor = Color.LerpUnclamped(from.fogColor, to.fogColor, t);
            fogNoiseVelocity = Vector3.Lerp(from.fogNoiseVelocity, to.fogNoiseVelocity, t);
            ClampValues();
        }
        private void ClampValues()
        {
            density = Mathf.Clamp01(density);
            cellSize = Mathf.Max(0.1f, cellSize);
            // Containment leaves room for spatial filtering. The grid is 3x taller along velocity.
            streakWidth = Mathf.Clamp(streakWidth, 0.0001f, cellSize * 0.04f);
            streakLength = Mathf.Clamp(streakLength, 0.001f, cellSize * 1.5f);
            fallSpeed = Mathf.Max(0, fallSpeed);
            nearFade = Mathf.Max(0, nearFade);
            midDistance = Mathf.Max(0.1f, midDistance);
            farDistance = Mathf.Max(0.2f, farDistance);
            maxDistance = Mathf.Max(1, maxDistance);
            maxCellSteps = Mathf.Clamp(maxCellSteps, 16, 256);
            hazeSteps = Mathf.Clamp(hazeSteps, 4, 32);
            noiseScale = Mathf.Max(0.001f, noiseScale);
            noiseStrength = Mathf.Clamp01(noiseStrength);
            brightness = Mathf.Max(0, brightness);
            streakOpacity = Mathf.Max(0, streakOpacity);
            hazeExtinction = Mathf.Max(0, hazeExtinction);
            scattering = Mathf.Max(0, scattering);
            fogDensity = Mathf.Max(0, fogDensity);
            fogBrightness = Mathf.Max(0, fogBrightness);
            fogScattering = Mathf.Max(0, fogScattering);
            fogStartDistance = Mathf.Max(0, fogStartDistance);
            fogDistanceFade = Mathf.Max(0, fogDistanceFade);
            fogMaxDistance = Mathf.Max(1, fogMaxDistance);
            fogHeightFalloff = Mathf.Max(0, fogHeightFalloff);
            directionalScattering = Mathf.Max(0, directionalScattering);
            scatteringAnisotropy = Mathf.Clamp(scatteringAnisotropy, -0.9f, 0.9f);
        }

        /// <summary>Explicitly normalize programmatic settings, including distance ordering.</summary>
        public void Sanitize()
        {
            ClampValues();
            if (renderMode == RainRenderMode.FogOnly) return;
            midDistance = Mathf.Max(nearFade + 0.1f, midDistance);
            farDistance = Mathf.Max(midDistance + 0.1f, farDistance);
            maxDistance = Mathf.Max(farDistance, maxDistance);
        }

    }
}
