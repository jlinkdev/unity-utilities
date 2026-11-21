using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace JLink.Plants.Tests
{
    public sealed class HouseplantTests
    {
        static PlantRecipe Recipe(PlantSpecies species)
        {
            var r=PlantRecipe.Default;r.species=species;r.seed=81;return r;
        }

        [Test]
        public void PlantletMovesItsExistingRosetteIntoAnIndependentSave()
        {
            var c=PlantConditions.Default;var g=PlantGrowth.Sow(Recipe(PlantSpecies.SpiderPlant),c);
            g.Advance(16,c);Assert.IsFalse(g.CanDetachPlantlet(13));
            g.Advance(14,c);Assert.IsTrue(g.CanDetachPlantlet(13));
            var before=new Mesh();var after=new Mesh();var baby=new Mesh();
            try
            {
                var m=new PlantMesher();m.Write(g,before);
                var offset=g.PlantletPosition(13);
                var daughter=g.DetachPlantlet(13);m.Write(g,after);m.Write(daughter,baby);
                Assert.AreEqual(baby.GetIndexCount(1),before.GetIndexCount(1)-after.GetIndexCount(1));
                var attached=before.vertices;
                foreach(var point in baby.vertices.Where((_,i)=>i%11==0))
                    Assert.IsTrue(attached.Any(v=>(v-(point+offset)).sqrMagnitude<.000000001f));
                Assert.Throws<ArgumentException>(()=>g.DetachPlantlet(13));
                var saved=g.ToJson();
                Assert.IsFalse(PlantGrowth.FromJson(saved).CanDetachPlantlet(13));
                var resumed=PlantGrowth.FromJson(daughter.ToJson());
                daughter.Advance(16,c);resumed.Advance(16,c);
                Assert.AreEqual(daughter.ToJson(),resumed.ToJson());Assert.AreEqual(saved,g.ToJson());
                Assert.Greater(daughter.OrganCount,5);
                m.Write(daughter,baby);Assert.Greater(baby.bounds.size.y,.18f);
            }
            finally {UnityEngine.Object.DestroyImmediate(before);UnityEngine.Object.DestroyImmediate(after);UnityEngine.Object.DestroyImmediate(baby);}
        }

        [Test]
        public void CareIsOptionalReversibleAndOperatesOnMaturePlants()
        {
            var host=new GameObject();var copy=new GameObject();
            try
            {
                var p=host.AddComponent<ProceduralPlant>();var other=copy.AddComponent<ProceduralPlant>();
                var r=Recipe(PlantSpecies.RubberPlant);r.crowns=1;p.Generate(r,40);
                var original=p.CurrentMesh.vertices;string mature=p.SaveState();
                p.Advance(12);Assert.AreEqual(mature,p.SaveState());CollectionAssert.AreEqual(original,p.CurrentMesh.vertices);
                Assert.IsFalse(p.Care.Enabled);p.SetCareEnabled(true);p.Water();p.Advance(32);
                Assert.Greater(p.Care.Stress(r.species),.4f);Assert.AreEqual(mature,p.SaveState());
                Assert.IsTrue(original.Zip(p.CurrentMesh.vertices,(a,b)=>a.y-b.y).Max()>.02f);
                var json=JsonUtility.ToJson(PlantSpecimen.Capture(p));
                JsonUtility.FromJson<PlantSpecimen>(json).Restore(other);
                Assert.AreEqual(p.Care.Soil,other.Care.Soil,.00001f);
                p.Water();CollectionAssert.AreEqual(original,p.CurrentMesh.vertices);
                Assert.Less(other.Care.Soil,.2f);other.SetCareEnabled(false);
                CollectionAssert.AreEqual(original,other.CurrentMesh.vertices);float soil=other.Care.Soil;
                other.Advance(100);Assert.AreEqual(soil,other.Care.Soil);
            }
            finally {UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(copy);}
        }

        [Test]
        public void CareClockIntegratesAcrossDryingAndVariesBySpecies()
        {
            var a=new GameObject();var b=new GameObject();var third=new GameObject();
            try
            {
                var p=a.AddComponent<ProceduralPlant>();var q=b.AddComponent<ProceduralPlant>();
                var rubber=third.AddComponent<ProceduralPlant>();
                var recipe=Recipe(PlantSpecies.SpiderPlant);
                p.Generate(recipe);q.Generate(recipe);rubber.Generate(Recipe(PlantSpecies.RubberPlant));
                foreach(var v in new[]{p,q,rubber}) v.SetCareEnabled(true);
                p.Advance(16);rubber.Advance(16);
                for(int i=0;i<32;i++)q.Advance(.5);
                Assert.AreEqual(p.Growth.Age,q.Growth.Age,.00001);
                Assert.AreEqual(p.Care.Soil,q.Care.Soil,.00001);
                Assert.Greater(p.Care.Stress(recipe.species),rubber.Care.Stress(PlantSpecies.RubberPlant));
                var before=PlantSpecimen.Capture(p);Assert.Throws<ArgumentOutOfRangeException>(()=>p.Advance(-1));
                Assert.AreEqual(before.care.Soil,p.Care.Soil);Assert.AreEqual(before.growth,p.SaveState());
            }
            finally {UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);UnityEngine.Object.DestroyImmediate(third);}
        }
    }
}
