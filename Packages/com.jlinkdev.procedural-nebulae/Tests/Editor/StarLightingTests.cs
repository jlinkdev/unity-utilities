using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace jlinkdev.ProceduralNebulae.Tests
{
    public class StarLightingTests
    {
        // Uniform density has an analytic integral, independent of the number of light steps.
        [TestCase(.8125f,1f,.25f)]
        [TestCase(4f,1f,.4375f)]
        [TestCase(-3f,1f,.5625f)]
        [TestCase(.8125f,0f,0f)] // Excited gas alone casts no dust shadow.
        [TestCase(.8125f,1f,.25f,1)]
        [TestCase(.8125f,0f,0f,1)]
        [TestCase(.8125f,1f,.25f,2)]
        [TestCase(.8125f,0f,0f,2)]
        [TestCase(.8125f,1f,.25f,3)]
        [TestCase(.8125f,0f,0f,3)]
        [TestCase(.8125f,1f,.25f,4)]
        [TestCase(.8125f,0f,0f,4)]
        public void ShadowIntegralEndsAtStarOrBoxFace(float starX, float dust, float expected, int shape = 0)
        {
            CheckShadow(starX,new Color(dust,1,.12f,-.37f),expected,shape,0);
        }

        [TestCase(0f,.012f,.35f)] // A protrusion occupies the previously empty cell.
        [TestCase(1f,-.012f,0f)] // A recess opens in previously solid dust.
        public void FineSurfaceChangesTheStellarShadow(float dust,float distance,float expected)
        {
            CheckShadow(.8125f,new Color(dust,0,1,distance),expected,(int)NebulaShape.SculptedBank,1);
        }

        [TestCase(.10f,.25f)]
        [TestCase(-.10f,0f)]
        public void SweptArcCastsShadowsFromItsBankAndLeavesTheOpeningClear(float edge,float expected)
        {
            CheckShadow(.8125f,new Color(1,0,1,edge),expected,(int)NebulaShape.SweptArc,0);
        }

        [TestCase(NebulaShape.PainterlyCavity)]
        [TestCase(NebulaShape.SweptArc)]
        public void PatternedDustUsesThePhysicalClearingOnTheStarRay(NebulaShape shape)
        {
            // The ray starts at the clearing centre. Its smooth boundary
            // removes half the transition width from the integrated distance.
            const float expected = .25f-.082f-.024f/2;
            CheckShadow(.8125f,new Color(1,0,1,1),expected,(int)shape,0,NebulaStructureWarp.CarapacePlates);
        }

        [Test]
        public void ShadowOnlyUpdateUsesItsOwnVolumeAfterAnotherFieldBakes()
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsAsyncGPUReadback)
                Assert.Ignore("GPU compute and readback required.");
            if (!SystemInfo.supports3DTextures ||
                !SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, GraphicsFormatUsage.LoadStore) ||
                !SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.LoadStore))
                Assert.Ignore("Floating-point 3D storage textures required.");

            var root = new GameObject("Two nebula volumes");
            var profile = ScriptableObject.CreateInstance<NebulaStyleProfile>();
            try
            {
                profile.settings.structureWarp = NebulaStructureWarp.CarapacePlates;
                var first = root.AddComponent<NebulaVolume>();
                first.style = profile;
                first.clearingRadius = 0;
                var light = new GameObject("Star");
                light.transform.SetParent(root.transform);
                first.star = light.AddComponent<NebulaStar>();
                Assert.IsTrue(first.PrepareForRendering());
                var original = ReadShadowSlice(first);

                var other = new GameObject("Other field");
                other.transform.SetParent(root.transform);
                Assert.IsTrue(other.AddComponent<NebulaVolume>().PrepareForRendering());
                light.transform.localPosition = new Vector3(42, 16, -8);
                Assert.IsTrue(first.PrepareForRendering());
                var relit = ReadShadowSlice(first);

                first.enabled = false;
                first.enabled = true;
                Assert.IsTrue(first.PrepareForRendering());
                CollectionAssert.AreNotEqual(original, relit);
                CollectionAssert.AreEqual(ReadShadowSlice(first), relit);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(profile);
            }
        }

        static ushort[] ReadShadowSlice(NebulaVolume volume)
        {
            var depth = volume.PrimaryOpticalDepth;
            Assert.IsNotNull(depth, "Expected a primary star shadow cache.");
            var read = AsyncGPUReadback.Request(depth, 0);
            read.WaitForCompletion();
            Assert.IsFalse(read.hasError);
            return read.GetData<ushort>(depth.volumeDepth / 2).ToArray();
        }

        static void CheckShadow(float starX, Color field, float expected, int shape, float surfaceDetail,
                                NebulaStructureWarp pattern = NebulaStructureWarp.None)
        {
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsAsyncGPUReadback)
                Assert.Ignore("GPU compute and readback required.");
            const int size = 8;
            var shader = Resources.Load<ComputeShader>("NebulaField");
            var input = new Texture3D(size,size,size,TextureFormat.RGBAFloat,false);
            var output = new RenderTexture(size,size,0,GraphicsFormat.R16_SFloat)
            {
                dimension=TextureDimension.Tex3D, volumeDepth=size, enableRandomWrite=true
            };
            try
            {
                var medium = new Color[size*size*size];
                System.Array.Fill(medium,field);
                input.SetPixels(medium);
                input.Apply();
                Assert.That(output.Create(), Is.True);
                int kernel = shader.FindKernel("StarOpticalDepth");
                shader.SetInt("_Resolution",size);
                shader.SetVector("_StarUVW",new Vector3(starX,.5625f,.5625f));
                shader.SetTexture(kernel,"_Density",input);
                shader.SetTexture(kernel,"_Detail",input);
                shader.SetFloat("_Wisps",0);
                shader.SetFloat("_FoldRelief",0);
                shader.SetInt("_LayeredCurtain",0);
                shader.SetInt("_StructureWarp",0);
                shader.SetVector("_DetailShift",Vector3.zero);
                shader.SetVector("_WarpPhase",Vector3.zero);
                shader.SetFloat("_SurfaceDetail",surfaceDetail);
                shader.SetFloat("_CloudBody",1);
                shader.SetFloat("_EdgeSoftness",.028f);
                shader.SetInt("_NebulaShape",shape);
                shader.SetInt("_DomainPattern",(int)pattern);
                shader.SetFloat("_DomainStrength",1);
                shader.SetVector("_Clearing",new Vector4(.5625f,.5625f,.5625f,.082f));
                shader.SetFloat("_ClearingSoftness",.024f);
                shader.SetTexture(kernel,"_Output",output);
                shader.Dispatch(kernel,2,2,2);
                var read = AsyncGPUReadback.Request(output,0,TextureFormat.RFloat);
                read.WaitForCompletion();
                Assert.That(read.hasError,Is.False);
                NativeArray<float> slice = read.GetData<float>(4);
                Assert.That(slice[4+size*4],Is.EqualTo(expected).Within(.001f));
            }
            finally
            {
                Object.DestroyImmediate(input);
                output.Release();
                Object.DestroyImmediate(output);
            }
        }
    }
}
