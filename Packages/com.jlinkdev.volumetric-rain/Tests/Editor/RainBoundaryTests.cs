using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace jlinkdev.UnityUtilities.VolumetricRain.Tests
{
    public class RainBoundaryTests
    {
        [UnityTest]
        public IEnumerator FlushShelterSurfacesStayDryWhileRainOutsideRemainsVisible()
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
            profile.noiseStrength = 0;
            var cameraObject = new GameObject("Boundary camera", typeof(Camera), typeof(VolumetricRainCamera));
            var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
            camera.nearClipPlane = .1f; camera.farClipPlane = 150;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var rain = cameraObject.GetComponent<VolumetricRainCamera>();
            rain.profile = profile; rain.freezeTime = true;
            var dry = new GameObject("Flush exclusion").AddComponent<RainExclusionVolume>();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.color = new Color(.1f,.15f,.2f);
            wall.GetComponent<Renderer>().sharedMaterial = material;
            var target = new RenderTexture(384,216,24,RenderTextureFormat.ARGB32); target.Create();
            var pixels = new Texture2D(384,216,TextureFormat.RGBA32,false,true);
            try
            {
                GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
                yield return null; yield return null;
                int leakedPixels = 0, outsidePixels = 0;
                // Wall, floor and transformed room, with the dry boundary flush to the opaque face.
                for (int pose=0;pose<3;pose++)
                {
                    var rotation = pose==1 ? Quaternion.Euler(90,0,0) : pose==2 ? Quaternion.Euler(17,31,8) : Quaternion.identity;
                    var origin = pose==2 ? new Vector3(-13.17f,2.43f,-19.31f) : Vector3.zero;
                    camera.transform.SetPositionAndRotation(origin, rotation);
                    dry.transform.SetPositionAndRotation(origin, rotation);
                    dry.transform.localScale = pose==2 ? new Vector3(1.1f,.9f,1.3f) : Vector3.one;
                    dry.size = new Vector3(30,30,10);
                    wall.transform.SetPositionAndRotation(dry.transform.TransformPoint(new Vector3(0,0,5.1f)),rotation);
                    wall.transform.localScale = Vector3.Scale(dry.transform.localScale,new Vector3(30,30,.2f));
                    var baseline = Capture(camera,target,pixels,rain,0);
                    for (int frame=0;frame<24;frame++)
                    {
                        rain.fixedTime = frame*.073f;
                        leakedPixels += ChangedPixels(baseline,Capture(camera,target,pixels,rain,1));
                    }
                    dry.enabled=false;
                    outsidePixels += ChangedPixels(baseline,Capture(camera,target,pixels,rain,1));
                    dry.enabled=true;
                }
                Debug.Log("RAIN_BOUNDARY_PIXELS: leaked="+leakedPixels+", exclusion-disabled="+outsidePixels);
                Assert.That(outsidePixels,Is.GreaterThan(100),"The fixture must see rain when the exclusion is disabled.");
                Assert.That(leakedPixels,Is.Zero,"Flush walls/floors must remain dry at every tested animation time and room transform.");
                // An actual opening must still show rain beyond the dry boundary.
                wall.SetActive(false);
                var clear = Capture(camera,target,pixels,rain,0);
                Assert.That(ChangedPixels(clear,Capture(camera,target,pixels,rain,1)),Is.GreaterThan(100));
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline=oldGraphics; QualitySettings.renderPipeline=oldQuality;
                QualitySettings.antiAliasing=oldAA; RenderTexture.active=oldTarget;
                Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(dry.gameObject); Object.DestroyImmediate(wall);
                Object.DestroyImmediate(material); Object.DestroyImmediate(profile); Object.DestroyImmediate(pixels);
                target.Release(); Object.DestroyImmediate(target);
                Object.DestroyImmediate(pipeline); Object.DestroyImmediate(feature); Object.DestroyImmediate(renderer);
            }
        }

        private static Color32[] Capture(Camera camera, RenderTexture target, Texture2D pixels, VolumetricRainCamera rain, float intensity)
        {
            rain.intensity=intensity;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
            RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply();
            return pixels.GetPixels32();
        }
        private static int ChangedPixels(Color32[] a, Color32[] b)
        {
            int count=0;
            for(int i=0;i<a.Length;i++)
                if(Mathf.Abs(a[i].r-b[i].r)>1 || Mathf.Abs(a[i].g-b[i].g)>1 || Mathf.Abs(a[i].b-b[i].b)>1)count++;
            return count;
        }
    }
}
