using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace JLink.Plants.Tests
{
    public class JadeTests
    {
        static PlantGrowth Sow(int seed, int crowns)
        {
            var recipe = PlantRecipe.Default;
            recipe.species = PlantSpecies.JadePlant;
            recipe.seed = seed;
            recipe.crowns = crowns;
            return PlantGrowth.Sow(recipe, PlantConditions.Default);
        }

        [TestCase(1), TestCase(3), TestCase(7)]
        public void ForksStayAttachedAcrossSaveAndResume(int crowns)
        {
            var plant = Sow(47, crowns);
            var conditions = PlantConditions.Default;
            plant.Advance(10 * crowns, conditions);
            var resumed = PlantGrowth.FromJson(plant.ToJson());
            plant.Advance(6 * crowns, conditions);
            resumed.Advance(6 * crowns, conditions);
            Assert.AreEqual(plant.ToJson(), resumed.ToJson());
            var childCounts = new Dictionary<int, int>();
            int roots = 0;
            for (int i = 0; i < resumed.OrganCount; i++)
            {
                var node = resumed.NodeAt(i);
                if (node.ParentId == 0) { roots++; continue; }
                var parent = resumed.NodeAt(node.ParentId - 1);
                Assert.Less((parent.Tip - node.Base).magnitude, .000001f);
                childCounts.TryGetValue(node.ParentId, out int siblings);
                childCounts[node.ParentId] = siblings + 1;
            }
            Assert.AreEqual(crowns, roots);
            Assert.IsTrue(childCounts.Values.Any(count => count == 2));
        }

        [Test]
        public void EveryNodeGrowsOneOppositePair()
        {
            var plant = Sow(47, 1);
            plant.Advance(45, PlantConditions.Default);
            var mesh = new Mesh();
            try
            {
                var mesher = new PlantMesher();
                mesher.Write(plant, mesh);
                var uv = mesh.uv;
                var patterns = mesh.uv3;
                var vertices = mesh.vertices;
                var tips = Enumerable.Range(0, vertices.Length).Where(i => patterns[i].x == 8 && uv[i].y == 1).ToArray();
                Assert.AreEqual(plant.OrganCount * 2, tips.Length);
                for (int n = 0; n < plant.OrganCount; n++)
                {
                    var node = plant.NodeAt(n);
                    var stem = (node.Tip - node.Base).normalized;
                    var first = Vector3.ProjectOnPlane(vertices[tips[n * 2]] - node.Tip, stem);
                    var second = Vector3.ProjectOnPlane(vertices[tips[n * 2 + 1]] - node.Tip, stem);
                    Assert.Greater(first.magnitude, .03f);
                    Assert.Less(Vector3.Dot(first.normalized, second.normalized), -.995f);
                }
                var other = Sow(82, 1);
                other.Advance(45, PlantConditions.Default);
                mesher.Write(other, mesh);
                Assert.AreNotEqual(vertices[tips[0]], mesh.vertices[tips[0]]);
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
