using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JLink.Plants.Tests
{
    public sealed class PlantGrowthTests
    {
        static PlantRecipe Recipe(PlantSpecies species)
        {
            var recipe = PlantRecipe.Default;
            recipe.species = species;
            return recipe;
        }

        [TestCase(PlantSpecies.Pothos), TestCase(PlantSpecies.SnakePlant), TestCase(PlantSpecies.Fern)]
        [TestCase(PlantSpecies.JadePlant)]
        public void GenerationMatchesIncrementalGrowthAndResume(PlantSpecies species)
        {
            var c = PlantConditions.Default;
            var immediate = PlantGrowth.Sow(Recipe(species),c);
            var incremental = PlantGrowth.Sow(Recipe(species),c);
            immediate.Advance(16,c);
            for (int i=0; i<32; i++) incremental.Advance(.5,c);
            Assert.That(incremental.ToJson(),Is.EqualTo(immediate.ToJson()));
            var restored = PlantGrowth.FromJson(incremental.ToJson());
            c.lightDirection = Vector3.left;
            restored.Advance(3.25,c);
            immediate.Advance(3.25,c);
            Assert.That(restored.ToJson(),Is.EqualTo(immediate.ToJson()));
        }

        [TestCase(PlantSpecies.Pothos), TestCase(PlantSpecies.SnakePlant), TestCase(PlantSpecies.Fern)]
        public void LightChangesNewGeometryAndPreservesMatureOrgans(PlantSpecies species)
        {
            var c = PlantConditions.Default;
            var plant = PlantGrowth.Sow(Recipe(species),c);
            var prefix = new Mesh();
            var left = new Mesh();
            var right = new Mesh();
            try
            {
                var writer = new PlantMesher();
                writer.Write(plant,prefix);
                int firstOrganVertices = prefix.vertexCount;
                plant.Advance(9,c);
                writer.Write(plant,prefix);
                var mature = prefix.vertices.Take(firstOrganVertices).ToArray();
                var twin = PlantGrowth.FromJson(plant.ToJson());
                c.lightDirection = Vector3.left;
                c.lightResponse = 1;
                plant.Advance(4,c);
                c.lightDirection = Vector3.right;
                twin.Advance(4,c);
                writer.Write(plant,left); writer.Write(twin,right);
                CollectionAssert.AreEqual(mature,left.vertices.Take(firstOrganVertices));
                CollectionAssert.AreEqual(mature,right.vertices.Take(firstOrganVertices));
                Assert.That(left.vertices.SequenceEqual(right.vertices),Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(prefix); UnityEngine.Object.DestroyImmediate(left); UnityEngine.Object.DestroyImmediate(right); }
        }

        [TestCase(PlantSpecies.Pothos), TestCase(PlantSpecies.SnakePlant), TestCase(PlantSpecies.Fern)]
        public void MaximumPlantHasFiniteIndexedGeometryAndUVs(PlantSpecies species)
        {
            var recipe = Recipe(species);
            recipe.crowns = 7;
            recipe.seed = -190;
            var plant = PlantGrowth.Sow(recipe,PlantConditions.Default);
            plant.Advance(double.MaxValue,PlantConditions.Default);
            Assert.That(plant.FullyGrown && plant.OrganCount == plant.Capacity);
            var mesh = new Mesh();
            try
            {
                new PlantMesher().Write(plant,mesh);
                Assert.That(mesh.vertexCount,Is.GreaterThan(100));
                Assert.That(mesh.subMeshCount,Is.EqualTo(2));
                Assert.That(mesh.GetIndexCount(1),Is.GreaterThan(0));
                Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.colors32.Length,Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.tangents.Length,Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.vertices.All(v => float.IsFinite(v.sqrMagnitude)));
                Assert.That(mesh.uv.All(uv => uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1));
                Assert.That(mesh.triangles.All(i => i >= 0 && i < mesh.vertexCount));
                if (species == PlantSpecies.Fern) Assert.That(mesh.vertexCount,Is.GreaterThan(65535));
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void SaveRestoresExactClockAtLaterGrowthAges()
        {
            var plant = PlantGrowth.Sow(PlantRecipe.Default, PlantConditions.Default);
            plant.Advance(44,PlantConditions.Default);
            var restored = PlantGrowth.FromJson(plant.ToJson());
            Assert.AreEqual(BitConverter.DoubleToInt64Bits(plant.Age),BitConverter.DoubleToInt64Bits(restored.Age));
            Assert.AreEqual(plant.ToJson(),restored.ToJson());
        }

        [Test]
        public void MoistureControlsRateAndInvalidInputLeavesPlantIntact()
        {
            var c = PlantConditions.Default;
            var a = PlantGrowth.Sow(PlantRecipe.Default,c);
            var b = PlantGrowth.Sow(PlantRecipe.Default,c);
            c.moisture = 0; a.Advance(8,c);
            c.moisture = 1; b.Advance(8,c);
            Assert.That(b.Age,Is.GreaterThan(a.Age));
            string original = a.ToJson();
            Assert.Throws<ArgumentOutOfRangeException>(() => a.Advance(double.NaN,c));
            Assert.That(a.ToJson(),Is.EqualTo(original));
            Assert.Throws<ArgumentException>(() => PlantGrowth.FromJson("{}"));
            Assert.Throws<ArgumentException>(() => PlantGrowth.FromJson(original.Replace("\"version\":2","\"version\":999")));
        }

        [TestCase(PlantSpecies.Pothos), TestCase(PlantSpecies.JadePlant)]
        [TestCase(PlantSpecies.SpiderPlant), TestCase(PlantSpecies.RubberPlant), TestCase(PlantSpecies.SnakePlant)]
        [TestCase(PlantSpecies.Monstera), TestCase(PlantSpecies.Fern)]
        public void BakeSurvivesRegenerationAndComponentRemoval(PlantSpecies species)
        {
            var go = new GameObject();
            var path = "Assets/PlantBake-" + Guid.NewGuid().ToString("N") + ".asset";
            try
            {
                var plant = go.AddComponent<ProceduralPlant>();
                plant.Generate(Recipe(species),18);
                var baked = JLink.Plants.Editor.PlantMeshBaker.Bake(plant,path);
                var vertices = baked.vertices;
                var uvs = baked.uv;
                var pattern = baked.uv3;
                Assert.That(baked,Is.Not.SameAs(plant.CurrentMesh));
                CollectionAssert.AreEqual(plant.CurrentMesh.uv,uvs);
                plant.Generate(Recipe(PlantSpecies.Fern),7);
                UnityEngine.Object.DestroyImmediate(plant);
                Assert.That(go.GetComponent<MeshFilter>().sharedMesh,Is.Null);
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                CollectionAssert.AreEqual(vertices,saved.vertices);
                CollectionAssert.AreEqual(uvs,saved.uv);
                CollectionAssert.AreEqual(pattern,saved.uv3);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
