using System;
using System.IO;
using System.Linq;
using jlinkdev.UnityUtilities.WaterSystem;
using jlinkdev.UnityUtilities.WaterSystem.Samples;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.WaterSystem.Development
{
    /// <summary>Explicit batch validation entry points. Run only in a disposable test project.</summary>
    public static class WaterSystemValidation
    {
        static WaterLaboratoryController laboratory;
        static RenderPipelineAsset oldGraphics, oldQuality;
        static int frames;
        static readonly string CaptureDirectory = "WaterValidationCaptures";

        public static void RenderSmoke()
        {
            ShaderUtil.allowAsyncCompilation = false;
            EditorSceneManager.OpenScene(WaterSystemDevelopmentBuilder.Root + "/Scenes/Water Laboratory.unity");
            laboratory = UnityEngine.Object.FindFirstObjectByType<WaterLaboratoryController>();
            if (laboratory == null) throw new InvalidOperationException("Sample controller missing.");
            oldGraphics = GraphicsSettings.defaultRenderPipeline;
            oldQuality = QualitySettings.renderPipeline;
            GraphicsSettings.defaultRenderPipeline = laboratory.Pipeline;
            QualitySettings.renderPipeline = laboratory.Pipeline;
            frames = 0;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (++frames < 20) return;
            EditorApplication.update -= Tick;
            int exit = 0;
            try { CaptureAndValidate(); }
            catch (Exception exception) { Debug.LogException(exception); exit = 1; }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = oldGraphics;
                QualitySettings.renderPipeline = oldQuality;
            }
            EditorApplication.Exit(exit);
        }

        static void CaptureAndValidate()
        {
            Directory.CreateDirectory(CaptureDirectory);
            var camera = laboratory.ViewCamera;
            var body = laboratory.Water;
            var waterCamera = camera.GetComponent<WaterCamera>();
            var profile = UnityEngine.Object.Instantiate(body.Profile);
            body.Profile = profile;
            // Freeze wave motion, so numerical mask assertions are repeatable while testing curved geometry.
            for (int i = 0; i < profile.Waves.Length; i++) { var w = profile.Waves[i]; w.Speed = 0f; profile.Waves[i] = w; }
            camera.aspect = 16f / 9f;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            camera.transform.rotation = Quaternion.identity;
            camera.transform.position = new Vector3(0f, 2f, -8f);
            waterCamera.DebugMode = WaterDebugMode.UnderwaterMask;
            var dry = Capture(camera, "mask-above", 320, 180);
            Require(Mean(dry) < .01f, "Above-water mask must be dry.");
            camera.transform.position = new Vector3(0f, -2f, -8f);
            var wet = Capture(camera, "mask-below", 320, 180);
            Require(Mean(wet) > .99f, "Submerged mask must be wet.");
            float surface = body.Sample(new Vector3(0f, 0f, -8f + camera.nearClipPlane), body.SurfaceTime).Height;
            camera.transform.position = new Vector3(0f, surface, -8f);
            var split = Capture(camera, "mask-waterline", 320, 180);
            Require(Mean(split) > .1f && Mean(split) < .9f, "Intersecting camera must show both media.");
            int checkedPixels = 0, mismatches = 0;
            for (int y = 0; y < 180; y += 2)
                for (int x = 0; x < 320; x += 2)
                {
                    Vector3 point = camera.ViewportToWorldPoint(new Vector3((x + .5f) / 320, (y + .5f) / 180, camera.nearClipPlane));
                    float signed = body.Sample(point, body.SurfaceTime).Height - point.y;
                    if (Mathf.Abs(signed) < .003f) continue; // Omit intentional one-pixel coverage antialiasing.
                    bool expected = signed > 0f && body.ContainsXZ(point);
                    if ((split[y * 320 + x].r > 127) != expected) mismatches++;
                    checkedPixels++;
                }
            Require(mismatches < checkedPixels * .01f, $"GPU mask disagrees with CPU surface: {mismatches}/{checkedPixels}.");
            camera.orthographic = true; camera.orthographicSize = 2f;
            var ortho = Capture(camera, "mask-orthographic", 320, 180);
            Require(Mean(ortho) > .1f && Mean(ortho) < .9f, "Orthographic split must show both media.");
            camera.orthographic = false;
            camera.transform.rotation = Quaternion.Euler(0f, 0f, 30f);
            var rolled = Capture(camera, "mask-rolled", 320, 180);
            Require(Mean(rolled) > .1f && Mean(rolled) < .9f, "Rolled camera must retain the split.");
            camera.transform.rotation = Quaternion.identity;
            camera.transform.position = new Vector3(30f, -1f, -8f);
            Require(Mean(Capture(camera, "mask-outside", 320, 180)) < .01f, "Outside-volume camera must be dry.");
            camera.transform.position = new Vector3(0f, -7f, -8f);
            Require(Mean(Capture(camera, "mask-bottom", 320, 180)) < .01f, "Below-bottom camera must be dry.");
            waterCamera.DebugMode = WaterDebugMode.None;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camera.transform.position = new Vector3(0f, 2f, -8f);
            camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            Capture(camera, "above-water", 960, 540);
            camera.transform.rotation = Quaternion.identity;
            camera.transform.position = new Vector3(0f, surface, -8f);
            Capture(camera, "waterline", 960, 540);
            camera.transform.position = new Vector3(0f, -1.6f, -8f);
            camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            Capture(camera, "underwater", 960, 540);
            foreach (string shaderName in new[] { "WaterSurface", "WaterBoundary", "WaterComposite", "WaterParticles" })
            {
                var shader = Resources.Load<Shader>(shaderName);
                if (shader == null && shaderName == "WaterParticles") continue;
                Require(shader != null && shader.isSupported, "Missing/unsupported shader: " + shaderName);
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    Require(message.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error, shaderName + ": " + message.message);
            }
            string report = $"Unity {Application.unityVersion}; URP {UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UniversalRenderPipeline).Assembly).version}; " +
                $"{SystemInfo.operatingSystem}; {SystemInfo.graphicsDeviceType}; {SystemInfo.graphicsDeviceName}\n" +
                $"PASS dry, submerged, curved split, orthographic, rolled, outside, bottom masks. CPU/GPU mismatches {mismatches}/{checkedPixels}.\n";
            File.WriteAllText(Path.Combine(CaptureDirectory, "results.txt"), report);
            Debug.Log("[Water Validation] " + report);
            UnityEngine.Object.DestroyImmediate(profile);
        }

        static Color32[] Capture(Camera camera, string name, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = name, antiAliasing = 1 };
            target.Create();
            var previous = RenderTexture.active;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                VolumeManager.instance.Update(camera.transform, camera.GetUniversalAdditionalCameraData().volumeLayerMask);
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(CaptureDirectory, name + ".png"), image.EncodeToPNG());
                return image.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            }
        }
        static float Mean(Color32[] pixels) => pixels.Average(pixel => pixel.r / 255f);
        static void Require(bool result, string message) { if (!result) throw new InvalidOperationException(message); }

        public static void BuildPlayer()
        {
            EditorSceneManager.OpenScene(WaterSystemDevelopmentBuilder.Root + "/Scenes/Water Laboratory.unity");
            var controller = UnityEngine.Object.FindFirstObjectByType<WaterLaboratoryController>();
            GraphicsSettings.defaultRenderPipeline = controller.Pipeline;
            QualitySettings.renderPipeline = controller.Pipeline;
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { WaterSystemDevelopmentBuilder.Root + "/Scenes/Water Laboratory.unity" },
                locationPathName = "Builds/WaterLaboratory.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development });
            Require(report.summary.result == BuildResult.Succeeded, "Water laboratory player build failed.");
            Debug.Log("[Water Validation] Player build succeeded: " + report.summary.totalSize + " bytes.");
        }
    }
}
