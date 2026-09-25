using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain.Tests
{
    public class FogFeatureTests
    {
        [Test]
        public void RuntimeBlendingDoesNotChangeAssetsAndDefinesDiscreteTransitions()
        {
            var from = ScriptableObject.CreateInstance<RainProfile>();
            var to = ScriptableObject.CreateInstance<RainProfile>();
            var go = new GameObject("Runtime settings", typeof(Camera), typeof(VolumetricRainCamera));
            try
            {
                from.fogDensity = 0; to.fogDensity = 2;
                from.fogColor = new Color(2,0,0); to.fogColor = new Color(0,4,0);
                from.renderMode = RainRenderMode.RainOnly; to.renderMode = RainRenderMode.FogOnly;
                from.seed = 1; to.seed = 2; to.cellSize = 1;
                var a = EditorJsonUtility.ToJson(from); var b = EditorJsonUtility.ToJson(to);
                var camera = go.GetComponent<VolumetricRainCamera>(); camera.profile = from;
                var settings = camera.CreateRuntimeSettings();
                settings.BlendAppearance(from,to,.5f);
                Assert.That(settings.fogDensity,Is.EqualTo(1));
                Assert.That(settings.fogColor,Is.EqualTo(new Color(1,2,0)));
                Assert.That(settings.renderMode,Is.EqualTo(from.renderMode));
                Assert.That(settings.seed,Is.EqualTo(1));
                Assert.That(settings.cellSize,Is.EqualTo(from.cellSize));
                Assert.That(camera.ResolveSettings(),Is.SameAs(settings));
                settings.BlendAppearance(from,to,.5f,BlendDiscreteSettings.AtStart);
                Assert.That(settings.renderMode,Is.EqualTo(to.renderMode));
                settings.BlendAppearance(from,to,1);
                Assert.That(settings.seed,Is.EqualTo(2));
                Assert.That(EditorJsonUtility.ToJson(from),Is.EqualTo(a));
                Assert.That(EditorJsonUtility.ToJson(to),Is.EqualTo(b));
                camera.RuntimeSettings = null;
                Assert.That(camera.ResolveSettings().fogDensity,Is.Zero);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(from); Object.DestroyImmediate(to); }
        }

        [UnityTest]
        public IEnumerator TransparentOutputCompositesEmptyPartialOpaqueAndHdrPixels()
        {
            using (var r = new Rig())
            {
                yield return null; yield return null;
                r.feature.outputMode = FogOutputMode.Premultiplied;
                r.profile.fogColor = new Color(4,2,.5f,1);
                r.profile.fogBrightness = 1; r.profile.fogScattering = 1;
                // Homogeneous 4 m slab at extinction .2, independent of pixel alpha.
                float transmittance = Mathf.Exp(-.2f*4);
                foreach(float alpha in new[] { 0f, .35f, 1f })
                {
                    r.camera.backgroundColor = new Color(.4f*alpha,.2f*alpha,.1f*alpha,alpha);
                    r.rain.intensity = 0; Color source = r.Center(r.Capture()); r.rain.intensity = 1;
                    Color fog = r.Center(r.Capture());
                    Color expected = source*transmittance + r.profile.fogColor*(1-transmittance);
                    expected.a = 1-(1-source.a)*transmittance;
                    AssertColor(fog,expected,.015f,"premultiplied fog at source alpha "+alpha);
                    Assert.That(fog.r,Is.GreaterThan(1),"HDR radiance must survive without clamping.");
                    foreach(var background in new[] { Color.black,Color.white,new Color(.1f,.6f,2f) })
                        AssertColor(Over(fog,background),Over(expected,background),.02f,"external background composition");
                    r.profile.hazeExtinction = 0;
                    AssertColor(r.Center(r.Capture()),source,.002f,"zero fog must preserve source");
                    r.profile.hazeExtinction = .2f;
                }
                // Empty pixels with stale RGB must not leak that hidden color into a premultiplied image.
                r.camera.backgroundColor = new Color(1,0,1,0);
                Color empty = r.Center(r.Capture());
                Color emptyExpected = r.profile.fogColor*(1-transmittance); emptyExpected.a = 1-transmittance;
                AssertColor(empty,emptyExpected,.015f,"empty source RGB");
                r.feature.outputMode = FogOutputMode.PreserveSceneAlpha;
                Assert.That(r.Center(r.Capture()).a,Is.EqualTo(0).Within(.002f),"legacy scene alpha contract");
                foreach(var message in ShaderUtil.GetShaderMessages(Shader.Find("Hidden/jlinkdev/Volumetric Fog and Rain")))
                    Assert.That(message.severity.ToString(),Is.Not.EqualTo("Error"),message.message);
            }
        }

        [UnityTest]
        public IEnumerator FeatherHeightDistancesAndNoiseHaveIndependentWorldSpaceBehavior()
        {
            using (var r = new Rig())
            {
                yield return null; yield return null;
                r.feature.outputMode = FogOutputMode.Premultiplied;
                r.camera.backgroundColor = Color.clear;
                Color[] hard = r.Capture();
                r.box.featherDistance = 1;
                Color[] soft = r.Capture();
                Assert.That(r.At(soft,1.75f,0).a,Is.LessThan(r.At(hard,1.75f,0).a*.5f));
                Assert.That(r.At(soft,1.75f,0).a,Is.GreaterThan(.001f));
                Assert.That(r.At(soft,2.5f,0).a,Is.Zero.Within(.002f));
                var duplicate = r.Box("Duplicate"); duplicate.transform.position = r.box.transform.position;
                duplicate.size = r.box.size; duplicate.featherDistance = 1;
                Assert.That(Difference(soft,r.Capture()),Is.LessThan(.00001),"union must not double density");
                duplicate.enabled = false;
                // Same world box dimensions represented by nonuniform transform scale.
                r.box.transform.localScale = new Vector3(2,.5f,1.5f); r.box.size = new Vector3(2,8,4f/1.5f);
                Assert.That(Difference(soft,r.Capture()),Is.LessThan(.001),"feather metres must survive scale");
                r.box.transform.rotation = Quaternion.Euler(0,0,90);
                Assert.That(Difference(soft,r.Capture()),Is.LessThan(.001),"rotated symmetric box");
                r.box.transform.rotation = Quaternion.identity; r.box.transform.localScale = Vector3.one; r.box.size = Vector3.one*4;
                r.box.featherDistance = 0;
                Assert.That(Difference(hard,r.Capture()),Is.LessThan(.00001),"zero feather restores hard path");
                var dry = r.Own(new GameObject("Dry core")).AddComponent<RainExclusionVolume>();
                dry.gameObject.layer = Rig.Layer; dry.transform.position = r.box.transform.position;
                dry.size = new Vector3(2,10,100); dry.featherDistance = 1;
                var cut = r.Capture();
                Assert.That(r.At(cut,.5f,0).a,Is.Zero.Within(.002f),"feather must never fill the dry core");
                Assert.That(r.At(cut,1.25f,0).a,Is.LessThan(r.At(cut,1.75f,0).a),"outward exclusion ramp");
                Assert.That(r.At(cut,1.25f,0).a,Is.GreaterThan(.001f));
                dry.enabled = false;
                r.profile.fogBaseHeight = 0; r.profile.fogHeightFalloff = 1;
                var height = r.Capture();
                Assert.That(r.At(height,-.5f,-1).a,Is.EqualTo(r.At(hard,-.5f,-1).a).Within(.002f));
                Assert.That(r.At(height,-.5f,1).a,Is.LessThan(r.At(height,-.5f,0).a));
                r.profile.fogHeightFalloff = 0;
                r.profile.renderMode = RainRenderMode.RainAndFog; r.profile.independentFogSettings = true; r.profile.density = 0;
                r.profile.fogDistanceMode = FogDistanceMode.Independent;
                r.profile.midDistance = 50; r.profile.farDistance = 100; r.profile.maxCellSteps = 16;
                Assert.That(Difference(hard,r.Capture()),Is.LessThan(.00001),"independent fog ignores streak distances and budget");
                r.profile.fogMaxDistance = 2;
                Assert.That(r.Center(r.Capture()).a,Is.Zero.Within(.002f));
                r.profile.fogMaxDistance = 100; r.profile.fogStartDistance = 8;
                Assert.That(r.Center(r.Capture()).a,Is.Zero.Within(.002f));
                r.profile.fogStartDistance = 0; r.profile.fogDistanceFade = 10;
                Assert.That(r.Center(r.Capture()).a,Is.LessThan(r.Center(hard).a));
                r.profile.maxCellSteps = 256; r.profile.fogDistanceFade = 0; r.profile.fogDistanceMode = FogDistanceMode.RainLinked;
                Assert.That(r.Center(r.Capture()).a,Is.Zero.Within(.002f),"legacy distance link retained");
                r.profile.renderMode = RainRenderMode.FogOnly;
                r.profile.noiseStrength = 1; r.profile.noiseScale = .5f;
                var still = r.Capture(); r.rain.fixedTime += 2;
                Assert.That(Difference(still,r.Capture()),Is.LessThan(.00001),"zero velocity stationary");
                r.profile.fogNoiseVelocity = Vector3.right;
                Assert.That(Difference(still,r.Capture()),Is.LessThan(.00001),"velocity change must not jump at the same time");
                r.rain.fixedTime += 2;
                Assert.That(Difference(still,r.Capture()),Is.GreaterThan(.001),"fog noise must advect");
                var moving = r.Capture(); r.profile.fogNoiseVelocity = Vector3.zero; r.rain.fixedTime += 2;
                Assert.That(Difference(moving,r.Capture()),Is.LessThan(.00001),"stopping velocity must retain phase");
                r.rain.RuntimeSettings = new RainSettings(r.profile); r.rain.RuntimeSettings.fogDensity = 0;
                Assert.That(r.Center(r.Capture()).a,Is.Zero.Within(.002f),"renderer must consume runtime override");
                Assert.That(r.profile.fogDensity,Is.EqualTo(1),"runtime override leaves asset intact");
                r.rain.RuntimeSettings=null; r.profile.renderMode=RainRenderMode.RainOnly;
                r.profile.density=1; r.profile.noiseStrength=0; r.profile.fallSpeed=0; r.profile.wind=Vector3.zero;
                r.profile.nearFade=0; r.profile.midDistance=12; r.profile.farDistance=30;
                var hardRain=r.Capture(); r.box.featherDistance=1;
                var softRain=r.Capture();
                Assert.That(Difference(hardRain,new Color[hardRain.Length]),Is.GreaterThan(.0001),"rain comparison must contain streaks");
                Assert.That(Difference(softRain,new Color[softRain.Length]),Is.LessThan(Difference(hardRain,new Color[hardRain.Length])),"feather must attenuate rain as well as fog");
                r.profile.fogNoiseVelocity=Vector3.one*10;
                Assert.That(Difference(softRain,r.Capture()),Is.LessThan(.00001),"fog noise velocity must not change rain");
            }
        }

        [UnityTest]
        public IEnumerator InjectionPointsAndDirectionalLightRender()
        {
            using (var r = new Rig())
            {
                yield return null; yield return null;
                var reference = r.Capture();
                foreach(RainInjectionPoint point in Enum.GetValues(typeof(RainInjectionPoint)))
                {
                    r.feature.injectionPoint = point;
                    Assert.That(Difference(reference,r.Capture()),Is.LessThan(.003),"empty-scene fog at "+point);
                }
                r.feature.injectionPoint = RainInjectionPoint.BeforePostProcessing;
                var light = r.Own(new GameObject("Sun")).AddComponent<Light>(); light.type = LightType.Directional;
                light.intensity = 1; light.color = Color.white; light.cullingMask = 1 << Rig.Layer;
                light.transform.rotation = Quaternion.Euler(0,180,0);
                RenderSettings.sun = light;
                yield return null;
                r.profile.directionalScattering = .25f; r.profile.scatteringAnisotropy = .5f;
                var toward = r.Center(r.Capture());
                light.transform.rotation = Quaternion.identity;
                var away = r.Center(r.Capture());
                Assert.That(toward.r,Is.GreaterThan(away.r+.02f),"forward glow should follow main light direction");
                r.profile.directionalScattering = 0;
                Assert.That(Difference(reference,r.Capture()),Is.LessThan(.003),"zero light strength restores unlit fog");
            }
        }

        [UnityTest]
        public IEnumerator TransparentGeometryCaptureAndPostProcessingRespectTiming()
        {
            using (var r = new Rig())
            {
                yield return null; yield return null;
                r.feature.outputMode = FogOutputMode.Premultiplied;
                var quad = r.Own(GameObject.CreatePrimitive(PrimitiveType.Quad));
                quad.layer=Rig.Layer; quad.transform.position=new Vector3(0,0,2); quad.transform.localScale=Vector3.one*10;
                var material=r.Own(new Material(Shader.Find("Hidden/jlinkdev/Fog Test Transparent")));
                material.SetColor("_Color",new Color(.4f,0,0,.5f)); quad.GetComponent<Renderer>().sharedMaterial=material;
                r.rain.intensity=0; var source=r.Center(r.Capture()); r.rain.intensity=1;
                float t=Mathf.Exp(-.8f);
                r.feature.injectionPoint=RainInjectionPoint.BeforePostProcessing;
                var after=r.Center(r.Capture());
                Color expectedAfter=source*t+Color.white*(1-t); expectedAfter.a=1-(1-source.a)*t;
                AssertColor(after,expectedAfter,.02f,"fog after actual transparent geometry");
                r.feature.injectionPoint=RainInjectionPoint.BeforeTransparents;
                Color expectedBefore=source+Color.white*((1-t)*(1-source.a)); expectedBefore.a=expectedAfter.a;
                AssertColor(r.Center(r.Capture()),expectedBefore,.02f,"transparent geometry over fog");
                // Refraction-style material reads the opaque snapshot, then covers the destination.
                material.SetFloat("_UseCapture",1);
                r.feature.injectionPoint=RainInjectionPoint.BeforeTransparentCapture;
                float included=r.Center(r.Capture()).r;
                r.feature.injectionPoint=RainInjectionPoint.BeforeTransparents;
                float excluded=r.Center(r.Capture()).r;
                Assert.That(included,Is.GreaterThan(.4f),"fog must be captured at the capture injection point");
                Assert.That(excluded,Is.LessThan(.01f),"before transparents is after opaque capture");
                quad.SetActive(false);
                var volume=r.Own(new GameObject("Exposure test")).AddComponent<Volume>(); volume.isGlobal=true; volume.priority=1000; volume.gameObject.layer=Rig.Layer;
                var volumeProfile=r.Own(ScriptableObject.CreateInstance<VolumeProfile>()); volume.sharedProfile=volumeProfile;
                var exposure=volumeProfile.Add<ColorAdjustments>(true); exposure.postExposure.Override(1);
                volume.enabled=false; volume.enabled=true; // Register on the final layer in an EditMode test frame.
                VolumeManager.instance.Update(r.camera.transform,1<<Rig.Layer);
                var cameraData=r.camera.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing=true; cameraData.volumeLayerMask=1<<Rig.Layer; r.camera.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.EveryFrame);
                yield return null;
                r.feature.injectionPoint=RainInjectionPoint.BeforePostProcessing;
                float exposed=r.Center(r.Capture()).r;
                Assert.That(VolumeManager.instance.stack.GetComponent<ColorAdjustments>().postExposure.value,Is.EqualTo(1).Within(.001f),"test exposure volume must be active");
                r.feature.injectionPoint=RainInjectionPoint.AfterPostProcessing;
                float unexposed=r.Center(r.Capture()).r;
                float exposureFactor = QualitySettings.activeColorSpace == ColorSpace.Linear ? 2 : Mathf.Pow(2,1/2.2f);
                Assert.That(exposed,Is.EqualTo(unexposed*exposureFactor).Within(.04f),"post-processing should affect only fog injected before it");
            }
        }

        [UnityTest]
        public IEnumerator EarlyFogThenTransparentsAt1xMsaa() => EarlyFogThenTransparents(1);

        [UnityTest]
        public IEnumerator EarlyFogThenTransparentsAt8xMsaa() => EarlyFogThenTransparents(8);

        private static IEnumerator EarlyFogThenTransparents(int samples)
        {
            using (var r = new Rig(samples, true))
            {
                yield return null; yield return null;
                r.feature.outputMode = FogOutputMode.Premultiplied;
                var quad = r.Own(GameObject.CreatePrimitive(PrimitiveType.Quad));
                quad.layer = Rig.Layer;
                quad.transform.position = new Vector3(0,0,2);
                quad.transform.localScale = Vector3.one * 10;
                var material = r.Own(new Material(Shader.Find("Hidden/jlinkdev/Fog Test Transparent")));
                material.SetColor("_Color", new Color(.4f,0,0,.5f));
                quad.GetComponent<Renderer>().sharedMaterial = material;
                foreach (var point in new[] { RainInjectionPoint.BeforeTransparentCapture, RainInjectionPoint.BeforeTransparents })
                {
                    r.feature.injectionPoint = point;
                    // Establish the same scene without the effect, including real glass blending.
                    r.rain.intensity = 0;
                    Color glass = r.Center(r.Capture());
                    Color glassReference = new Color(.4f,0,0,.5f);
                    if (QualitySettings.activeColorSpace == ColorSpace.Linear) glassReference = glassReference.linear;
                    AssertColor(glass, glassReference, .01f, "glass reference");
                    Assert.That(r.probe.depthSamples, Is.EqualTo(samples), "test must use the requested MSAA depth attachment");
                    Assert.That(r.probe.colorSamples, Is.EqualTo(samples), "reference MSAA color attachment");
                    r.rain.intensity = 1;
                    Color result = r.Center(r.Capture());
                    Assert.That(r.probe.colorSamples, Is.EqualTo(samples), "fog must preserve active color samples at " + point);
                    Assert.That(r.probe.depthSamples, Is.EqualTo(samples), "fog must remain compatible with geometry depth at " + point);
                    float transmittance = Mathf.Exp(-.8f);
                    Color expected = glass + Color.white * ((1-transmittance)*(1-glass.a));
                    expected.a = 1-(1-glass.a)*transmittance;
                    AssertColor(result, expected, .02f, "glass over fog at " + samples + "x / " + point);
                    // Exercise a glass material that reads the opaque capture as well.
                    material.SetFloat("_UseCapture", 1);
                    float captured = r.Center(r.Capture()).r;
                    Assert.That(captured, Is.EqualTo(point == RainInjectionPoint.BeforeTransparentCapture ? 1-transmittance : 0).Within(.02f), "MSAA refraction capture order");
                    material.SetFloat("_UseCapture", 0);
                }
                LogAssert.NoUnexpectedReceived();
            }
        }

        // Observe actual Render Graph attachments immediately before transparent geometry.
        // Checking the URP asset alone would miss a test silently falling back to 1x.
        private sealed class AttachmentProbe : ScriptableRendererFeature
        {
            public int colorSamples, depthSamples;
            private ProbePass pass;
            public override void Create() => pass = new ProbePass(this);
            public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) => renderer.EnqueuePass(pass);
            private sealed class ProbePass : ScriptableRenderPass
            {
                private readonly AttachmentProbe owner;
                public ProbePass(AttachmentProbe owner) { this.owner = owner; renderPassEvent = RenderPassEvent.BeforeRenderingTransparents; }
                public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
                {
                    var resources = frameData.Get<UniversalResourceData>();
                    owner.colorSamples = (int)graph.GetTextureDesc(resources.activeColorTexture).msaaSamples;
                    owner.depthSamples = (int)graph.GetTextureDesc(resources.activeDepthTexture).msaaSamples;
                }
            }
        }

        private static Color Over(Color premult, Color background)
        {
            Color result = premult + background*(1-premult.a); result.a = 1; return result;
        }
        private static void AssertColor(Color a, Color b, float tolerance, string label)
        {
            for(int i=0;i<4;i++) Assert.That(a[i],Is.EqualTo(b[i]).Within(tolerance),label+" channel "+i);
        }
        private static double Difference(Color[] a, Color[] b)
        {
            double sum=0; for(int i=0;i<a.Length;i++) for(int j=0;j<4;j++) sum+=Math.Abs(a[i][j]-b[i][j]);
            return sum/(a.Length*4);
        }
        private sealed class Rig : IDisposable
        {
            public const int Layer = 30;
            public readonly Camera camera;
            public readonly VolumetricRainCamera rain;
            public readonly RainProfile profile;
            public readonly RainRendererFeature feature;
            public readonly RainVolume box;
            public readonly AttachmentProbe probe;
            private readonly List<Object> objects = new List<Object>();
            private readonly RenderPipelineAsset graphics = GraphicsSettings.defaultRenderPipeline, quality = QualitySettings.renderPipeline;
            private readonly RenderTexture active = RenderTexture.active;
            private readonly Light sun = RenderSettings.sun;
            private readonly RenderTexture target;
            private readonly Texture2D pixels;
            private readonly RenderTexture resolved;
            public T Own<T>(T obj) where T : Object { objects.Add(obj); return obj; }
            public RainVolume Box(string name)
            {
                var obj=Own(new GameObject(name)); obj.layer=Layer; return obj.AddComponent<RainVolume>();
            }
            public Rig(int samples = 1, bool inspectAttachments = false)
            {
                var renderer=Own(ScriptableObject.CreateInstance<UniversalRendererData>());
                renderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                feature=Own(ScriptableObject.CreateInstance<RainRendererFeature>());
                renderer.rendererFeatures.Add(feature); feature.Create();
                if (inspectAttachments)
                {
                    probe = Own(ScriptableObject.CreateInstance<AttachmentProbe>());
                    probe.Create(); renderer.rendererFeatures.Add(probe);
                }
                renderer.SetDirty();
                var pipeline=Own(UniversalRenderPipelineAsset.Create(renderer));
                pipeline.supportsCameraOpaqueTexture=true; pipeline.supportsCameraDepthTexture=true; pipeline.supportsHDR=true; pipeline.colorGradingMode=ColorGradingMode.HighDynamicRange; pipeline.msaaSampleCount=samples;
                profile=Own(ScriptableObject.CreateInstance<RainProfile>());
                profile.renderMode=RainRenderMode.FogOnly; profile.extent=RainExtent.VolumesOnly;
                profile.noiseStrength=0; profile.fogDensity=1; profile.hazeExtinction=.2f;
                profile.fogColor=Color.white; profile.fogBrightness=1; profile.fogScattering=1; profile.hazeSteps=32;
                var go=Own(new GameObject("Fog feature test camera",typeof(Camera),typeof(VolumetricRainCamera)));
                camera=go.GetComponent<Camera>(); camera.enabled=false; camera.cullingMask=1<<Layer;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.clear;
                camera.nearClipPlane=.1f; camera.farClipPlane=150; camera.orthographic=true; camera.orthographicSize=4; camera.allowHDR=true; camera.allowMSAA=true;
                rain=go.GetComponent<VolumetricRainCamera>(); rain.profile=profile; rain.freezeTime=true; rain.fixedTime=2;
                box=Box("Fog slab"); box.transform.position=new Vector3(0,0,5); box.size=Vector3.one*4;
                target=Own(new RenderTexture(128,128,24,RenderTextureFormat.ARGBHalf)); target.antiAliasing=samples;
                Assert.That(SystemInfo.GetRenderTextureSupportedMSAASampleCount(target.descriptor), Is.EqualTo(samples), "requested test MSAA must be supported");
                target.Create();
                if (samples > 1) { resolved=Own(new RenderTexture(128,128,0,RenderTextureFormat.ARGBHalf)); resolved.Create(); }
                pixels=Own(new Texture2D(128,128,TextureFormat.RGBAFloat,false,true));
                GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline;
            }
            public Color[] Capture()
            {
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=target});
                if (resolved != null) target.ResolveAntiAliasedSurface(resolved);
                RenderTexture.active=resolved != null ? resolved : target; pixels.ReadPixels(new Rect(0,0,128,128),0,0); pixels.Apply(); return pixels.GetPixels();
            }
            public Color Center(Color[] image) => image[64*128+64];
            public Color At(Color[] image,float x,float y) => image[Mathf.Clamp((int)((y+4)/8*128),0,127)*128+Mathf.Clamp((int)((x+4)/8*128),0,127)];
            public void Dispose()
            {
                GraphicsSettings.defaultRenderPipeline=graphics; QualitySettings.renderPipeline=quality; RenderSettings.sun=sun;
                RenderTexture.active=active; target.Release(); if (resolved != null) resolved.Release();
                for(int i=objects.Count-1;i>=0;i--) Object.DestroyImmediate(objects[i]);
            }
        }
    }
}
