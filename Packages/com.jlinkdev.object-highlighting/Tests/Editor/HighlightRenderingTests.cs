using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.ObjectHighlighting.Tests
{
    public sealed class HighlightRenderingTests
    {
        private GameObject root;
        private Camera camera;
        private ObjectHighlight target;
        private Material black;
        private RenderTexture image, previousTarget;
        private Texture2D pixels;
        private UniversalRendererData renderer;
        private UniversalRenderPipelineAsset pipeline;
        private ObjectHighlightRendererFeature feature;
        private RenderPipelineAsset previousQuality, previousDefault;

        [SetUp]
        public void SetUp()
        {
            previousTarget = RenderTexture.active;
            previousDefault = GraphicsSettings.defaultRenderPipeline;
            previousQuality = QualitySettings.renderPipeline;
            root = new GameObject("Highlight test");
            camera = root.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(1000, 0, 0);
            camera.orthographic = true;
            camera.orthographicSize = 2;
            camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            feature = ScriptableObject.CreateInstance<ObjectHighlightRendererFeature>();
            renderer.rendererFeatures.Add(feature);
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            QualitySettings.renderPipeline = pipeline;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            black = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            black.SetColor("_BaseColor", Color.black);
            target = Box("Target", new Vector3(0, 0, 4), 1).AddComponent<ObjectHighlight>();
            image = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
            pixels = new Texture2D(128, 128, TextureFormat.RGB24, false);
            camera.targetTexture = image;
        }

        private GameObject Box(string name, Vector3 offset, float size)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.layer = 30;
            obj.transform.SetParent(root.transform, false);
            obj.transform.localPosition = offset;
            obj.transform.localScale = Vector3.one * size;
            obj.GetComponent<MeshRenderer>().sharedMaterial = black;
            return obj;
        }

        private void Render()
        {
            camera.Render();
            RenderTexture.active = image;
            pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
            pixels.Apply();
        }

        [TearDown]
        public void TearDown()
        {
            camera.targetTexture = null;
            RenderTexture.active = previousTarget;
            QualitySettings.renderPipeline = previousQuality;
            GraphicsSettings.defaultRenderPipeline = previousDefault;
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(pixels);
            Object.DestroyImmediate(black);
            Object.DestroyImmediate(pipeline);
            Object.DestroyImmediate(renderer);
            Object.DestroyImmediate(feature);
        }

        [Test]
        public void HoverSelectionAndDisableRestoreTheExpectedPixels()
        {
            target.SetHovered(true);
            Render();
            Assert.That(pixels.GetPixel(80, 64).b, Is.GreaterThan(0.5f), "Hover edge");
            Assert.That(pixels.GetPixel(83, 64).maxColorComponent, Is.LessThan(0.02f), "Hover width");
            target.SetSelected(true);
            target.SetHovered(false);
            Render();
            Assert.That(pixels.GetPixel(80, 64).r, Is.GreaterThan(0.7f), "Selection survives pointer exit");
            Assert.That(pixels.GetPixel(86, 64).r, Is.GreaterThan(0.04f), "Outer glow beyond contour");
            Assert.That(pixels.GetPixel(64, 64).maxColorComponent, Is.LessThan(0.02f), "Interior remains black");
            target.SetHovered(true);
            target.SetSelected(false);
            Render();
            Assert.That(pixels.GetPixel(80, 64).b, Is.GreaterThan(0.5f), "Hover restored");
            target.enabled = false;
            Render();
            Assert.That(pixels.GetPixel(80, 64).maxColorComponent, Is.LessThan(0.02f));
        }

        [TestCase(ObjectHighlightRendererFeature.GlowResolution.Full)]
        [TestCase(ObjectHighlightRendererFeature.GlowResolution.Half)]
        [TestCase(ObjectHighlightRendererFeature.GlowResolution.Quarter)]
        [TestCase(ObjectHighlightRendererFeature.GlowResolution.Off)]
        public void GlowResolutionPreservesTheContour(ObjectHighlightRendererFeature.GlowResolution resolution)
        {
            feature.glowResolution = resolution;
            target.SetSelected(true);
            Render();
            Assert.That(pixels.GetPixel(81, 64).r, Is.GreaterThan(0.7f));
            float halo = pixels.GetPixel(86, 64).r;
            if (resolution == ObjectHighlightRendererFeature.GlowResolution.Off) Assert.That(halo, Is.LessThan(0.02f));
            else Assert.That(halo, Is.GreaterThan(0.04f));
        }

        [Test]
        public void ChildSkinnedMeshUsesTheRootsGlowOnlyOverride()
        {
            var child = Box("Skinned child", Vector3.zero, 1);
            child.transform.SetParent(target.transform, false);
            child.transform.localPosition = Vector3.zero;
            var source = child.GetComponent<MeshFilter>().sharedMesh;
            var mesh = Object.Instantiate(source);
            var style = ScriptableObject.CreateInstance<HighlightStyle>();
            try
            {
                Object.DestroyImmediate(child.GetComponent<MeshRenderer>());
                Object.DestroyImmediate(child.GetComponent<MeshFilter>());
                var skin = child.AddComponent<SkinnedMeshRenderer>();
                var weights = new BoneWeight[mesh.vertexCount];
                for (int i = 0; i < weights.Length; i++) weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1 };
                mesh.boneWeights = weights;
                mesh.bindposes = new[] { Matrix4x4.identity };
                skin.bones = new[] { child.transform };
                skin.rootBone = child.transform;
                skin.sharedMesh = mesh;
                skin.sharedMaterial = black;
                skin.updateWhenOffscreen = true;
                target.GetComponent<MeshRenderer>().enabled = false;
                style.outlineWidth = 0;
                style.glowColor = Color.green;
                style.glowIntensity = 2;
                style.glowRadius = 14;
                target.selectedStyle = style;
                target.SetSelected(true);
                Render();
                var color = pixels.GetPixel(84, 64);
                Assert.That(color.g, Is.GreaterThan(0.08f));
                Assert.That(color.r, Is.LessThan(0.02f));
                Assert.That(pixels.GetPixel(64, 64).maxColorComponent, Is.LessThan(0.02f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(style);
            }
        }

        [Test]
        public void MultisampledTargetResolvesTheMask()
        {
            pipeline.msaaSampleCount = 4;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            image = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = image;
            target.SetSelected(true);
            Render();
            Assert.That(pixels.GetPixel(81, 64).r, Is.GreaterThan(0.7f));
            Assert.That(pixels.GetPixel(86, 64).r, Is.GreaterThan(0.04f));
        }

        [Test]
        public void FullyOccludedAndCameraExcludedObjectsProduceNoHighlight()
        {
            target.SetSelected(true);
            var wall = Box("Occluder", new Vector3(0, 0, 2), 1.5f);
            Render();
            Assert.That(pixels.GetPixel(80, 64).maxColorComponent, Is.LessThan(0.02f));
            wall.SetActive(false);
            target.gameObject.layer = 29;
            Render();
            Assert.That(pixels.GetPixel(80, 64).maxColorComponent, Is.LessThan(0.02f));
        }

        [Test]
        public void AdjacentStylesRespectBothSilhouetteInteriors()
        {
            target.SetSelected(true);
            var other = Box("Hovered", new Vector3(1, 0, 4), 1).AddComponent<ObjectHighlight>();
            other.SetHovered(true);
            Render();
            Assert.That(pixels.GetPixel(80, 64).maxColorComponent, Is.LessThan(0.02f), "No amber on cyan object");
            Assert.That(pixels.GetPixel(79, 64).maxColorComponent, Is.LessThan(0.02f), "No cyan on amber object");
            Assert.That(pixels.GetPixel(112, 64).b, Is.GreaterThan(0.5f));
            Assert.That(pixels.GetPixel(47, 64).r, Is.GreaterThan(0.7f));
        }
    }
}
