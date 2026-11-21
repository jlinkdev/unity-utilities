using System.Collections.Generic;
using JLink.Pots.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JLink.Pots.Tests
{
    public class PotMeshTests
    {
        Mesh mesh;
        readonly PotMesh generator = new PotMesh();
        [SetUp] public void Create() => mesh = new Mesh();
        [TearDown] public void Dispose() => Object.DestroyImmediate(mesh);

        [TestCase(PotForm.Nursery)] [TestCase(PotForm.Bowl)] [TestCase(PotForm.Cylinder)]
        [TestCase(PotForm.Trough)] [TestCase(PotForm.Urn)] [TestCase(PotForm.Bonsai)]
        public void VesselIsClosedAfterWeldingShadingEdges(PotForm form)
        {
            var p = PotRecipe.Create(form);
            p.saucer = true;
            p.fluteDepth = .1f; p.twist = .5f;
            p.wheelOffsets = new[] { 0f, 0f, .24f, 0f, -.15f, 0f };
            foreach (bool hole in new[] { false,true })
            {
                p.drainage = hole;
                generator.Build(p,mesh);
                var vertices = mesh.vertices;
                var indices = mesh.triangles;
                var ids = new Dictionary<Vector3,int>();
                var edges = new Dictionary<(int,int),(int uses,int balance)>();
                int Id(Vector3 point)
                {
                    if (!ids.TryGetValue(point,out int id)) ids.Add(point,id = ids.Count);
                    return id;
                }
                void Edge(int a,int b)
                {
                    var key = (Mathf.Min(a,b),Mathf.Max(a,b));
                    edges.TryGetValue(key,out var data);
                    edges[key] = (data.uses+1,data.balance+(a < b ? 1 : -1));
                }
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var a = vertices[indices[i]]; var b = vertices[indices[i+1]]; var c = vertices[indices[i+2]];
                    Assert.Greater(Vector3.Cross(b-a,c-a).sqrMagnitude,0);
                    Edge(Id(a),Id(b)); Edge(Id(b),Id(c)); Edge(Id(c),Id(a));
                }
                foreach (var edge in edges.Values) Assert.AreEqual((2,0),edge);
                Assert.Less(mesh.bounds.min.y,0,"The saucer sits below the base.");
                Assert.Greater(mesh.GetIndexCount(3),0);
                Assert.That(mesh.GetIndexCount(2) > 0, Is.EqualTo(p.handles > 0));
            }
        }

        [Test]
        public void ThumbSculptMovesOnlyThePickedPatch()
        {
            var p = PotRecipe.Create(PotForm.Nursery);
            var front = PotMesh.Surface(p,.5f,0);
            var back = PotMesh.Surface(p,.5f,Mathf.PI);
            p.marks.Add(new ThumbMark { level = .5f, degrees = 0, size = .2f, depth = -.25f });
            Assert.Less(PotMesh.Surface(p,.5f,0).x,front.x);
            Assert.AreEqual(back,PotMesh.Surface(p,.5f,Mathf.PI));
        }

        [Test]
        public void RegeneratingAnEditedRecipeMovesTheHandleRoots()
        {
            var p = PotRecipe.Create(PotForm.Urn);
            generator.Build(p,mesh);
            int first = mesh.GetTriangles(2)[0];
            Vector3 old = mesh.vertices[first];
            p.width *= 1.5f;
            generator.Build(p,mesh);
            Assert.That(mesh.vertices[first].x, Is.GreaterThan(old.x*1.4f));
            var before = mesh.vertices;
            generator.Build(p.Copy(),mesh);
            CollectionAssert.AreEqual(before,mesh.vertices);
        }

        [Test]
        public void BakedAssetsSurviveSourceChanges()
        {
            var obj = new GameObject();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Pot Test Mesh.asset");
            string designPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Pot Test Design.asset");
            try
            {
                var pot = obj.AddComponent<ProceduralPot>();
                pot.Apply(PotRecipe.Create(PotForm.Urn));
                PotWorkshopEditor.SaveCopy(pot,true,path);
                PotWorkshopEditor.SaveCopy(pot,false,designPath);
                var bounds = pot.GeneratedMesh.bounds;
                pot.Apply(PotRecipe.Create(PotForm.Bonsai));
                Object.DestroyImmediate(obj);
                Assert.AreEqual(bounds,AssetDatabase.LoadAssetAtPath<Mesh>(path).bounds);
                Assert.AreEqual("Urn",AssetDatabase.LoadAssetAtPath<PotDesign>(designPath).recipe.title);
            }
            finally
            {
                if (obj) Object.DestroyImmediate(obj);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.DeleteAsset(designPath);
            }
        }
    }
}
