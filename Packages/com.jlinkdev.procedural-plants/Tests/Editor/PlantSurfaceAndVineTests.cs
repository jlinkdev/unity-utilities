using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JLink.Plants.Tests
{
    public sealed class PlantSurfaceAndVineTests
    {
        [Test]
        public void PbrUsesImportedLinearDataAndKeepsTheSameSpecimen()
        {
            var go = new GameObject();
            try
            {
                var plant = go.AddComponent<ProceduralPlant>();
                var mesh = plant.CurrentMesh;
                string saved = plant.SaveState();
                plant.Surface = PlantSurface.PBR;
                var m = go.GetComponent<MeshRenderer>().sharedMaterials[1];
                Assert.AreEqual("JLink/Plants/Botanical PBR",m.shader.name);
                Assert.IsFalse(ShaderUtil.ShaderHasError(m.shader),
                    string.Join("\n",ShaderUtil.GetShaderMessages(m.shader).Select(s=>s.message)));
                TextureImporter Importer(string slot) =>
                    (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m.GetTexture(slot)));
                Assert.IsTrue(Importer("_BaseMap").sRGBTexture);
                Assert.IsFalse(Importer("_MaskMap").sRGBTexture);
                Assert.AreEqual(TextureImporterType.NormalMap,Importer("_BumpMap").textureType);
                Assert.AreSame(mesh,plant.CurrentMesh);
                Assert.AreEqual(saved,plant.SaveState());
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void LongPresetHangsBelowSoilAndRoundTripsAtFullLength()
        {
            var recipe = PlantRecipe.Default;
            recipe.seed = 63; recipe.crowns = 5; recipe.leafSize = .91f;
            var plant = PlantGrowth.Sow(recipe,PlantConditions.Default);
            plant.Advance(90,PlantConditions.Default);
            var mesh = new Mesh();
            try
            {
                var mesher = new PlantMesher();
                mesher.Write(plant,mesh);
                Assert.Less(mesh.bounds.min.y,-1f);
                var shape = mesh.vertices;
                mesher.Write(PlantGrowth.FromJson(plant.ToJson()),mesh);
                CollectionAssert.AreEqual(shape,mesh.vertices);
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void SavedStraightVineKeepsItsEndpointAndCanGrowCurvedChildren()
        {
            const string json = "{\"version\":1,\"recipe\":{\"species\":0,\"seed\":37,\"crowns\":1,\"spread\":1,\"leafSize\":1},\"age\":\"0\",\"organs\":[{\"parent\":-1,\"depth\":0,\"origin\":{\"x\":0,\"y\":0,\"z\":0},\"direction\":{\"x\":0,\"y\":1,\"z\":0},\"outward\":{\"x\":1,\"y\":0,\"z\":0},\"length\":0.1,\"size\":1,\"variation\":0.5,\"birth\":0}]}";
            var plant = PlantGrowth.FromJson(json);
            plant.Advance(8,PlantConditions.Default);
            Assert.Greater(plant.OrganCount,1);
            var mesh = new Mesh();
            try
            {
                new PlantMesher().Write(plant,mesh);
                // A v1 straight tube interleaves base and tip vertices in the first ring.
                Assert.AreEqual(.1f,mesh.vertices[1].y,.00001f);
                Assert.AreEqual(plant.ToJson(),PlantGrowth.FromJson(plant.ToJson()).ToJson());
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
