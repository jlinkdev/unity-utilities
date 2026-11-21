using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace JLink.Plants.Tests
{
    public class PlantTrainingTests
    {
        [Test]
        public void SameCuttingTrailsOrClimbsAndBothResumeExactly()
        {
            var c=PlantConditions.Default;
            var pot=PlantSupports.Windowsill;
            var stock=PlantGrowth.Sow(PlantRecipe.Default,c,pot);
            stock.Advance(46,c,pot);
            var source=new PlantSpecimen {growth=stock.Cut(26,true).ToJson(),conditions=c};
            string raw=source.growth;
            var climbing=CuttingPlacement.Climbing.Pose(source,pot);
            var trailing=CuttingPlacement.Trailing.Pose(source,pot);
            Assert.AreEqual(8,climbing.OrganCount);
            Assert.AreEqual(raw,source.growth);
            Assert.AreEqual(raw,trailing.ToJson());
            int n=climbing.OrganCount;
            var pole=CuttingPlacement.Climbing.SupportsFor(pot);
            Assert.IsTrue(climbing.TryPreviewNext(c,pole,out var prediction));
            Assert.AreEqual(PlantContact.Pole,prediction.Contact);
            for(int i=0;i<n;i++)
            {
                var a=climbing.NodeAt(i);var b=trailing.NodeAt(i);
                Assert.AreEqual(a.Id,b.Id); Assert.AreEqual(a.Tip.magnitude,b.Tip.magnitude,.00001f);
            }
            climbing.Advance(6,c,pole);trailing.Advance(6,c,pot);
            Assert.AreEqual(prediction.ParentId,climbing.NodeAt(n).ParentId);
            Assert.AreEqual(prediction.Tip,climbing.NodeAt(n).Tip);
            Assert.IsTrue(Enumerable.Range(n,climbing.OrganCount-n).All(i=>climbing.NodeAt(i).Contact==PlantContact.Pole));
            Assert.IsFalse(Enumerable.Range(n,trailing.OrganCount-n).Any(i=>trailing.NodeAt(i).Contact==PlantContact.Pole));
            foreach(var g in new[]{climbing,trailing})
            {
                var environment=g==climbing ? pole : pot;
                var resumed=PlantGrowth.FromJson(g.ToJson());
                g.Advance(8,c,environment);resumed.Advance(8,c,environment);
                Assert.AreEqual(g.ToJson(),resumed.ToJson());
            }
        }

        [Test]
        public void ForecastForSelectedShootMatchesLaterBirthWithoutChangingSource()
        {
            var c=PlantConditions.Default; var s=PlantSupports.Windowsill;
            var g=PlantGrowth.Sow(PlantRecipe.Default,c,s);g.Advance(18,c,s);
            string saved=g.ToJson(); int start=g.OrganCount;
            var future=PlantGrowth.FromJson(saved); future.Advance(120,c,s);
            for(int shoot=1;shoot<=4;shoot++)
            {
                var first=Enumerable.Range(start,future.OrganCount-start).Select(future.NodeAt).First(n=>n.ShootId==shoot);
                Assert.IsTrue(g.TryPreviewVine(shoot,c,s,out var next));
                Assert.AreEqual(first.Id,next.Id);Assert.AreEqual(first.Tip,next.Tip);Assert.AreEqual(first.Contact,next.Contact);
            }
            Assert.AreEqual(saved,g.ToJson());
        }

        [Test]
        public void MatureMonsteraNodeRetainsLeafRootGeometryAndProducesANewShoot()
        {
            var r=PlantRecipe.Default;r.species=PlantSpecies.Monstera;r.crowns=1;
            var c=PlantConditions.Default;
            var g=PlantGrowth.Sow(r,c);g.Advance(4,c);
            Assert.IsFalse(Enumerable.Range(0,g.OrganCount).Select(g.NodeAt).Any(n=>g.CanCut(n.Id)));
            g.Advance(15,c);
            var tip=g.NodeAt(g.OrganCount-1);Assert.IsTrue(g.CanCut(tip.Id));
            var clipping=g.Cut(tip.Id,true);
            Assert.AreEqual(1,clipping.OrganCount);
            Assert.AreEqual(tip.Depth,clipping.NodeAt(0).Depth);
            var mesh=new Mesh();
            try
            {
                new PlantMesher().Write(clipping,mesh,LeafDetail.Standard);
                Assert.Greater(mesh.GetIndexCount(0),0u);Assert.Greater(mesh.GetIndexCount(1),0u);
            }
            finally { Object.DestroyImmediate(mesh); }
            var rooted=PlantGrowth.FromJson(clipping.ToJson());
            rooted.Advance(2,c);
            Assert.AreEqual(tip.Id,rooted.NodeAt(1).ParentId);
            int count=g.OrganCount;g.Advance(2,c);
            Assert.AreEqual(tip.ParentId,g.NodeAt(count).ParentId);
        }
    }
}
