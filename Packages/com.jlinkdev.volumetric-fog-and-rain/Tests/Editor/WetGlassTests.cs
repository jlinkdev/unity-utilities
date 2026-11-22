using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain.Tests
{
    public sealed class WetGlassTests
    {
        [Test]
        public void OrdinaryLitMaterialStillUsesColorAndSmoothnessOverrides()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var renderer = slab.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Smoothness", .2f);
                var state = new RainSurfaceState();
                state.SetWetness(.5f);
                state.Apply(new Renderer[] { renderer }, false, 1, .4f, .8f);
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetColor("_BaseColor").r, Is.EqualTo(.8f).Within(.0001f));
                Assert.That(block.GetFloat("_Smoothness"), Is.EqualTo(.5f).Within(.0001f));
                Assert.That(material.GetFloat("_Smoothness"), Is.EqualTo(.2f));
            }
            finally
            {
                Object.DestroyImmediate(slab);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void OpaqueWetMaterialRetainsItsAuthoredTintAndResumesThePreviewAfterUse()
        {
            var shader = Shader.Find("jlinkdev/Volumetric Fog and Rain/Wet Surface");
            Assert.That(shader, Is.Not.Null);
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
            var material = new Material(shader);
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var renderer = slab.GetComponent<Renderer>();
            var surfaces = new Renderer[] { renderer };
            var state = new RainSurfaceState();
            var block = new MaterialPropertyBlock();
            try
            {
                material.SetFloat("_Wetness", .7f);
                renderer.sharedMaterial = material;
                state.SetWetness(.7f);
                state.Advance(1, true, 1, 10, 10);
                state.Apply(surfaces, false, 1, .5f, .9f);
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_Wetness"), Is.EqualTo(.8f).Within(.0001f));
                Assert.That(block.GetFloat("_RippleActivity"), Is.EqualTo(1f));
                Assert.That(block.GetFloat("_BeadTime"), Is.EqualTo(1));
                Assert.That(block.HasColor("_BaseColor"), Is.False);
                state.Advance(0, false, .25f, 10, 10);
                state.Apply(surfaces, false, 1.25f, .5f, .9f);
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_RippleActivity"), Is.GreaterThan(0f).And.LessThan(1f),
                    "Existing waves should settle gradually after the rain stops.");
                state.Advance(0, false, .75f, 10, 10);
                state.Apply(surfaces, false, 2, .5f, .9f);
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_RippleActivity"), Is.Zero, "The film should be calm within one second.");
                Assert.That(block.GetFloat("_Wetness"), Is.EqualTo(.7f).Within(.0001f));
                Assert.That(block.GetFloat("_RainTime"), Is.EqualTo(2));
                Assert.That(block.GetFloat("_BeadTime"), Is.EqualTo(1));
                state.Restore();
                renderer.GetPropertyBlock(block);
                Assert.That(block.isEmpty, Is.True);
                Assert.That(material.GetFloat("_Wetness"), Is.EqualTo(.7f));
            }
            finally
            {
                Object.DestroyImmediate(slab);
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void BeadReplacementStopsWhileGlassDriesAndResetRestoresRendererOverrides()
        {
            var pane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var renderer = pane.GetComponent<Renderer>();
            var surfaces = new Renderer[] { renderer };
            var block = new MaterialPropertyBlock();
            var state = new RainSurfaceState();
            try
            {
                block.SetFloat("_Seed", 11);
                renderer.SetPropertyBlock(block);
                state.Advance(1, true, 8, 8, 55);
                state.Apply(surfaces, true, 8, 0, 0);
                state.Advance(0, false, 5, 8, 55);
                state.Apply(surfaces, true, 13, 0, 0);
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_Wetness"), Is.EqualTo(1 - 5f / 55f).Within(.0001f));
                Assert.That(block.GetFloat("_BeadTime"), Is.EqualTo(8));
                Assert.That(block.GetFloat("_RainTime"), Is.EqualTo(13));
                state.ResetWetness();
                renderer.GetPropertyBlock(block);
                Assert.That(block.GetFloat("_Seed"), Is.EqualTo(11));
                Assert.That(block.GetFloat("_Wetness"), Is.Zero);
                Assert.That(block.GetFloat("_BeadTime"), Is.Zero);
            }
            finally { Object.DestroyImmediate(pane); }
        }
    }
}
