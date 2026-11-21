using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace JLink.Plants.Tests
{
    public sealed class LeafTopologyTests
    {
        [TestCase(PlantSpecies.Pothos), TestCase(PlantSpecies.SnakePlant), TestCase(PlantSpecies.Fern), TestCase(PlantSpecies.Monstera)]
        [TestCase(PlantSpecies.SpiderPlant), TestCase(PlantSpecies.RubberPlant), TestCase(PlantSpecies.JadePlant)]
        public void AllDetailLevelsProduceClosedBladesAndKeepTheGrowthRecord(PlantSpecies species)
        {
            var recipe = PlantRecipe.Default;
            recipe.species = species;
            if (species == PlantSpecies.Monstera) recipe.crowns = 1;
            var plant = PlantGrowth.Sow(recipe,PlantConditions.Default);
            plant.Advance(species == PlantSpecies.SpiderPlant ? 29 : species == PlantSpecies.Monstera ? 10 : 6.5,PlantConditions.Default);
            string initial = plant.ToJson();
            var mesh = new Mesh();
            var writer = new PlantMesher();
            try
            {
                int previousCount = 0;
                foreach (LeafDetail detail in System.Enum.GetValues(typeof(LeafDetail)))
                {
                    writer.Write(plant,mesh,detail);
                    var triangles = mesh.GetTriangles(1);
                    Assert.Greater(triangles.Length,previousCount);
                    previousCount = triangles.Length;
                    var vertices = mesh.vertices;
                    var uv = mesh.uv;
                    var edgeUses = new Dictionary<(int,int),int>();
                    var orientation = new Dictionary<(int,int),int>();
                    for (int i=0; i<triangles.Length; i+=3)
                    {
                        int a = triangles[i], b = triangles[i+1], c = triangles[i+2];
                        var normal = Vector3.Cross(vertices[b]-vertices[a], vertices[c]-vertices[a]);
                        Assert.Greater(normal.sqrMagnitude,0, "Collapsed leaf triangle");
                        Vector2 ab = uv[b]-uv[a], ac = uv[c]-uv[a];
                        Assert.Greater(Mathf.Abs(ab.x*ac.y-ab.y*ac.x),0,"Collapsed leaf UV triangle");
                        CountEdge(a,b); CountEdge(b,c); CountEdge(c,a);
                    }
                    foreach (var entry in edgeUses)
                    {
                        Assert.AreEqual(2,entry.Value,"Open or non-manifold leaf edge");
                        Assert.AreEqual(0,orientation[entry.Key],"Inconsistent triangle winding");
                    }

                    void CountEdge(int a,int b)
                    {
                        var key = (Mathf.Min(a,b),Mathf.Max(a,b));
                        edgeUses.TryGetValue(key,out int uses);
                        orientation.TryGetValue(key,out int winding);
                        edgeUses[key] = uses+1;
                        orientation[key] = winding+(a<b ? 1 : -1);
                    }
                }
                Assert.AreEqual(initial,plant.ToJson());
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
