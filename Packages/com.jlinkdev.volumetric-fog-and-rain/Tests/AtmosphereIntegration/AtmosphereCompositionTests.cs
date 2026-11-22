using System.Collections;
using System.Collections.Generic;
using jlinkdev.UnityUtilities.InteractiveFog;
using jlinkdev.UnityUtilities.VolumetricFogAndRain;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace jlinkdev.UnityUtilities.PackageDevelopment.Tests
{
    public sealed class AtmosphereCompositionTests
    {
        const int TestLayer = 30;
        const int ImageSize = 64;
        readonly List<Object> objects = new();

        T Own<T>(T asset) where T : Object
        {
            objects.Add(asset);
            return asset;
        }

        [UnityTest]
        public IEnumerator LocalMistReceivesOnlyAtmosphereInFrontOfItsDepth()
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supports3DRenderTextures)
                Assert.Ignore("Interactive Fog requires compute shaders and 3D textures.");

            var graphics = GraphicsSettings.defaultRenderPipeline;
            var quality = QualitySettings.renderPipeline;
            var previousTarget = RenderTexture.active;
            try
            {
                var renderer = Own(ScriptableObject.CreateInstance<UniversalRendererData>());
                var weather = Own(ScriptableObject.CreateInstance<RainRendererFeature>());
                var interactive = Own(ScriptableObject.CreateInstance<FogRendererFeature>());
                // Leave the weather feature's normal injection setting at its default.
                weather.shareAtmosphereWithGroundFog = true;
                weather.Create();
                interactive.Create();
                renderer.rendererFeatures.Add(interactive);
                renderer.rendererFeatures.Add(weather);
                renderer.SetDirty();
                var pipeline = Own(UniversalRenderPipelineAsset.Create(renderer));
                pipeline.supportsCameraDepthTexture = true;
                pipeline.supportsHDR = true;

                var cameraObject = Own(new GameObject("Test camera", typeof(Camera), typeof(VolumetricRainCamera)));
                var camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = new Vector3(300, .3f, 0);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 24;
                camera.cullingMask = 1 << TestLayer;
                var rain = cameraObject.GetComponent<VolumetricRainCamera>();
                var settings = rain.CreateRuntimeSettings();
                settings.renderMode = RainRenderMode.FogOnly;
                settings.extent = RainExtent.VolumesOnly;
                settings.maxDistance = 24;
                settings.hazeExtinction = .32f;
                settings.fogDensity = 1;
                settings.fogColor = Color.white;
                settings.fogScattering = settings.fogBrightness = 1;
                settings.noiseStrength = 0;
                settings.hazeSteps = 32;
                rain.freezeTime = true;

                var weatherBox = Own(new GameObject("Atmospheric slab")).AddComponent<RainVolume>();
                weatherBox.gameObject.layer = TestLayer;
                weatherBox.size = new Vector3(12, 10, 3);
                weatherBox.featherDistance = 0;
                var front = new Vector3(300, 0, 3);
                var back = new Vector3(300, 0, 14);
                weatherBox.transform.position = front;

                var ground = Own(new GameObject("Absorbing ground mist")).AddComponent<FogVolume>();
                ground.gameObject.layer = TestLayer;
                ground.transform.position = new Vector3(300, 0, 8);
                ground.simulation = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.jlinkdev.volumetric-fog-and-rain/Runtime/InteractiveFog/Shaders/FogFluid.compute");
                ground.resolution = new Vector3Int(24, 12, 32);
                ground.size = new Vector3(6, 2, 4);
                ground.fogHeight = 1.8f;
                ground.albedo = Color.black;
                ground.wispDisplacement = ground.detail = 0;
                ground.automaticSimulation = false;
                ground.Initialize();
                Assert.That(ground.IsReady, Is.True);

                var target = Own(new RenderTexture(ImageSize, ImageSize, 24, RenderTextureFormat.ARGBHalf));
                target.Create();
                var pixels = Own(new Texture2D(ImageSize, ImageSize, TextureFormat.RGBAFloat, false, true));
                GraphicsSettings.defaultRenderPipeline = QualitySettings.renderPipeline = pipeline;
                yield return null;
                Color Read(Color background)
                {
                    camera.backgroundColor = background;
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, ImageSize, ImageSize), 0, 0);
                    pixels.Apply();
                    return pixels.GetPixel(ImageSize / 2, ImageSize / 2);
                }

                // Recover atmospheric radiance and transmission independently from two backgrounds.
                ground.extinction = 0;
                float atmosphere = Read(Color.black).r;
                float weatherOnWhite = Read(Color.white).r;
                float transmission = weatherOnWhite - atmosphere;
                Assert.That(atmosphere, Is.InRange(.3f, .9f), "The reference slab must contain visible atmosphere.");
                foreach (var resolution in new[] { FogRenderResolution.Full, FogRenderResolution.Half, FogRenderResolution.Quarter })
                {
                    ground.extinction = .8f;
                    ground.renderResolution = resolution;
                    rain.intensity = 0;
                    float mistTransmission = Read(Color.white).r;
                    Assert.That(mistTransmission, Is.InRange(.005f, .75f), "The mist must measurably absorb the white background.");
                    rain.intensity = 1;
                    weatherBox.transform.position = front;
                    Assert.That(Read(Color.white).r, Is.EqualTo(atmosphere + transmission * mistTransmission).Within(.035f),
                        $"{resolution}: air in front of the mist must retain its scattered light.");
                    weatherBox.transform.position = back;
                    Assert.That(Read(Color.white).r, Is.EqualTo(mistTransmission * weatherOnWhite).Within(.035f),
                        $"{resolution}: air behind the mist must attenuate through the mist.");
                    weather.outputMode = FogOutputMode.Premultiplied;
                    Color transparent = Read(Color.clear);
                    Assert.That(transparent.r, Is.EqualTo(mistTransmission * atmosphere).Within(.035f),
                        $"{resolution}: scattered light on an empty target must stay premultiplied.");
                    Assert.That(transparent.a, Is.EqualTo(1 - transmission * mistTransmission).Within(.035f),
                        $"{resolution}: output opacity must account for both media.");
                    weather.outputMode = FogOutputMode.PreserveSceneAlpha;
                }

                // The pixel's opaque surface terminates both media before this ray reaches either box.
                var cover = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
                cover.layer = TestLayer;
                cover.transform.position = new Vector3(300, .3f, .9f);
                cover.transform.localScale = new Vector3(4, 4, .5f);
                weatherBox.transform.position = front;
                rain.intensity = ground.extinction = 0;
                Color surface = Read(Color.black);
                rain.intensity = 1;
                ground.extinction = .8f;
                Color occluded = Read(Color.black);
                for (int channel = 0; channel < 4; ++channel)
                    Assert.That(occluded[channel], Is.EqualTo(surface[channel]).Within(.005f),
                        "Atmosphere and mist behind opaque geometry must not leak through it.");
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = graphics;
                QualitySettings.renderPipeline = quality;
                RenderTexture.active = previousTarget;
                for (int i = objects.Count - 1; i >= 0; --i)
                    if (objects[i]) Object.DestroyImmediate(objects[i]);
                objects.Clear();
            }
        }
    }
}
