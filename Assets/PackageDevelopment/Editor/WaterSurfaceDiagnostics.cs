using System;
using System.IO;
using System.Text;
using jlinkdev.UnityUtilities.WaterSystem.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.WaterSystem.Development
{
    /// <summary>Host-only disposable-project diagnostic. Never run in an interactive development project.</summary>
    public static class WaterSurfaceDiagnostics
    {
        const string Output = "WaterSurfaceDiagnostics";
        static WaterLaboratoryController laboratory;
        static RenderPipelineAsset oldGraphics, oldQuality;
        static int frames;

        public static void Run()
        {
            EditorSceneManager.OpenScene(WaterSystemDevelopmentBuilder.Root + "/Scenes/Water Laboratory.unity");
            laboratory = UnityEngine.Object.FindFirstObjectByType<WaterLaboratoryController>();
            if (laboratory == null) throw new InvalidOperationException("Water laboratory not found.");
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
            int result = 0;
            try { Diagnose(); }
            catch (Exception exception) { Debug.LogException(exception); result = 1; }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = oldGraphics;
                QualitySettings.renderPipeline = oldQuality;
            }
            EditorApplication.Exit(result);
        }

        static void Diagnose()
        {
            Directory.CreateDirectory(Output);
            var camera = laboratory.ViewCamera;
            var body = laboratory.Water;
            var waterCamera = camera.GetComponent<WaterCamera>();
            var additional = camera.GetUniversalAdditionalCameraData();
            var originalProfile = body.Profile;
            var profile = UnityEngine.Object.Instantiate(originalProfile);
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            float oldAspect = camera.aspect;
            bool oldPost = additional.renderPostProcessing;
            bool oldEnabled = body.SurfaceRenderer.enabled;
            var oldDebug = waterCamera.DebugMode;
            var report = new StringBuilder();
            try
            {
                body.Profile = profile;
                for (int i = 0; i < profile.Waves.Length; i++)
                {
                    var wave = profile.Waves[i]; wave.Speed = 0;
                    profile.Waves[i] = wave;
                }
                camera.aspect = 16f / 9f;
                camera.transform.SetPositionAndRotation(new Vector3(0, 2, -8), Quaternion.Euler(12, 0, 0));
                waterCamera.DebugMode = WaterDebugMode.None;
                additional.renderPostProcessing = false;
                var baseline = Capture(camera, "above-baseline");
                AppendState(report, body, "baseline");

                profile.FoamStrength = 0;
                profile.DetailNormalStrength = 0;
                profile.Refraction = 0;
                var flat = Capture(camera, "above-nofoam-flat");
                AppendState(report, body, "nofoam-flat");

                profile.Absorption = Vector3.one * 10;
                profile.ShallowColor = Color.red;
                profile.DeepColor = Color.red;
                profile.Density = 1;
                var red = Capture(camera, "above-thick-red");
                AppendState(report, body, "thick-red");

                body.SurfaceRenderer.enabled = false;
                var absent = Capture(camera, "above-nosurface");
                report.AppendLine("Mean absolute RGB difference (0..1):");
                report.AppendLine("baseline / nofoam-flat = " + Difference(baseline, flat));
                report.AppendLine("baseline / thick-red = " + Difference(baseline, red));
                report.AppendLine("baseline / no-surface = " + Difference(baseline, absent));
                double absorptionDifference = Difference(flat, red);
                report.AppendLine("nofoam-flat / thick-red = " + absorptionDifference);
                body.SurfaceRenderer.enabled = oldEnabled;
                double orderDifference = CheckSurfaceOrder(camera, body, originalProfile);
                report.AppendLine("grazing forward / reversed triangle order = " + orderDifference);
                File.WriteAllText(Path.Combine(Output, "results.txt"), report.ToString());
                Debug.Log("[Water Surface Diagnostics]\n" + report);
                if (absorptionDifference <= .02)
                    throw new InvalidOperationException("Water surface absorption did not respond to the profile. Verify depth is available before transparent rendering.");
                if (orderDifference >= .001)
                    throw new InvalidOperationException("Water surface color depends on triangle submission order at grazing incidence.");
            }
            finally
            {
                body.SurfaceRenderer.enabled = oldEnabled;
                body.Profile = originalProfile;
                waterCamera.DebugMode = oldDebug;
                additional.renderPostProcessing = oldPost;
                camera.aspect = oldAspect;
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        static double CheckSurfaceOrder(Camera camera, WaterBody body, WaterProfile sourceProfile)
        {
            var previousProfile = body.Profile;
            var profile = UnityEngine.Object.Instantiate(sourceProfile);
            var mesh = body.SurfaceMesh;
            int[] originalIndices = mesh.triangles;
            Bounds originalBounds = mesh.bounds;
            try
            {
                // Freeze every visual animation while retaining waves steep enough to overlap along a view ray.
                profile.Waves = new[] { new WaveSettings(90, .5f, 3f, 0), new WaveSettings(35, .15f, 1.7f, 0, 1.2f) };
                profile.DetailNormalStrength = 0;
                profile.FoamStrength = 0;
                profile.CausticsSpeed = 0;
                profile.Distortion = 0;
                body.Profile = profile;
                camera.transform.SetPositionAndRotation(new Vector3(0, .22f, -8), Quaternion.Euler(2, 0, 0));
                var forward = Capture(camera, "grazing-order-forward");
                var reversedIndices = new int[originalIndices.Length];
                for (int i = 0; i < originalIndices.Length; i += 3)
                {
                    int source = originalIndices.Length - 3 - i;
                    reversedIndices[i] = originalIndices[source];
                    reversedIndices[i + 1] = originalIndices[source + 1];
                    reversedIndices[i + 2] = originalIndices[source + 2];
                }
                mesh.triangles = reversedIndices; // Reverse submission order only; preserve every triangle's winding.
                var reversed = Capture(camera, "grazing-order-reversed");
                return Difference(forward, reversed);
            }
            finally
            {
                mesh.triangles = originalIndices;
                mesh.bounds = originalBounds;
                body.Profile = previousProfile;
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        static void AppendState(StringBuilder report, WaterBody body, string label)
        {
            var material = body.SurfaceMaterial;
            var block = new MaterialPropertyBlock();
            body.SurfaceRenderer.GetPropertyBlock(block);
            report.AppendLine(label + ": shader=" + material.shader.name + "; asset=" + AssetDatabase.GetAssetPath(material.shader));
            report.AppendLine("  block density=" + block.GetFloat("_WaterDensity") + "; refraction=" + block.GetFloat("_WaterRefraction") +
                "; waves=" + block.GetInt("_WaterWaveCount") + "; deep=" + block.GetColor("_WaterDeepColor"));
            report.AppendLine("  profile density=" + body.Profile.Density + "; refraction=" + body.Profile.Refraction);
            report.AppendLine("  material instance=" + material.GetInstanceID() + "; renderer material instance=" +
                body.SurfaceRenderer.sharedMaterial.GetInstanceID() + "; queue=" + material.renderQueue +
                "; renderer enabled=" + body.SurfaceRenderer.enabled + "; mesh vertices=" + body.SurfaceMesh.vertexCount);
        }

        static Color32[] Capture(Camera camera, string name)
        {
            const int width = 960, height = 540;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = name, antiAliasing = 1 };
            target.Create();
            var previous = RenderTexture.active;
            var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Output, name + ".png"), pixels.EncodeToPNG());
                return pixels.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }

        static double Difference(Color32[] a, Color32[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b);
            return sum / (a.Length * 3.0 * 255.0);
        }
    }
}
