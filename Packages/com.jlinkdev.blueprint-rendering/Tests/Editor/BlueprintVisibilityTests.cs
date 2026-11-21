using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.BlueprintRendering.Tests
{
    public sealed class BlueprintVisibilityTests
    {
        [Test]
        public void ThirdSurfaceRevealsGeometryBehindBothFacesOfAnOpaqueWall()
        {
            var root = new GameObject("Blueprint visibility test");
            var camera = root.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(1000, 500, 0);
            camera.orthographic = true;
            camera.orthographicSize = 2;
            camera.cullingMask = 1 << 30;
            camera.useOcclusionCulling = false;
            var drawing = root.AddComponent<BlueprintCamera>();
            drawing.layers = camera.cullingMask;
            drawing.paperColor = Color.black;
            drawing.inkColor = Color.white;
            drawing.hiddenInk = 1;
            drawing.creaseAngle = 1;
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            var feature = ScriptableObject.CreateInstance<BlueprintRendererFeature>();
            renderer.rendererFeatures.Add(feature);
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
            var readback = new Texture2D(128, 128, TextureFormat.RGB24, false);
            var previousTarget = RenderTexture.active;
            var previousDefault = GraphicsSettings.defaultRenderPipeline;
            var previousQuality = QualitySettings.renderPipeline;
            try
            {
                AddBox(root.transform, material, 2f, 3f);
                AddBox(root.transform, material, 3f, 1f);
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                camera.targetTexture = target;
                // The region includes the rear box silhouette but excludes the wall outline.
                drawing.surfaceLayers = 2;
                Assert.That(CountInk(camera, target, readback), Is.Zero);
                drawing.surfaceLayers = 3;
                Assert.That(CountInk(camera, target, readback), Is.GreaterThan(50));
                drawing.layers = 0;
                Assert.That(CountInk(camera, target, readback), Is.Zero);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousTarget;
                QualitySettings.renderPipeline = previousQuality;
                GraphicsSettings.defaultRenderPipeline = previousDefault;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(pipeline);
                Object.DestroyImmediate(renderer);
                Object.DestroyImmediate(feature);
            }
        }

        private static void AddBox(Transform parent, Material material, float depth, float width)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.layer = 30;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = Vector3.forward * depth;
            box.transform.localScale = new Vector3(width, width, 0.1f);
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static int CountInk(Camera camera, RenderTexture target, Texture2D readback)
        {
            camera.Render();
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
            readback.Apply();
            var pixels = readback.GetPixels32();
            int count = 0;
            for (int y = 40; y < 88; y++)
                for (int x = 40; x < 88; x++)
                    if (pixels[y * 128 + x].r > 30) count++;
            return count;
        }
    }
}
