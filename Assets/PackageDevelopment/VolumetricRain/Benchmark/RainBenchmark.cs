using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using jlinkdev.UnityUtilities.VolumetricRain;

public class RainBenchmark
{
    [UnityTest]
    public IEnumerator Measure()
    {
        string variant = Environment.GetEnvironmentVariable("RAIN_BENCH_VARIANT") ?? "Final";
        if (variant == "Compare")
        {
            yield return MeasureVariant("Reference");
            yield return MeasureVariant("Final");
        }
        else yield return MeasureVariant(variant);
    }

    private IEnumerator MeasureVariant(string variant)
    {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);

        string output = Environment.GetEnvironmentVariable("RAIN_BENCH_OUTPUT") ?? "Logs/RainPerformance";
        Directory.CreateDirectory(output);
        var oldGraphics = GraphicsSettings.defaultRenderPipeline;
        var oldQuality = QualitySettings.renderPipeline;
        int oldAA = QualitySettings.antiAliasing;
        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        var feature = ScriptableObject.CreateInstance<RainRendererFeature>();
        if (variant == "Reference")
        {
            var shader = Shader.Find("Hidden/jlinkdev/Volumetric Rain Reference");
            Assert.NotNull(shader);
            typeof(RainRendererFeature).GetField("shader", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(feature, shader);
        }
        renderer.rendererFeatures.Add(feature); feature.Create(); renderer.SetDirty();
        var pipeline = UniversalRenderPipelineAsset.Create(renderer);
        pipeline.supportsCameraDepthTexture = true; pipeline.msaaSampleCount = 1;
        var profile = ScriptableObject.CreateInstance<RainProfile>();
        var go = new GameObject("Rain Benchmark", typeof(Camera), typeof(VolumetricRainCamera));
        var camera = go.GetComponent<Camera>(); camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f,.045f,.06f);
        camera.transform.SetPositionAndRotation(new Vector3(0,2.5f,-9), Quaternion.Euler(5,0,0));
        camera.farClipPlane = 150;
        var rain = go.GetComponent<VolumetricRainCamera>(); rain.profile = profile; rain.freezeTime = true; rain.fixedTime = 2;
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.color = new Color(.18f,.22f,.26f);
        var objects = new List<GameObject>();
        Action<Vector3,Vector3> box = (p,s) => { var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.transform.position=p; b.transform.localScale=s; b.GetComponent<Renderer>().sharedMaterial=material; objects.Add(b); };
        box(new Vector3(0,-.25f,30),new Vector3(60,.5f,100));
        box(new Vector3(-2,2,1),new Vector3(2,4,1));
        box(new Vector3(3,3,14),new Vector3(3,6,2));
        box(new Vector3(0,5,55),new Vector3(32,10,1));
                var wetBox = new GameObject("Benchmark rain volume").AddComponent<RainVolume>();
        wetBox.transform.position = new Vector3(0, 8, 20); wetBox.size = new Vector3(30, 20, 40); wetBox.enabled = false;
        var dryBox = new GameObject("Benchmark exclusion").AddComponent<RainExclusionVolume>();
        dryBox.transform.position = camera.transform.position; dryBox.enabled = false;
        objects.Add(wetBox.gameObject); objects.Add(dryBox.gameObject);
        var target = new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32); target.Create();
        var pixels = new Texture2D(1920,1080,TextureFormat.RGBA32,false,true);
        var oldActive = RenderTexture.active;
        Recorder recorder = null;
        var rows = new List<string> { "variant,scenario,gpu_samples,gpu_median_ms,gpu_p95_ms,synchronized_render_median_ms" };
        try
        {
            GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline;
            yield return null; yield return null;
            var scenarios = new List<string> { "default", "no_noise", "large_cells", "sparse", "no_haze", "off" };
            if (variant != "Reference") scenarios.AddRange(new[] { "bounded", "sheltered", "fully_dry", "fog_global", "fog_bounded" });
            foreach (string scenario in scenarios)
            {
                                profile.renderMode = scenario.StartsWith("fog_") ? RainRenderMode.FogOnly : RainRenderMode.RainAndFog;
                wetBox.enabled = scenario == "bounded" || scenario == "fog_bounded";
                profile.extent = wetBox.enabled ? RainExtent.VolumesOnly : RainExtent.Unbounded;
                dryBox.enabled = scenario == "sheltered" || scenario == "fully_dry";
                dryBox.size = scenario == "fully_dry" ? Vector3.one * 300 : new Vector3(10, 6, 10);
                profile.noiseStrength=scenario=="no_noise" ? 0 : .35f;
                profile.cellSize=scenario=="large_cells" ? 1.3f : .65f;
                profile.density=scenario=="sparse" ? .2f : .8f;
                profile.hazeExtinction=scenario=="no_haze" ? 0 : .015f;
                rain.intensity=scenario=="off" ? 0 : 1;
                var gpu = new List<double>(); var sync = new List<double>();
                for (int i=0;i<420;i++)
                {
                    var sw=System.Diagnostics.Stopwatch.StartNew();
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
                    // A 1px readback fences GPU completion. Report separately from true GPU scope timing.
                    RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,1,1),0,0); pixels.Apply();
                    sw.Stop();
                    if (recorder==null) { recorder=Recorder.Get("Volumetric Rain"); recorder.enabled=true; }
                    yield return null;
                    if (i>=120)
                    {
                        sync.Add(sw.Elapsed.TotalMilliseconds);
                        if (recorder.gpuSampleBlockCount>0 && recorder.gpuElapsedNanoseconds>0)
                            gpu.Add(recorder.gpuElapsedNanoseconds/1000000.0/recorder.gpuSampleBlockCount);
                    }
                }
                gpu.Sort(); sync.Sort();
                Func<List<double>,double,double> percentile=(v,q)=>v.Count==0 ? double.NaN : v[(int)((v.Count-1)*q)];
                rows.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3:F4},{4:F4},{5:F4}",variant,scenario,gpu.Count,percentile(gpu,.5),percentile(gpu,.95),percentile(sync,.5)));
                File.WriteAllLines(Path.Combine(output,variant+".csv"),rows);
                RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(output,variant+"-"+scenario+".png"),pixels.EncodeToPNG());
            }
            File.WriteAllText(Path.Combine(output,variant+"-hardware.txt"),Application.unityVersion+"\n"+SystemInfo.graphicsDeviceName+"\n"+SystemInfo.graphicsDeviceType);
        }
        finally
        {
            if(recorder!=null)recorder.enabled=false;
            GraphicsSettings.defaultRenderPipeline=oldGraphics; QualitySettings.renderPipeline=oldQuality; QualitySettings.antiAliasing=oldAA;
            RenderTexture.active=oldActive;
            foreach(var b in objects)Object.DestroyImmediate(b);
            Object.DestroyImmediate(go); Object.DestroyImmediate(profile); Object.DestroyImmediate(material);
            Object.DestroyImmediate(pixels); target.Release(); Object.DestroyImmediate(target);
            Object.DestroyImmediate(pipeline); Object.DestroyImmediate(feature); Object.DestroyImmediate(renderer);
        }
    }
}
