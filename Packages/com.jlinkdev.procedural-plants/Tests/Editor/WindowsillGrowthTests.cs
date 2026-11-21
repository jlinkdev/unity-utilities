using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JLink.Plants.Tests
{
    public sealed class WindowsillGrowthTests
    {
        static PlantRecipe Single(PlantSpecies species=PlantSpecies.Pothos)
        { var r=PlantRecipe.Default;r.crowns=1;r.species=species;return r; }

        [Test]
        public void MatureParentActivatesTheRetainedBudAndBothPlantsResume()
        {
            var c=PlantConditions.Default;
            var plant=PlantGrowth.Sow(Single(),c);
            plant.Advance(1000,c);
            Assert.IsTrue(plant.FullyGrown);
            var cut=plant.NodeAt(7);
            var subtree=plant.PreviewCut(cut.Id);
            var before=Enumerable.Range(0,plant.OrganCount).Select(plant.NodeAt).ToDictionary(n=>n.Id);
            var clipping=plant.Cut(cut.Id);
            Assert.AreEqual(before.Count,plant.OrganCount+clipping.OrganCount);
            Assert.AreEqual(Vector3.zero,clipping.NodeAt(0).Base);
            var other=PlantGrowth.FromJson(clipping.ToJson());
            for(int i=0;i<clipping.OrganCount;i++)
            {
                var n=clipping.NodeAt(i);
                Assert.Contains(n.Id,subtree);
                Assert.Less((n.Tip+cut.Base-before[n.Id].Tip).magnitude,.00001f);
                Assert.AreEqual(PlantContact.Free,n.Contact);
            }
            foreach(var n in Enumerable.Range(0,plant.OrganCount).Select(plant.NodeAt))
                Assert.AreEqual(before[n.Id].Tip,n.Tip);
            var resumed=PlantGrowth.FromJson(plant.ToJson());
            int count=resumed.OrganCount;
            resumed.Advance(1.2,c);
            Assert.AreEqual(cut.ParentId,resumed.NodeAt(count).ParentId);
            Assert.Greater(resumed.NodeAt(count).Id,before.Keys.Max());
            clipping.Advance(3,c);other.Advance(3,c);
            Assert.AreEqual(other.ToJson(),clipping.ToJson());
            Assert.AreEqual(count,plant.OrganCount);
        }

        [Test]
        public void HangingCuttingPlantsBaseFirstAndPreservesStemLengths()
        {
            var p=PlantGrowth.Sow(Single(),PlantConditions.Default);
            p.Advance(100,PlantConditions.Default);
            var twin=PlantGrowth.FromJson(p.ToJson());
            var node=Enumerable.Range(0,p.OrganCount).Select(p.NodeAt).First(n=>n.Depth>7 && n.Tip.y<n.Base.y);
            var raw=p.Cut(node.Id);
            var planted=twin.Cut(node.Id,plantUpright:true);
            Assert.AreEqual(Vector3.zero,planted.NodeAt(0).Base);
            Assert.Greater(planted.NodeAt(0).Tip.y,0);
            Assert.Greater(Enumerable.Range(0,planted.OrganCount).Sum(i=>planted.NodeAt(i).Tip.y),0);
            for(int i=0;i<raw.OrganCount;i++)
                Assert.AreEqual(raw.NodeAt(i).Tip.magnitude,planted.NodeAt(i).Tip.magnitude,.00001f);
        }

        [Test]
        public void TwoCutsBeforeAdvancingKeepBothBudsAcrossSave()
        {
            var r=PlantRecipe.Default; r.crowns=2;
            var c=PlantConditions.Default;
            var p=PlantGrowth.Sow(r,c); p.Advance(120,c);
            var all=Enumerable.Range(0,p.OrganCount).Select(p.NodeAt).ToArray();
            var roots=all.Where(n=>n.ParentId==0).ToArray();
            var cutA=all.First(n=>n.ParentId==roots[0].Id);
            var cutB=all.First(n=>n.ParentId==roots[1].Id);
            p.Cut(cutA.Id);p.Cut(cutB.Id);
            p=PlantGrowth.FromJson(p.ToJson());
            int before=p.OrganCount;
            p.Advance(2.5,c);
            Assert.AreEqual(roots[0].Id,p.NodeAt(before).ParentId);
            Assert.AreEqual(roots[1].Id,p.NodeAt(before+1).ParentId);
        }

        [Test]
        public void ShelfContactReleasesAtTheEdgeAndSurvivesMovingTheSupport()
        {
            var c=PlantConditions.Default;
            var supports=PlantSupports.Windowsill;
            supports.shelfXZ=Rect.MinMaxRect(-.60f,-.60f,.60f,.60f);
            supports.shelfHeight=-.06f;
            var p=PlantGrowth.Sow(Single(),c,supports);
            p.Advance(23,c,supports);
            var nodes=Enumerable.Range(0,p.OrganCount).Select(p.NodeAt).ToArray();
            Assert.IsTrue(nodes.Any(n=>n.Contact==PlantContact.Shelf));
            Assert.IsTrue(nodes.Any(n=>n.Tip.y<supports.shelfHeight-.04f));
            supports.shelf=false;
            var saved=PlantGrowth.FromJson(p.ToJson());
            saved.Advance(3,c,supports);
            foreach(var n in nodes)
            {
                var restored=Enumerable.Range(0,saved.OrganCount).Select(saved.NodeAt).Single(x=>x.Id==n.Id);
                Assert.AreEqual(n.Contact,restored.Contact);Assert.AreEqual(n.Tip,restored.Tip);
            }
        }

        [Test]
        public void PoleAttachmentAndRimContactAreRecordedAtBirth()
        {
            var c=PlantConditions.Default;
            var s=PlantSupports.Windowsill;s.pole=true;
            var p=PlantGrowth.Sow(Single(PlantSpecies.Monstera),c,s);p.Advance(9,c,s);
            var nodes=Enumerable.Range(0,p.OrganCount).Select(p.NodeAt).ToArray();
            Assert.Greater(nodes.Count(n=>n.Contact==PlantContact.Pole),3);
            Assert.Greater(nodes.Last().Tip.y,.45f);
            s.pole=false;s.rimHeight=.19f;s.rimRadius=.18f;
            var rim=PlantGrowth.Sow(Single(),c,s);rim.Advance(8,c,s);
            Assert.IsTrue(Enumerable.Range(0,rim.OrganCount).Select(rim.NodeAt).Any(n=>n.Contact==PlantContact.Rim));
        }

        [TestCase(LeafDetail.Simple),TestCase(LeafDetail.Standard),TestCase(LeafDetail.CloseUp)]
        public void MatureMonsteraContainsRealEnclosedOpenings(LeafDetail detail)
        {
            var g=PlantGrowth.Sow(Single(PlantSpecies.Monstera),PlantConditions.Default);
            var mesh=new Mesh(); var writer=new PlantMesher();
            try
            {
                writer.Write(g,mesh,detail);
                Assert.AreEqual(2,Euler(mesh),"The juvenile blade should be a closed, unperforated shell.");
                g.Advance(100,PlantConditions.Default);
                writer.Write(g,mesh,detail);
                Assert.Less(Euler(mesh),2*g.OrganCount,"Expected holes through the mature blade surfaces.");
            }
            finally {UnityEngine.Object.DestroyImmediate(mesh);}
        }

        static int Euler(Mesh m)
        {
            var verts=new HashSet<int>();var edges=new Dictionary<(int,int),int>();
            var tris=m.GetTriangles(1);
            for(int i=0;i<tris.Length;i+=3)
            for(int j=0;j<3;j++)
            {
                int a=tris[i+j],b=tris[i+(j+1)%3];verts.Add(a);
                var edge=(Math.Min(a,b),Math.Max(a,b));edges.TryGetValue(edge,out int count);edges[edge]=count+1;
            }
            Assert.IsTrue(edges.Values.All(n=>n==2),"Non-manifold foliage edge");
            return verts.Count-edges.Count+tris.Length/3;
        }

        [TestCase(true),TestCase(false)]
        public void SavedPrefabReopensWithShapeAndMaterials(bool living)
        {
            string folder="Assets/PlantPrefabTest-"+Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets",folder.Substring(7));
            var source=new GameObject().AddComponent<ProceduralPlant>();GameObject instance=null;
            try
            {
                source.Surface=PlantSurface.PBR;source.Supports=PlantSupports.Windowsill;
                source.Generate(Single(),9);source.Cut(source.Growth.NodeAt(5).Id);
                var prefab=JLink.Plants.Editor.PlantPrefabBaker.Bake(source,folder+"/Specimen.prefab",living);
                instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var plant=instance.GetComponent<ProceduralPlant>();
                Assert.AreEqual(living,plant!=null);
                CollectionAssert.AreEqual(source.CurrentMesh.vertices,instance.GetComponent<MeshFilter>().sharedMesh.vertices);
                CollectionAssert.AreEqual(source.GetComponent<MeshRenderer>().sharedMaterials,instance.GetComponent<MeshRenderer>().sharedMaterials);
                if(living)
                {
                    Assert.AreEqual(source.SaveState(),plant.SaveState());
                    Assert.AreEqual(source.Supports.shelfXZ,plant.Supports.shelfXZ);
                    plant.Advance(2);Assert.AreNotEqual(source.SaveState(),plant.SaveState());
                }
                else Assert.IsTrue(AssetDatabase.Contains(instance.GetComponent<MeshFilter>().sharedMesh));
            }
            finally
            {
                if(instance)UnityEngine.Object.DestroyImmediate(instance);
                UnityEngine.Object.DestroyImmediate(source.gameObject);
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
