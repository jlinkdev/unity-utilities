using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JLink.Plants.Tests
{
    public sealed class PlantLifecycleTests
    {
        [UnityTest]
        public IEnumerator LivingAndFrozenCuttingsOwnTheirGeometry()
        {
            var root=new GameObject("Cutting bench");
            Mesh frozenMesh=null;
            try
            {
                var source=root.AddComponent<ProceduralPlant>();
                var recipe=PlantRecipe.Default; recipe.crowns=1;
                source.Generate(recipe,12);
                var cutting=source.Cut(source.Growth.NodeAt(5).Id);
                string parent=source.SaveState();
                var living=PlantInstances.Living(source,root.transform,cutting);
                Assert.AreEqual(cutting.ToJson(),living.SaveState());
                Assert.AreNotSame(cutting,living.Growth);
                var frozen=PlantInstances.Frozen(living,root.transform);
                frozenMesh=frozen.GetComponent<MeshFilter>().sharedMesh;
                var vertices=frozenMesh.vertices;
                Assert.IsNull(frozen.GetComponent<ProceduralPlant>());
                Assert.AreNotSame(frozenMesh,living.CurrentMesh);
                living.Advance(4);
                CollectionAssert.AreEqual(vertices,frozenMesh.vertices);
                Assert.AreEqual(parent,source.SaveState());
            }
            finally { Object.Destroy(root); }
            yield return null;
            yield return null;
            Assert.IsTrue(frozenMesh==null);
        }

        [UnityTest]
        public IEnumerator DisableResumeAndLoadWithoutSharingMeshes()
        {
            var a = new GameObject("First plant");
            var b = new GameObject("Second plant");
            try
            {
                var first = a.AddComponent<ProceduralPlant>();
                var second = b.AddComponent<ProceduralPlant>();
                first.Advance(1.75);
                string checkpoint = first.SaveState();
                second.LoadState(checkpoint);
                Assert.That(second.CurrentMesh,Is.Not.SameAs(first.CurrentMesh));
                Assert.That(first.GetComponent<MeshRenderer>().sharedMaterial,Is.Not.Null);
                CollectionAssert.AreEqual(first.CurrentMesh.vertices,second.CurrentMesh.vertices);
                Mesh previous = first.CurrentMesh;
                a.SetActive(false);
                first.Advance(2);
                yield return null;
                Assert.That(previous == null);
                a.SetActive(true);
                second.Advance(2);
                Assert.That(first.SaveState(),Is.EqualTo(second.SaveState()));
                Assert.That(first.CurrentMesh.vertexCount,Is.GreaterThan(0));
                Assert.Throws<System.ArgumentException>(() => first.LoadState("{}"));
                Assert.That(first.SaveState(),Is.EqualTo(second.SaveState()));
            }
            finally { Object.Destroy(a); Object.Destroy(b); }
        }
    }
}
