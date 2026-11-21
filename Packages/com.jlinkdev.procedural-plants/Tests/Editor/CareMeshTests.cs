using System;
using NUnit.Framework;
using UnityEngine;

namespace JLink.Plants.Tests
{
    public class CareMeshTests
    {
        GameObject pot;
        ProceduralPlant plant;
        Mesh expected;

        [SetUp]
        public void SetUp()
        {
            pot = new GameObject("Test pot");
            plant = pot.AddComponent<ProceduralPlant>();
            expected = new Mesh();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(pot);
            UnityEngine.Object.DestroyImmediate(expected);
        }

        void AssertMatchesFullBuild()
        {
            new PlantMesher().Write(plant.Growth, expected, plant.Detail, plant.Care);
            var actual = plant.CurrentMesh;
            CollectionAssert.AreEqual(expected.vertices, actual.vertices);
            CollectionAssert.AreEqual(expected.colors32, actual.colors32);
            CollectionAssert.AreEqual(expected.normals, actual.normals);
            CollectionAssert.AreEqual(expected.tangents, actual.tangents);
            CollectionAssert.AreEqual(expected.uv, actual.uv);
            CollectionAssert.AreEqual(expected.uv3, actual.uv3);
            for (int slot = 0; slot < 2; slot++)
                CollectionAssert.AreEqual(expected.GetTriangles(slot), actual.GetTriangles(slot));
            Assert.AreEqual(expected.bounds, actual.bounds);
        }

        [TestCase(PlantSpecies.Pothos), TestCase(PlantSpecies.SnakePlant), TestCase(PlantSpecies.Fern)]
        [TestCase(PlantSpecies.Monstera), TestCase(PlantSpecies.SpiderPlant), TestCase(PlantSpecies.RubberPlant)]
        [TestCase(PlantSpecies.JadePlant)]
        public void CareOnlyUpdatesMatchAFullBuild(PlantSpecies species)
        {
            var recipe = PlantRecipe.Default;
            recipe.species = species;
            recipe.crowns = 1;
            plant.Generate(recipe, 250);
            plant.SetCareEnabled(true);
            plant.Advance(100);
            AssertMatchesFullBuild();
            plant.Water(.10f);
            AssertMatchesFullBuild();
            plant.Water();
            AssertMatchesFullBuild();
        }

        [Test]
        public void ChangingTheGrowthRecordInvalidatesTheLeafPose()
        {
            var recipe = PlantRecipe.Default;
            recipe.species = PlantSpecies.SpiderPlant;
            plant.Generate(recipe, 50);
            plant.SetCareEnabled(true);
            plant.Growth.DetachPlantlet(13);
            plant.Advance(0);
            AssertMatchesFullBuild();
            plant.Detail = LeafDetail.Simple;
            pot.SetActive(false);
            plant.Advance(45);
            pot.SetActive(true);
            AssertMatchesFullBuild();
        }

        [Test]
        public void AWateredDaughterAndDryMotherResumeIndependently()
        {
            var nursery = new GameObject("Nursery");
            try
            {
                var child = nursery.AddComponent<ProceduralPlant>();
                var recipe = PlantRecipe.Default;
                recipe.species = PlantSpecies.SpiderPlant;
                plant.Generate(recipe, 30);
                plant.SetCareEnabled(true);
                plant.Advance(20);
                var cutting = PlantSpecimen.Capture(plant, plant.DetachPlantlet(13));
                cutting.Restore(child);
                Assert.IsTrue(child.Care.Enabled);
                Assert.AreEqual(plant.Care.Soil, child.Care.Soil);
                Assert.Less(child.Care.Soil, .05f);
                child.Water();
                Assert.AreEqual(0f, plant.Care.Soil);
                string motherSave = JsonUtility.ToJson(PlantSpecimen.Capture(plant));
                string childSave = JsonUtility.ToJson(PlantSpecimen.Capture(child));

                plant.Advance(3);
                child.Advance(4);
                string nextMother = plant.SaveState(), nextChild = child.SaveState();
                float nextSoil = child.Care.Soil;
                JsonUtility.FromJson<PlantSpecimen>(motherSave).Restore(plant);
                JsonUtility.FromJson<PlantSpecimen>(childSave).Restore(child);
                child.Advance(4);
                plant.Advance(3);
                Assert.AreEqual(nextMother, plant.SaveState());
                Assert.AreEqual(nextChild, child.SaveState());
                Assert.AreEqual(nextSoil, child.Care.Soil);
                Assert.IsFalse(plant.Growth.CanDetachPlantlet(13));
                Assert.Greater(child.Growth.OrganCount, 6);
            }
            finally { UnityEngine.Object.DestroyImmediate(nursery); }
        }

        [Test]
        public void InvalidSpecimenSettingsLeaveTheDestinationIntact()
        {
            string original = plant.SaveState();
            var invalid = PlantSpecimen.Capture(plant);
            invalid.detail = (LeafDetail)99;
            Assert.Throws<ArgumentException>(() => invalid.Restore(plant));
            Assert.AreEqual(original, plant.SaveState());
            Assert.AreEqual(LeafDetail.Standard, plant.Detail);
        }
    }
}
