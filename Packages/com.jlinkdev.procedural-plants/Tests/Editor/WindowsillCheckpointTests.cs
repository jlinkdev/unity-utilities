using System;
using JLink.Plants.Studio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JLink.Plants.Tests
{
    public class WindowsillCheckpointTests
    {
        [Serializable]
        class BenchData
        {
            public int version = 3;
            public PlantSpecimen mother;
            public WindowsillWorkshop.TraySlot[] slots = new WindowsillWorkshop.TraySlot[6];
        }

        [TestCase(.65f), TestCase(1.8f)]
        public void SelectedPotFramesThePlantletAndPreservesTheBench(float aspect)
        {
            var root = new GameObject("Test windowsill");
            try
            {
                Transform Child(string name, Transform parent)
                {
                    var child = new GameObject(name).transform;
                    child.SetParent(parent, false);
                    return child;
                }
                var camera = Child("Camera", root.transform).gameObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.aspect = aspect;
                var bench = root.AddComponent<WindowsillWorkshop>();
                var settings = new SerializedObject(bench);
                settings.FindProperty("viewCamera").objectReferenceValue = camera;
                foreach (var slot in new[] { "mother", "nursery" })
                {
                    var pot = Child(slot, root.transform);
                    pot.localPosition = Vector3.right * (slot == "mother" ? -.49f : .48f);
                    var plant = Child("Plant", pot).gameObject.AddComponent<ProceduralPlant>();
                    settings.FindProperty(slot).objectReferenceValue = plant;
                    settings.FindProperty(slot + "Pole").objectReferenceValue = Child("Pole", pot).gameObject;
                }
                settings.ApplyModifiedPropertiesWithoutUndo();
                bench.ShowSpiderPreset();
                bench.PlantSpiderBaby(bench.ReadyPlantlets()[0]);
                string checkpoint = bench.SaveBench();
                float wide = camera.orthographicSize;
                bench.SetPotCloseUp(true);
                Assert.AreSame(bench.Nursery, bench.Active);
                Assert.Less(camera.orthographicSize, wide * .7f);
                foreach (var vertex in bench.Active.CurrentMesh.vertices)
                {
                    Vector3 p = camera.WorldToViewportPoint(bench.Active.transform.TransformPoint(vertex));
                    Assert.That(p.x, Is.InRange(0f, 1f));
                    Assert.That(p.y, Is.InRange(0f, 1f));
                }
                Assert.AreEqual(checkpoint, bench.SaveBench());
                bench.SetPotCloseUp(false);
                Assert.AreEqual(wide, camera.orthographicSize, .0001f);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void PartialTrayIsRejectedBeforeTheMotherIsReplaced()
        {
            var go = new GameObject("Test windowsill");
            try
            {
                var mother = go.AddComponent<ProceduralPlant>();
                var bench = go.AddComponent<WindowsillWorkshop>();
                var settings = new SerializedObject(bench);
                settings.FindProperty("mother").objectReferenceValue = mother;
                settings.ApplyModifiedPropertiesWithoutUndo();
                var checkpoint = new BenchData { mother = PlantSpecimen.Capture(mother) };
                checkpoint.slots[0] = new WindowsillWorkshop.TraySlot { specimen = checkpoint.mother };
                var recipe = PlantRecipe.Default;
                recipe.species = PlantSpecies.RubberPlant;
                mother.Generate(recipe);
                string original = mother.SaveState();
                Assert.Throws<ArgumentException>(() => bench.RestoreBench(JsonUtility.ToJson(checkpoint)));
                Assert.AreEqual(original, mother.SaveState());
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
