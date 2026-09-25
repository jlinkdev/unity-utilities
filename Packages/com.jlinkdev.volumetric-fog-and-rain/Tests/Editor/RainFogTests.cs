using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain.Tests
{
    public class RainFogTests
    {
        [UnityTest]
        public IEnumerator ModesRespectIndependentFogVolumesAndDepth()
        {
            var oldGraphics = GraphicsSettings.defaultRenderPipeline;
            var oldQuality = QualitySettings.renderPipeline;
            int oldAA = QualitySettings.antiAliasing;
            var oldTarget = RenderTexture.active;
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            var feature = ScriptableObject.CreateInstance<RainRendererFeature>();
            renderer.rendererFeatures.Add(feature); feature.Create(); renderer.SetDirty();
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.supportsCameraDepthTexture = true; pipeline.msaaSampleCount = 1;
            var profile = ScriptableObject.CreateInstance<RainProfile>();
            profile.renderMode = RainRenderMode.FogOnly; profile.extent = RainExtent.VolumesOnly;
            profile.density = 0; profile.noiseStrength = 0; profile.hazeExtinction = .2f;
            profile.nearFade = 20; profile.midDistance = 100; profile.farDistance = 200;
            var go = new GameObject("Fog test camera", typeof(Camera), typeof(VolumetricRainCamera));
            var camera = go.GetComponent<Camera>(); camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.03f,.04f,.06f);
            camera.nearClipPlane = .1f; camera.farClipPlane = 150;
            camera.orthographic = true; camera.orthographicSize = 3;
            var rain = go.GetComponent<VolumetricRainCamera>(); rain.profile = profile; rain.freezeTime = true; rain.fixedTime = 2;
            var volume = new GameObject("Fog bounds").AddComponent<RainVolume>();
            volume.transform.position = new Vector3(0,0,5); volume.size = new Vector3(4,4,4);
            var duplicate = new GameObject("Fog overlap").AddComponent<RainVolume>(); duplicate.enabled = false;
            var dry = new GameObject("Fog exclusion").AddComponent<RainExclusionVolume>(); dry.enabled = false;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.SetActive(false);
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.color = Color.gray;
            wall.GetComponent<Renderer>().sharedMaterial = material;
            var target = new RenderTexture(384,216,24,RenderTextureFormat.ARGB32); target.Create();
            var pixels = new Texture2D(384,216,TextureFormat.RGBA32,false,true);
            Color32[] Capture(float intensity=1)
            {
                rain.intensity = intensity;
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
                RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply();
                return pixels.GetPixels32();
            }
            try
            {
                GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline;
                yield return null; yield return null;
                var clear = Capture(0);
                var fog = Capture();
                Assert.That(Difference(clear,fog),Is.GreaterThan(.01),"Near fog must render even with zero rain density and distant rain transitions.");
                Assert.That(Difference(clear,Capture(.5f)),Is.LessThan(Difference(clear,fog)),"Camera intensity must control fog.");
                for(int y=0;y<target.height;y++) Assert.That(fog[y*target.width],Is.EqualTo(clear[y*target.width]),"Fog must remain inside its projected box.");
                profile.cellSize=.1f; profile.maxCellSteps=16; profile.fallSpeed=100;
                profile.wind=Vector3.one*20; profile.streakLength=2; profile.streakWidth=.04f;
                profile.density=1; profile.rainColor=Color.green; profile.brightness=99;
                profile.nearFade=40; profile.midDistance=300; profile.farDistance=500; rain.fixedTime=99;
                Assert.That(Difference(fog,Capture()),Is.EqualTo(0),"Fog Only must ignore rain geometry, color, occupancy, motion and transitions.");
                profile.fogColor=Color.red;
                Assert.That(Difference(fog,Capture()),Is.GreaterThan(.01),"Independent fog color must affect the image.");
                profile.fogColor=new Color(.65f,.75f,.85f,1);
                profile.maxDistance=2;
                Assert.That(Difference(clear,Capture()),Is.EqualTo(0),"Fog Max Distance must not be raised to the rain's Far Distance.");
                profile.maxDistance=100;
                profile.fogDensity=0;
                Assert.That(Difference(clear,Capture()),Is.EqualTo(0),"Zero fog density must disable Fog Only even with nonzero rain density.");
                profile.fogDensity=.8f;
                duplicate.transform.position=volume.transform.position; duplicate.size=volume.size; duplicate.enabled=true;
                Assert.That(Difference(fog,Capture()),Is.EqualTo(0),"Overlapping fog boxes must be unioned.");
                duplicate.enabled=false;
                dry.enabled=true; dry.transform.position=volume.transform.position; dry.size=volume.size;
                Assert.That(Difference(clear,Capture()),Is.EqualTo(0),"Exclusions must remove fog.");
                dry.transform.position=Vector3.zero; dry.size=Vector3.one*10;
                Assert.That(Difference(clear,Capture()),Is.GreaterThan(.001),"From inside an exclusion, fog beyond the opening must remain visible.");
                dry.enabled=false;
                rain.debugView=RainDebugView.Streaks;
                Assert.That(Difference(Capture(),new Color32[target.width*target.height]),Is.EqualTo(0),"Fog Only must have zero streak contribution.");
                rain.debugView=RainDebugView.TraversalCost;
                foreach(var pixel in Capture()) { Assert.That(pixel.r,Is.Zero); Assert.That(pixel.g,Is.Zero); }
                rain.debugView=RainDebugView.Composite;
                wall.SetActive(true); wall.transform.position=new Vector3(0,0,2); wall.transform.localScale=new Vector3(100,100,.2f);
                Assert.That(Difference(Capture(0),Capture()),Is.EqualTo(0),"Opaque geometry must occlude the fog behind it.");
                wall.SetActive(false);
                camera.orthographic=false;
                Assert.That(Difference(Capture(0),Capture()),Is.GreaterThan(.01),"Perspective cameras must render fog.");
                profile.extent=RainExtent.Unbounded;
                Assert.That(Difference(Capture(0),Capture()),Is.GreaterThan(.01),"Unbounded Fog Only must use the same full-distance integration.");
                dry.enabled=true; dry.size=Vector3.one*400;
                Assert.That(Difference(Capture(0),Capture()),Is.EqualTo(0),"Global fog must respect exclusions.");
                dry.enabled=false;
                profile.renderMode=RainRenderMode.RainOnly;
                profile.nearFade=.3f; profile.midDistance=12; profile.farDistance=30;
                profile.cellSize=.65f; profile.streakLength=.35f; profile.streakWidth=.008f;
                profile.maxCellSteps=96; profile.brightness=1.5f; profile.rainColor=new Color(.65f,.75f,.85f,1);
                var rainOnly=Capture();
                Assert.That(Difference(Capture(0),rainOnly),Is.GreaterThan(.001),"Rain Only must still render streaks.");
                profile.hazeExtinction=10; profile.fogDensity=10; profile.fogColor=Color.magenta;
                Assert.That(Difference(rainOnly,Capture()),Is.EqualTo(0),"Fog settings must not affect Rain Only.");
                rain.debugView=RainDebugView.Haze;
                Assert.That(Difference(Capture(),new Color32[target.width*target.height]),Is.EqualTo(0));
                rain.debugView=RainDebugView.Composite;
                profile.renderMode=RainRenderMode.RainAndFog; profile.independentFogSettings=true;
                profile.density=0; profile.hazeExtinction=.2f;
                Assert.That(Difference(Capture(0),Capture()),Is.GreaterThan(.01),"Independent combined fog must not be gated by rain density.");
                profile.independentFogSettings=false;
                Assert.That(Difference(Capture(0),Capture()),Is.EqualTo(0),"Default combined mode must retain legacy rain-linked behavior.");
                foreach(var message in ShaderUtil.GetShaderMessages(Shader.Find("Hidden/jlinkdev/Volumetric Fog and Rain")))
                    Assert.That(message.severity.ToString(),Is.Not.EqualTo("Error"),message.message);
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline=oldGraphics; QualitySettings.renderPipeline=oldQuality;
                QualitySettings.antiAliasing=oldAA; RenderTexture.active=oldTarget;
                Object.DestroyImmediate(go); Object.DestroyImmediate(volume.gameObject); Object.DestroyImmediate(duplicate.gameObject); Object.DestroyImmediate(dry.gameObject);
                Object.DestroyImmediate(wall); Object.DestroyImmediate(material); Object.DestroyImmediate(profile);
                Object.DestroyImmediate(pixels); target.Release(); Object.DestroyImmediate(target);
                Object.DestroyImmediate(pipeline); Object.DestroyImmediate(feature); Object.DestroyImmediate(renderer);
            }
        }
        private static double Difference(Color32[] a,Color32[] b)
        {
            double sum=0;
            for(int i=0;i<a.Length;i++) sum+=System.Math.Abs(a[i].r-b[i].r)+System.Math.Abs(a[i].g-b[i].g)+System.Math.Abs(a[i].b-b[i].b);
            return sum/(a.Length*3*255.0);
        }
    }
}
