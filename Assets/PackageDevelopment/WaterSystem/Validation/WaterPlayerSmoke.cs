using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using jlinkdev.UnityUtilities.WaterSystem.EditableSamples;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.WaterSystem.Development
{
    /// <summary>Host-only, opt-in standalone validation. Never runs without -water-smoke.</summary>
    public sealed class WaterPlayerSmoke : MonoBehaviour
    {
        [Serializable]
        private sealed class CaptureResult
        {
            public string name;
            public string file;
            public float meanRed;
            public string cameraState;
        }

        [Serializable]
        private sealed class PropResult
        {
            public string name;
            public Vector3 position;
            public Vector3 linearVelocity;
            public Vector3 angularVelocity;
            public int activeFloatPoints;
        }

        [Serializable]
        private sealed class SmokeResult
        {
            public bool passed;
            public string unityVersion;
            public string graphicsApi;
            public string graphicsDevice;
            public string operatingSystem;
            public float elapsedSeconds;
            public int physicsObservations;
            public int maskComparedPixels;
            public int maskMismatches;
            public List<string> supportedShaders = new List<string>();
            public List<PropResult> floatingProps = new List<PropResult>();
            public List<CaptureResult> captures = new List<CaptureResult>();
            public List<string> errors = new List<string>();
        }

        private readonly SmokeResult result = new SmokeResult();
        private string outputDirectory;
        private WaterLaboratoryController laboratory;
        private WaterProfile frozenProfile;
        private WaterProfile originalProfile;
        private float startedAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (Array.FindIndex(arguments, value => string.Equals(value, "-water-smoke", StringComparison.OrdinalIgnoreCase)) < 0) return;
            new GameObject("Water Player Smoke (Explicit CLI)").AddComponent<WaterPlayerSmoke>();
        }

        private IEnumerator Start()
        {
            startedAt = Time.realtimeSinceStartup;
            result.unityVersion = Application.unityVersion;
            result.graphicsApi = SystemInfo.graphicsDeviceType.ToString();
            result.graphicsDevice = SystemInfo.graphicsDeviceName;
            result.operatingSystem = SystemInfo.operatingSystem;
            Application.logMessageReceived += RecordLog;
            Application.runInBackground = true;
            IEnumerator sequence = Run();
            // Catch failures from every coroutine step, including warm-up physics checks.
            while (true)
            {
                object instruction;
                try
                {
                    if (!sequence.MoveNext()) break;
                    instruction = sequence.Current;
                }
                catch (Exception exception)
                {
                    result.errors.Add(exception.ToString());
                    break;
                }
                yield return instruction;
            }
            Finish();
        }

        private IEnumerator Run()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int outputIndex = Array.FindIndex(arguments, value => string.Equals(value, "-water-output", StringComparison.OrdinalIgnoreCase));
            Require(outputIndex >= 0 && outputIndex + 1 < arguments.Length, "Pass -water-output followed by an absolute output directory.");
            string requestedOutput = arguments[outputIndex + 1];
            Require(Path.IsPathRooted(requestedOutput), "The -water-output directory must be absolute.");
            outputDirectory = Path.GetFullPath(requestedOutput);
            Directory.CreateDirectory(outputDirectory);
            laboratory = FindFirstObjectByType<WaterLaboratoryController>();
            Require(laboratory != null, "Water Laboratory controller is missing from the player scene.");
            Require(laboratory.Water != null && laboratory.ViewCamera != null, "Laboratory references are incomplete.");
            Require(laboratory.FloatingProps != null && laboratory.FloatingProps.Length == 3, "Expected exactly three configured floating props.");
            Require(laboratory.ViewCamera.TryGetComponent<WaterCameraPass>(out var local) && local.feature, "The sample camera needs its water feature.");

            float warmupRealtime = Time.realtimeSinceStartup;
            float warmupSimulation = Time.time;
            while (Time.realtimeSinceStartup - warmupRealtime < 3f || Time.time - warmupSimulation < 3f)
            {
                Require(Time.realtimeSinceStartup - warmupRealtime < 45f, "Physics warm-up did not advance three simulation seconds within 45 seconds.");
                ValidateFloatingProps(false);
                yield return null;
            }
            ValidateFloatingProps(true);

            // Position the camera independently of the interactive controls during captures.
            laboratory.enabled = false;
            yield return null;
            yield return null;

            WaterBody body = laboratory.Water;
            Camera camera = laboratory.ViewCamera;
            WaterCamera waterCamera = camera.GetComponent<WaterCamera>();
            Require(waterCamera != null, "The sample camera must include WaterCamera.");
            waterCamera.BodyOverride = body;
            originalProfile = body.Profile;
            frozenProfile = Instantiate(originalProfile);
            for (int i = 0; i < frozenProfile.Waves.Length; i++)
            {
                WaveSettings wave = frozenProfile.Waves[i];
                wave.Speed = 0f;
                frozenProfile.Waves[i] = wave;
            }
            body.Profile = frozenProfile;
            body.Rebuild();
            camera.aspect = 16f / 9f;
            camera.orthographic = false;
            camera.transform.rotation = Quaternion.identity;
            Vector3 viewOrigin = body.transform.position + new Vector3(0f, 0f, -8f);
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = false; // ACES must not transform the numeric mask.
            waterCamera.DebugMode = WaterDebugMode.UnderwaterMask;
            camera.transform.position = viewOrigin + Vector3.up * 2f;
            Require(Mean(Capture(camera, "mask-above", 320, 180)) < 0.01f, "Above-water mask is not dry.");
            camera.transform.position = viewOrigin - Vector3.up * 2f;
            Require(Mean(Capture(camera, "mask-below", 320, 180)) > 0.99f, "Submerged mask is not wet.");
            float surface = body.Sample(viewOrigin + Vector3.forward * camera.nearClipPlane, body.SurfaceTime).Height;
            camera.transform.position = new Vector3(viewOrigin.x, surface, viewOrigin.z);
            Color32[] split = Capture(camera, "mask-waterline", 320, 180);
            float splitMean = Mean(split);
            Require(splitMean > 0.1f && splitMean < 0.9f, "Waterline camera must show both media.");
            ValidateMaskAgreement(body, camera, split, 320, 180);

            cameraData.renderPostProcessing = true;
            waterCamera.DebugMode = WaterDebugMode.None;
            camera.transform.position = viewOrigin + Vector3.up * 2f;
            camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            Capture(camera, "above-water", 960, 540);
            camera.transform.position = new Vector3(viewOrigin.x, surface, viewOrigin.z);
            camera.transform.rotation = Quaternion.identity;
            Capture(camera, "waterline", 960, 540);
            camera.transform.position = viewOrigin - Vector3.up * 1.6f;
            camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            Capture(camera, "underwater", 960, 540);
            foreach (string resource in new[] { "WaterSurface", "WaterBoundary", "WaterComposite", "WaterParticles" })
            {
                Shader shader = Resources.Load<Shader>(resource);
                Require(shader != null && shader.isSupported, "Missing or unsupported player shader: " + resource);
                result.supportedShaders.Add(shader.name);
            }
            yield return null; // Allow any deferred render errors to reach the logger before final status.
        }

        private void ValidateFloatingProps(bool record)
        {
            WaterBody water = laboratory.Water;
            foreach (Rigidbody prop in laboratory.FloatingProps)
            {
                Require(prop != null, "A configured floating prop is missing.");
                Require(Finite(prop.position) && Finite(prop.linearVelocity) && Finite(prop.angularVelocity), "Non-finite floating prop state: " + prop.name);
                Require(water.ContainsXZ(prop.position), "Floating prop left the water footprint: " + prop.name);
                Require(prop.position.y >= water.transform.position.y - water.Depth &&
                    prop.position.y <= water.transform.position.y + water.Profile.MaximumDisplacement + 2f,
                    "Floating prop left the water's vertical envelope: " + prop.name);
                Require(prop.linearVelocity.sqrMagnitude < 2500f, "Floating prop speed exceeds the smoke-test stability bound: " + prop.name);
                WaterBuoyancy buoyancy = prop.GetComponent<WaterBuoyancy>();
                Require(buoyancy != null && buoyancy.isActiveAndEnabled && !prop.isKinematic, "Floating prop is not running buoyancy: " + prop.name);
                if (record) result.floatingProps.Add(new PropResult {
                    name = prop.name, position = prop.position, linearVelocity = prop.linearVelocity,
                    angularVelocity = prop.angularVelocity, activeFloatPoints = buoyancy.ActivePointCount });
            }
            result.physicsObservations++;
        }

        private void ValidateMaskAgreement(WaterBody body, Camera camera, Color32[] pixels, int width, int height)
        {
            for (int y = 0; y < height; y += 2)
                for (int x = 0; x < width; x += 2)
                {
                    Vector3 point = camera.ViewportToWorldPoint(new Vector3((x + 0.5f) / width, (y + 0.5f) / height, camera.nearClipPlane));
                    float signedDepth = body.Sample(point, body.SurfaceTime).Height - point.y;
                    if (Mathf.Abs(signedDepth) < 0.003f) continue; // Ignore the intentionally antialiased boundary band.
                    bool expected = signedDepth > 0f && body.ContainsXZ(point) && point.y >= body.transform.position.y - body.Depth;
                    if ((pixels[y * width + x].r > 127) != expected) result.maskMismatches++;
                    result.maskComparedPixels++;
                }
            Require(result.maskComparedPixels > 1000 && result.maskMismatches < result.maskComparedPixels * 0.01f,
                "GPU/CPU mask mismatch: " + result.maskMismatches + "/" + result.maskComparedPixels);
        }

        private Color32[] Capture(Camera camera, string name, int width, int height)
        {
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = name, antiAliasing = 1 };
            Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Require(target.Create(), "Failed to allocate capture target: " + name);
                camera.GetComponent<WaterCamera>().RefreshState();
                VolumeManager.instance.Update(camera.transform, camera.GetUniversalAdditionalCameraData().volumeLayerMask);
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                string file = Path.Combine(outputDirectory, name + ".png");
                File.WriteAllBytes(file, image.EncodeToPNG());
                Color32[] pixels = image.GetPixels32();
                result.captures.Add(new CaptureResult { name = name, file = file, meanRed = Mean(pixels), cameraState = camera.GetComponent<WaterCamera>().State.ToString() });
                return pixels;
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Destroy(target);
                Destroy(image);
            }
        }

        private void RecordLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                result.errors.Add(type + ": " + condition + "\n" + stackTrace);
        }

        private void Finish()
        {
            Application.logMessageReceived -= RecordLog;
            if (laboratory != null && originalProfile != null) laboratory.Water.Profile = originalProfile;
            if (frozenProfile != null) Destroy(frozenProfile);
            result.elapsedSeconds = Time.realtimeSinceStartup - startedAt;
            result.passed = result.errors.Count == 0;
            string json = JsonUtility.ToJson(result, true);
            var summary = new StringBuilder();
            summary.AppendLine(result.passed ? "PASS Water standalone player smoke" : "FAIL Water standalone player smoke");
            summary.AppendLine("Unity " + result.unityVersion + "; " + result.graphicsApi + "; " + result.graphicsDevice);
            summary.AppendLine("Physics observations: " + result.physicsObservations + "; props: " + result.floatingProps.Count);
            summary.AppendLine("Mask mismatch: " + result.maskMismatches + "/" + result.maskComparedPixels);
            foreach (CaptureResult capture in result.captures) summary.AppendLine(capture.name + ": mean red " + capture.meanRed.ToString("F4") + "; " + capture.cameraState);
            foreach (string error in result.errors) summary.AppendLine(error);
            try
            {
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    File.WriteAllText(Path.Combine(outputDirectory, "player-results.json"), json);
                    File.WriteAllText(Path.Combine(outputDirectory, "player-results.txt"), summary.ToString());
                }
            }
            catch (Exception exception)
            {
                result.passed = false;
                summary.AppendLine("Writing validation results failed: " + exception);
            }
            Debug.Log("[Water Player Smoke] " + summary);
            Application.Quit(result.passed ? 0 : 1);
        }

        private static float Mean(Color32[] pixels)
        {
            double total = 0;
            foreach (Color32 pixel in pixels) total += pixel.r;
            return (float)(total / (pixels.Length * 255.0));
        }

        private static bool Finite(Vector3 vector) => Finite(vector.x) && Finite(vector.y) && Finite(vector.z);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
