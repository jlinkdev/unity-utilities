using System.Collections.Generic;
using System.IO;
using System.Linq;
using JLink.Pots.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JLink.Pots.Tests
{
    public class PotUvTests
    {
        readonly PotMesh builder = new PotMesh();
        Mesh mesh;
        [SetUp] public void SetUp() => mesh = new Mesh();
        [TearDown] public void TearDown() => Object.DestroyImmediate(mesh);

        [Test]
        public void CylinderHasMetricTilesAndSmoothClosingSeam()
        {
            var p = PotRecipe.Create(PotForm.Cylinder);
            p.rings = new List<ProfileRing> { new ProfileRing(0,1),new ProfileRing(1,1) };
            p.width = .32f; p.height = .4f; p.tileSize = .1f;
            builder.Build(p,mesh);
            var uv = mesh.uv; var vertices = mesh.vertices; var normals = mesh.normals; var tangents = mesh.tangents;
            int seam = p.segments;
            Assert.AreEqual(vertices[0],vertices[seam]);
            Assert.Greater(vertices[0].z,0,"Seam at back of pot");
            Assert.AreEqual(normals[0],normals[seam]);
            Assert.AreEqual(tangents[0],tangents[seam]);
            Assert.That(uv[seam].x-uv[0].x,Is.EqualTo(10).Within(.0001f));
            Assert.That(uv[seam+1].y-uv[0].y,Is.EqualTo((vertices[seam+1].y-vertices[0].y)/p.tileSize).Within(.0001f));
            for (int i = 0; i < uv.Length; i++)
            {
                Assert.That(((Vector3)tangents[i]).magnitude,Is.EqualTo(1).Within(.002f));
                Assert.That(Vector3.Dot(normals[i],(Vector3)tangents[i]),Is.EqualTo(0).Within(.002f));
                Assert.AreEqual(1,Mathf.Abs(tangents[i].w));
            }
        }

        [Test]
        public void ReferenceSurvivesSculptSaveReloadAndExplicitRefit()
        {
            var p = PotRecipe.Create(PotForm.Urn);
            p.saucer = true;
            builder.Build(p,mesh);
            var first = mesh.uv;
            p.width *= 1.7f;
            p.handleSpan = .55f;
            p.wheelOffsets = new[] { 0f,.2f,0f };
            p.marks.Add(new ThumbMark { level = .5f, degrees = 30, size = .2f, depth = -.18f });
            builder.Build(p,mesh);
            CollectionAssert.AreEqual(first,mesh.uv);
            var saved = p.Copy();
            new PotMesh().Build(saved,mesh);
            CollectionAssert.AreEqual(first,mesh.uv);
            saved.RefitUVs();
            builder.Build(saved,mesh);
            var fitted = mesh.uv;
            Assert.Greater(fitted[p.segments].x,first[p.segments].x);
            saved.tileSize *= .5f;
            builder.Build(saved,mesh);
            Assert.Greater(mesh.uv[p.segments].x,fitted[p.segments].x);
        }

        [TestCase(true)] [TestCase(false)]
        public void AtlasContainsSeparatedIslandsAcrossAllMaterialSlots(bool combine)
        {
            var p = PotRecipe.Create(PotForm.Trough);
            p.baseStyle = BaseStyle.FootRing; p.handles = 6; p.saucer = true;
            p.fluteDepth = .14f; p.twist = .48f;
            p.marks.Add(new ThumbMark { level = .05f, degrees = 31, size = .2f, depth = -.4f });
            builder.Build(p,mesh);
            p.depthRatio *= 1.6f;
            builder.Build(p,mesh);
            var shape = mesh.vertices;
            builder.Build(p,mesh,PotUvMode.Paintable,combine);
            CollectionAssert.AreEqual(shape,mesh.vertices);
            Assert.AreEqual(combine ? 1 : 4,mesh.subMeshCount);
            var uv = mesh.uv; var indices = mesh.triangles;
            var parent = Enumerable.Range(0,uv.Length).ToArray();
            int Find(int i)
            {
                while (i != parent[i]) { parent[i] = parent[parent[i]]; i = parent[i]; }
                return i;
            }
            for (int i = 0; i < indices.Length; i+=3)
            {
                int a = indices[i], b = indices[i+1], c = indices[i+2];
                parent[Find(b)] = Find(a); parent[Find(c)] = Find(a);
                Vector2 ab = uv[b]-uv[a], ac = uv[c]-uv[a];
                Assert.Greater(Mathf.Abs(ab.x*ac.y-ab.y*ac.x),0);
            }
            var bounds = new Dictionary<int,Bounds>();
            for (int i = 0; i < uv.Length; i++)
            {
                Assert.That(uv[i].x,Is.InRange(.0038f,.9962f));
                Assert.That(uv[i].y,Is.InRange(.0038f,.9962f));
                int key = Find(i);
                if (!bounds.TryGetValue(key,out var box)) box = new Bounds(uv[i],Vector3.zero);
                box.Encapsulate(uv[i]); bounds[key] = box;
            }
            Assert.AreEqual(27,bounds.Count,"Body, inner wall, foot, two faces, drain, 18 handle islands and three saucer islands");
            var boxes = bounds.Values.ToArray();
            var orientation = new Dictionary<int,float>();
            for (int i = 0; i < indices.Length; i += 3)
            {
                int a = indices[i], key = Find(a);
                Vector2 ab = uv[indices[i+1]]-uv[a], ac = uv[indices[i+2]]-uv[a];
                float sign = Mathf.Sign(ab.x*ac.y-ab.y*ac.x);
                if (orientation.TryGetValue(key,out float previous)) Assert.AreEqual(previous,sign,"Folded UV triangle");
                else orientation.Add(key,sign);
            }
            for (int i = 0; i < boxes.Length; i++)
                for (int j = 0; j < i; j++)
                {
                    var a = boxes[i]; var b = boxes[j];
                    float gap = Mathf.Max(a.min.x-b.max.x,b.min.x-a.max.x,a.min.y-b.max.y,b.min.y-a.max.y);
                    Assert.Greater(gap,.0077f);
                }
        }

        [Test]
        public void PainterObjImportsWithPackedUvsAndRequestedMaterials()
        {
            string folder = "Assets/PotObjTest"+System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets",Path.GetFileName(folder));
            try
            {
                var p = PotRecipe.Create(PotForm.Urn); p.saucer = true;
                foreach (bool single in new[] { true,false })
                {
                    builder.Build(p,mesh,PotUvMode.Paintable,single);
                    string path = folder+"/pot"+single+".obj";
                    PotObjExporter.Write(mesh,path);
                    var lines = File.ReadAllLines(path);
                    Assert.AreEqual(mesh.vertexCount,lines.Count(l => l.StartsWith("vt ")));
                    Assert.AreEqual(single ? 1 : 4,lines.Count(l => l.StartsWith("usemtl ")));
                    Assert.IsTrue(File.Exists(folder+"/"+lines[0].Substring(7)));
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var imported = model.GetComponentInChildren<MeshFilter>().sharedMesh;
                    Assert.AreEqual(single ? 1 : 4,imported.subMeshCount);
                    Assert.AreEqual(imported.vertexCount,imported.uv.Length);
                    Assert.AreEqual(mesh.triangles.Length,imported.triangles.Length);
                }
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }
    }
}
