using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace jlinkdev.UnityUtilities.VolumetricRain.Tests
{
    public class RainRenderTests
    {
        [UnityTest]
        public IEnumerator RenderGraphProducesStableRainAndRespectsDepth()
        {
            var oldPipeline = GraphicsSettings.defaultRenderPipeline;
            var oldQuality = QualitySettings.renderPipeline;
            int oldAA = QualitySettings.antiAliasing;
            var previousScene = SceneManager.GetActiveScene();
            var scene = previousScene;
            SceneManager.SetActiveScene(scene);
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            var feature = ScriptableObject.CreateInstance<RainRendererFeature>();
            renderer.rendererFeatures.Add(feature);
            feature.Create(); renderer.SetDirty();
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.supportsCameraDepthTexture = true;
            pipeline.msaaSampleCount = 1;
            var profile = ScriptableObject.CreateInstance<RainProfile>();
            profile.noiseStrength = 0;
            var go = new GameObject("Rain Render Test", typeof(Camera), typeof(VolumetricRainCamera));
            var camera = go.GetComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.04f, 0.06f, 1);
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 150;
            var rain = go.GetComponent<VolumetricRainCamera>();
            rain.profile = profile; rain.freezeTime = true; rain.fixedTime = 2;
            var target = new RenderTexture(384, 216, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var readback = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, true);
            var oldActive = RenderTexture.active;
            Material wallMaterial = null;
            GameObject wall = null;
            var volumes = new System.Collections.Generic.List<GameObject>();
            try
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                yield return null;
                yield return null;
                var clear = Capture(camera, target, readback, rain, 0);
                var wet = Capture(camera, target, readback, rain, 1);
                Assert.That(Difference(clear, wet), Is.GreaterThan(0.01), "Rain pass must alter a sky-only image.");
                var stable = Capture(camera, target, readback, rain, 1);
                Assert.That(Difference(wet, stable), Is.LessThan(0.00001), "Frozen field must be repeatable.");
                Directory.CreateDirectory("Logs/VolumetricRain");
                File.WriteAllBytes("Logs/VolumetricRain/rain-perspective.png", readback.EncodeToPNG());
                rain.fixedTime += 0.05f;
                var animated = Capture(camera, target, readback, rain, 1);
                Assert.That(Difference(wet, animated), Is.GreaterThan(0.0001), "Analytic field must animate.");
                camera.transform.position += Vector3.right * 0.5f;
                var translated = Capture(camera, target, readback, rain, 1);
                Assert.That(Difference(animated, translated), Is.GreaterThan(0.0001), "Field must exhibit parallax.");
                camera.orthographic = true; camera.orthographicSize = 4;
                var ortho = Capture(camera, target, readback, rain, 1);
                var orthoClear = Capture(camera, target, readback, rain, 0);
                Assert.That(Difference(ortho, orthoClear), Is.GreaterThan(0.01), "Orthographic rays must render rain.");
                camera.orthographic = false;
                camera.transform.position = Vector3.zero;
                rain.fixedTime = 2;
                profile.extent = RainExtent.VolumesOnly;
                clear = Capture(camera, target, readback, rain, 0);
                var empty = Capture(camera, target, readback, rain, 1);
                Assert.That(Difference(clear, empty), Is.EqualTo(0), "Bounded mode without a volume must be dry.");
                var volume = new GameObject("Wet test box").AddComponent<RainVolume>();
                volumes.Add(volume.gameObject);
                volume.transform.position = new Vector3(0, 0, 14);
                volume.size = new Vector3(8, 8, 24);
                var bounded = Capture(camera, target, readback, rain, 1);
                Assert.That(Difference(clear, bounded), Is.GreaterThan(0.001), "Camera outside a rain box must see its contents.");
                var overlap = new GameObject("Overlapping wet box").AddComponent<RainVolume>();
                volumes.Add(overlap.gameObject);
                overlap.transform.position = volume.transform.position; overlap.size = volume.size;
                Assert.That(Difference(bounded, Capture(camera, target, readback, rain, 1)), Is.EqualTo(0), "Overlapping rain boxes must not double contribution.");
                overlap.enabled = false;
                var dry = new GameObject("Dry test box").AddComponent<RainExclusionVolume>();
                volumes.Add(dry.gameObject);
                dry.transform.position = volume.transform.position; dry.size = volume.size * 2;
                Assert.That(Difference(clear, Capture(camera, target, readback, rain, 1)), Is.EqualTo(0), "An exclusion covering a rain box must remove streaks AND haze.");
                dry.transform.position = Vector3.zero; dry.size = new Vector3(10, 10, 10);
                var sheltered = Capture(camera, target, readback, rain, 1);
                Assert.That(Difference(clear, sheltered), Is.GreaterThan(0.001), "Inside a dry box, rain outside must remain visible.");
                Assert.That(Difference(bounded, sheltered), Is.GreaterThan(0.0001), "The dry box must remove foreground rain.");
                Directory.CreateDirectory("Logs/VolumetricRain");
                File.WriteAllBytes("Logs/VolumetricRain/rain-volumes.png", readback.EncodeToPNG());
                var secondDry = new GameObject("Overlapping dry box").AddComponent<RainExclusionVolume>();
                volumes.Add(secondDry.gameObject);
                secondDry.transform.position = dry.transform.position; secondDry.size = dry.size;
                Assert.That(Difference(sheltered, Capture(camera, target, readback, rain, 1)), Is.EqualTo(0), "Overlapping exclusions must behave as a union.");
                secondDry.enabled = false;
                // Exclusion volumes must also work when the field is unbounded.
                profile.extent = RainExtent.Unbounded;
                dry.size = Vector3.one * 400;
                Assert.That(Difference(clear, Capture(camera, target, readback, rain, 1)), Is.EqualTo(0), "Unbounded rain must respect exclusions too.");
                profile.extent = RainExtent.VolumesOnly;
                dry.enabled = false;
                volume.gameObject.layer = 2;
                camera.cullingMask = ~(1 << 2);
                Assert.That(Difference(clear, Capture(camera, target, readback, rain, 1)), Is.EqualTo(0), "Camera layers must filter volume registration.");
                camera.cullingMask = -1; volume.gameObject.layer = 0;
                // Five visible inclusions must fail closed instead of silently dropping a box.
                var excess = new System.Collections.Generic.List<RainVolume>();
                for (int i = 0; i < 4; i++)
                {
                    var box = new GameObject("Capacity test box").AddComponent<RainVolume>();
                    box.transform.position = volume.transform.position; box.size = volume.size;
                    volumes.Add(box.gameObject); excess.Add(box);
                }
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Volumetric Rain supports at most 4 visible rain boxes"));
                Assert.That(Difference(clear, Capture(camera, target, readback, rain, 1)), Is.EqualTo(0), "Capacity overflow must skip rain safely.");
                foreach (var box in excess) box.enabled = false;
                // Transforms, parallel rays and an exactly known silhouette.
                dry.enabled = false;
                camera.orthographic = true; camera.orthographicSize = 4;
                volume.transform.SetPositionAndRotation(new Vector3(-1, 0, 14), Quaternion.Euler(0, 30, 0));
                volume.transform.localScale = new Vector3(-1, 1.5f, 0.8f);
                volume.size = new Vector3(2, 2, 4);
                var rotatedClear = Capture(camera, target, readback, rain, 0);
                var rotated = Capture(camera, target, readback, rain, 1);
                Assert.That(Difference(rotatedClear, rotated), Is.GreaterThan(0.0001), "Rotated, negatively scaled box must render with parallel orthographic rays.");
                for (int y = 0; y < target.height; y++)
                    Assert.That(rotated[y * target.width + target.width - 1], Is.EqualTo(rotatedClear[y * target.width + target.width - 1]), "Pixels outside the volume silhouette must stay dry.");
                // A distant thin volume must retain haze even when thinner than the usual sample spacing.
                camera.orthographic = false;
                volume.transform.SetPositionAndRotation(new Vector3(0, 0, 50), Quaternion.identity);
                volume.transform.localScale = Vector3.one; volume.size = new Vector3(200, 200, 0.1f);
                profile.hazeExtinction = 1;
                rain.debugView = RainDebugView.Haze;
                Assert.That(Difference(Capture(camera, target, readback, rain, 1), new Color32[target.width * target.height]), Is.GreaterThan(0.001), "Thin distant volumes must contribute haze.");
                // A deliberately late fade start must not be forced to 65% of Far Distance.
                volume.size = new Vector3(2, 2, 0.1f);
                profile.midDistance = 55; profile.farDistance = 70;
                profile.cellSize = 1; profile.maxCellSteps = 256;
                Assert.That(Difference(Capture(camera, target, readback, rain, 1), new Color32[target.width * target.height]), Is.EqualTo(0), "A slab at 50 m must have no haze when the authored Mid Distance is 55 m and the budget is sufficient.");
                profile.midDistance = 12; profile.farDistance = 30;
                profile.cellSize = .65f; profile.maxCellSteps = 96;
                rain.debugView = RainDebugView.Composite; profile.hazeExtinction = 0.015f;
                volume.enabled = false;
                profile.extent = RainExtent.Unbounded;
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = new Vector3(0, 0, 0.16f);
                wall.transform.localScale = new Vector3(10, 10, 0.02f);
                wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                wallMaterial.color = Color.gray;
                wall.GetComponent<Renderer>().sharedMaterial = wallMaterial;
                profile.nearFade = 5;
                var wallClear = Capture(camera, target, readback, rain, 0);
                var wallWet = Capture(camera, target, readback, rain, 1);
                Assert.That(Difference(wallClear, wallWet), Is.LessThan(0.002), "Opaque near wall must occlude rain behind it.");
                foreach (var message in ShaderUtil.GetShaderMessages(Shader.Find("Hidden/jlinkdev/Volumetric Rain")))
                    Assert.That(message.severity.ToString(), Is.Not.EqualTo("Error"), message.message);
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = oldPipeline;
                QualitySettings.renderPipeline = oldQuality;
                QualitySettings.antiAliasing = oldAA;
                RenderTexture.active = oldActive;
                foreach (var box in volumes) Object.DestroyImmediate(box);
                Object.DestroyImmediate(go);
                if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
                Object.DestroyImmediate(readback); target.Release(); Object.DestroyImmediate(target);
                Object.DestroyImmediate(wall); Object.DestroyImmediate(wallMaterial); Object.DestroyImmediate(profile);
                Object.DestroyImmediate(pipeline); Object.DestroyImmediate(feature); Object.DestroyImmediate(renderer);
            }
        }

        private static Color32[] Capture(Camera camera, RenderTexture target, Texture2D pixels, VolumetricRainCamera rain, float intensity)
        {
            rain.intensity = intensity;
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            return pixels.GetPixels32();
        }

        private static double Difference(Color32[] a, Color32[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += System.Math.Abs(a[i].r - b[i].r) + System.Math.Abs(a[i].g - b[i].g) + System.Math.Abs(a[i].b - b[i].b);
            return sum / (a.Length * 3 * 255.0);
        }
    }
}
