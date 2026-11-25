using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JLink.ProceduralTextures.Tests
{
    public sealed class ProcessingTests
    {
        static TextureRecipe Stripes() => new TextureRecipe
        {
            generator = "stripes", width = 64, height = 64, cellsX = 1, duty = 0.5f
        };

        [Test]
        public void WarpIsSeededIndependentAndUsesTheChosenAxis()
        {
            var r = Stripes();
            r.domainWarp = true;
            r.warpAmount = 0.25f;
            r.warpSeed = 173;
            r.warpDirection = WarpDirection.Vertical;
            var vertical = new TextureField(r);
            Assert.That(vertical.Sample(0.49f, 0.25f), Is.EqualTo(1));
            r.warpDirection = WarpDirection.Horizontal;
            var a = TextureGenerator.Generate(r);
            var same = TextureGenerator.Generate(r);
            r.warpSeed++;
            var b = TextureGenerator.Generate(r);
            try
            {
                CollectionAssert.AreEqual(a.GetPixels32(), same.GetPixels32());
                CollectionAssert.AreNotEqual(a.GetPixels32(), b.GetPixels32());
                var field = new TextureField(r);
                for (int y = 0; y < 32; y++)
                {
                    float v = y / 32f;
                    Assert.That(field.Sample(0.49f, v), Is.EqualTo(field.Sample(1.49f, v - 1)));
                }
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(same); }
        }

        [TestCase(BlurMode.Isotropic, 0, true)]
        [TestCase(BlurMode.Directional, 0, true)]
        [TestCase(BlurMode.Directional, 90, false)]
        public void BlurCrossesTheSeamAndRespectsDirection(BlurMode mode, float angle, bool across)
        {
            var r = Stripes();
            r.blur = mode;
            r.blurAngle = angle;
            r.blurRadius = 0.125f;
            var field = new TextureField(r);
            const float halfPixel = 0.5f / 64;
            Assert.That(field.Sample(halfPixel, 0.5f), across ? Is.InRange(0.50f, 0.85f) : Is.EqualTo(1f).Within(0.00001f));
            Assert.That(field.Sample(0.25f, 0.5f), Is.EqualTo(1f).Within(0.00001f));
            Assert.That(field.Sample(1 + halfPixel, 0.5f), Is.EqualTo(field.Sample(halfPixel, 0.5f)));
        }

        [Test]
        public void EveryGeneratorFeedsTheSharedProcessingPipeline()
        {
            foreach (var definition in TextureCatalog.All)
            {
                var r = new TextureRecipe { generator = definition.Id, width = 48, height = 36, cellsX = 3, cellsY = 3,
                    domainWarp = true, blur = BlurMode.Isotropic, blurRadius = 0.04f, detectEdges = true, edgeWidth = 0.04f };
                var texture = TextureGenerator.Generate(r);
                try
                {
                    var pixels = texture.GetPixels32();
                    Assert.That(System.Array.Exists(pixels, pixel => pixel.r != pixels[0].r), Is.True, definition.Id);
                }
                finally { Object.DestroyImmediate(texture); }
            }
        }

        [Test]
        public void SobelFindsTheOutlineAndFeedsTheAlphaRemap()
        {
            var r = new TextureRecipe { generator = "rectangle", sizeX = 0.5f, sizeY = 0.5f, softness = 0,
                width = 64, height = 64, detectEdges = true, edgeWidth = 2f / 64, colorMode = TextureColorMode.AlphaMask };
            var field = new TextureField(r);
            Assert.That(field.Sample(0.25f, 0.5f), Is.EqualTo(1f));
            Assert.That(field.Sample(0.5f, 0.5f), Is.Zero);
            Assert.That(field.Sample(0, 0.5f), Is.Zero);
            r.invert = true;
            var asset = TextureGenerator.Generate(r);
            try
            {
                Assert.That(asset.GetPixels32()[32 * 64 + 16], Is.EqualTo(new Color32(255,255,255,0)));
                Assert.That(asset.GetPixels32()[32 * 64 + 32].a, Is.EqualTo(255));
            }
            finally { Object.DestroyImmediate(asset); }
        }

        [TestCase(128, 64)]
        [TestCase(128, 1)]
        [TestCase(1, 128)]
        [TestCase(1, 1)]
        public void CombinedPassesAgreeAcrossRuntimeBakeAndPreview(int width, int height)
        {
            var r = new TextureRecipe { generator = "curve-gradient", width = width, height = height, sizeX = 1,
                fitTexelEndpoints = true, domainWarp = true, warpSeed = 9, blur = BlurMode.Directional,
                blurAngle = 28, blurRadius = 0.12f, detectEdges = true, edgeWidth = 0.035f };
            var field = new TextureField(r);
            var bake = TextureGenerator.Generate(r);
            var preview = TextureGenerator.GeneratePreview(r, 128);
            try
            {
                var pixels = bake.GetPixels32();
                CollectionAssert.AreEqual(pixels, preview.GetPixels32());
                for (int y = 0; y < height; y += 5)
                    for (int x = 0; x < width; x += 3)
                    {
                        float u = width == 1 ? 0.5f : (float)x / (width - 1);
                        float v = height == 1 ? 0.5f : (float)y / (height - 1);
                        Assert.That(pixels[y * width + x].r, Is.EqualTo(Mathf.RoundToInt(255 * field.Sample(u,v))));
                    }
            }
            finally { Object.DestroyImmediate(bake); Object.DestroyImmediate(preview); }
        }

        [Test]
        public void PreviewUsesProportionalWidthsAndClampsOpenBoundaries()
        {
            var r = new TextureRecipe { generator = "linear", width = 256, height = 128, sizeX = 1,
                blur = BlurMode.Isotropic, blurRadius = 0.10f, detectEdges = true, edgeWidth = 0.02f, edgeStrength = 4 };
            var bake = TextureGenerator.Generate(r);
            var preview = TextureGenerator.GeneratePreview(r, 64);
            try
            {
                float middle = bake.GetPixelBilinear(0.5f, 0.5f).r;
                Assert.That(middle, Is.GreaterThan(0.1f));
                Assert.That(preview.GetPixelBilinear(0.5f,0.5f).r, Is.EqualTo(middle).Within(0.02f));
                Assert.That(bake.GetPixel(0,64).r, Is.Zero);
            }
            finally { Object.DestroyImmediate(bake); Object.DestroyImmediate(preview); }
        }

        [TestCase(90, TextureWrapMode.Clamp, TextureWrapMode.Repeat)]
        [TestCase(30, TextureWrapMode.Clamp, TextureWrapMode.Clamp)]
        [TestCase(0, TextureWrapMode.Repeat, TextureWrapMode.Clamp)]
        public void TransformedRepeatAxesMatchTheGeneratorPeriods(float angle, TextureWrapMode u, TextureWrapMode v)
        {
            var r = new TextureRecipe { generator = "perlin", width = 32, height = 32, seamlessX = true,
                seamlessY = false, coordinateScale = new Vector2(2,1), coordinateRotation = angle,
                domainWarp = true, blur = BlurMode.Isotropic, detectEdges = true };
            var asset = TextureGenerator.Generate(r);
            try
            {
                Assert.That(asset.wrapModeU, Is.EqualTo(u));
                Assert.That(asset.wrapModeV, Is.EqualTo(v));
            }
            finally { Object.DestroyImmediate(asset); }
        }
    }
}
