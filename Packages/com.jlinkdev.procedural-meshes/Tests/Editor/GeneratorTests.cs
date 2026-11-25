using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JLink.ProceduralMeshes.Tests
{
    public sealed class GeneratorTests
    {
        static string[] Generators => MeshCatalog.All.Select(item => item.Id).ToArray();

        [TestCaseSource(nameof(Generators))]
        public void GeometryHasValidSurfaceAndVertexData(string id)
        {
            var r = new MeshRecipe { generator = id, heightInColorR = true, capEnds = true };
            var mesh = MeshGenerator.Generate(r);
            try
            {
                var points = mesh.vertices;
                var uv = mesh.uv;
                var normals = mesh.normals;
                var tangents = mesh.tangents;
                var tris = mesh.triangles;
                Assert.That(uv.Length, Is.EqualTo(points.Length));
                Assert.That(tangents.Length, Is.EqualTo(points.Length));
                Assert.That(mesh.colors32.Length, Is.EqualTo(points.Length));
                Assert.That(tris.Length, Is.Positive);
                for (int i = 0; i < tris.Length; i += 3)
                {
                    int a = tris[i], b = tris[i+1], c = tris[i+2];
                    Vector3 face = Vector3.Cross(points[b] - points[a], points[c] - points[a]);
                    Assert.That(Vector3.Dot(face, normals[a] + normals[b] + normals[c]), Is.Positive);
                    Assert.That(normals[a].magnitude, Is.EqualTo(1f).Within(0.0001f));
                    Assert.That(float.IsNaN(tangents[a].x) || float.IsInfinity(uv[a].x), Is.False);
                    if (id == "icosphere")
                        Assert.That(Mathf.Max(uv[a].x, Mathf.Max(uv[b].x, uv[c].x)) -
                            Mathf.Min(uv[a].x, Mathf.Min(uv[b].x, uv[c].x)), Is.LessThanOrEqualTo(0.501f));
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void CutFrustumClosesBothFacesAndPreservesMaterialRegions()
        {
            var r = new MeshRecipe { sweep = 90, capEnds = true, separateEnds = true, closeCut = true,
                sides = 8, heightSegments = 2, bottomRadius = 1, topRadius = 0.5f, sideUV = SideMapping.WorldUnits };
            var mesh = MeshGenerator.Generate(r);
            try
            {
                Assert.That(mesh.subMeshCount, Is.EqualTo(2));
                Assert.That(mesh.GetIndexCount(0) / 3, Is.EqualTo(32));
                Assert.That(mesh.GetIndexCount(1) / 3, Is.EqualTo(24));
                Assert.That(mesh.uv[8].x, Is.EqualTo(Mathf.PI / 2).Within(0.00001f));
                foreach (int part in new[] { 0, 1 })
                {
                    var indices = mesh.GetTriangles(part);
                    var points = mesh.vertices;
                    var normals = mesh.normals;
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        int a = indices[i], b = indices[i+1], c = indices[i+2];
                        Assert.That(Vector3.Dot(Vector3.Cross(points[b]-points[a], points[c]-points[a]), normals[a]), Is.Positive);
                    }
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void PartialAnnulusAndSubdividedSphereUseRequestedTopology()
        {
            var arc = MeshGenerator.Generate(new MeshRecipe { generator = "annulus", sweep = 90, sides = 8, bands = 2, discUV = DiscMapping.Strip });
            var sphere = MeshGenerator.Generate(new MeshRecipe { generator = "icosphere", subdivisions = 3 });
            try
            {
                Assert.That(arc.triangles.Length / 3, Is.EqualTo(32));
                Assert.That(arc.uv[8], Is.EqualTo(new Vector2(1,0)));
                Assert.That(arc.bounds.min.x, Is.GreaterThan(-0.0001f));
                Assert.That(sphere.triangles.Length / 3, Is.EqualTo(20 * 64));
            }
            finally { Object.DestroyImmediate(arc); Object.DestroyImmediate(sphere); }
        }

        [Test]
        public void RecipeActionsEditAnUndoableDraftAndKeepItsSourceUntouched()
        {
            string path = "Assets/RecipeActions-" + Guid.NewGuid().ToString("N") + ".procmesh";
            string template = path + ".template~";
            string original = JsonUtility.ToJson(new MeshRecipe());
            var reusable = new MeshRecipe { generator = "torus", sides = 47, heightInColorR = true };
            AssetImporterEditor editor = null;
            try
            {
                File.WriteAllText(path, original);
                File.WriteAllText(template, JsonUtility.ToJson(reusable));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                editor = (AssetImporterEditor)UnityEditor.Editor.CreateEditor(AssetImporter.GetAtPath(path));
                const BindingFlags methods = BindingFlags.Instance | BindingFlags.NonPublic;
                editor.GetType().GetMethod("UseSettings", methods).Invoke(editor, new object[] { Path.GetFullPath(template) });
                var target = typeof(AssetImporterEditor).GetProperty("extraDataTarget", methods | BindingFlags.Public);
                using var draft = new SerializedObject((Object)target.GetValue(editor));
                var reset = editor.GetType().GetMethod("ResetSettings", methods);
                Assert.That(draft.FindProperty("recipe.generator").stringValue, Is.EqualTo(reusable.generator));
                Undo.IncrementCurrentGroup();
                reset.Invoke(editor, new object[] { true });
                Undo.FlushUndoRecordObjects();
                draft.Update();
                Assert.That(draft.FindProperty("recipe.sides").intValue, Is.EqualTo(new MeshRecipe().sides));
                Assert.That(draft.FindProperty("recipe.heightInColorR").boolValue, Is.True);
                Undo.PerformUndo();
                draft.Update();
                Assert.That(draft.FindProperty("recipe.sides").intValue, Is.EqualTo(reusable.sides));
                reset.Invoke(editor, new object[] { false });
                draft.Update();
                Assert.That(draft.FindProperty("recipe.heightInColorR").boolValue, Is.False);
                Assert.That(draft.FindProperty("recipe.generator").stringValue, Is.EqualTo(reusable.generator));
                Assert.That(File.ReadAllText(path), Is.EqualTo(original));
                editor.DiscardChanges();
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                AssetDatabase.DeleteAsset(path);
                File.Delete(template);
            }
        }

        [Test]
        public void InspectorSwitchPreservesLegacyConsumerAndWritesOnlyActiveFields()
        {
            string directory = "Assets/GeneratorSwitch-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(directory);
            string path = directory + "/Source.procmesh";
            const string original = "{\"schemaVersion\":1,\"generator\":\"cylinder\",\"generatorVersion\":1,\"bottomRadius\":0.7,\"topRadius\":0.3,\"height\":2,\"sides\":12,\"heightSegments\":3,\"seamDegrees\":20,\"heightInColorR\":true}";
            AssetImporterEditor editor = null;
            try
            {
                File.WriteAllText(path, original);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var native = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(native, out string guid, out long fileId));
                string consumerPath = directory + "/Saved.prefab";
                var go = new GameObject("Consumer", typeof(MeshFilter));
                try
                {
                    go.GetComponent<MeshFilter>().sharedMesh = native;
                    PrefabUtility.SaveAsPrefabAsset(go, consumerPath);
                }
                finally { Object.DestroyImmediate(go); }
                editor = (AssetImporterEditor)UnityEditor.Editor.CreateEditor(AssetImporter.GetAtPath(path));
                var select = editor.GetType().GetMethod("SelectGenerator", BindingFlags.Instance | BindingFlags.NonPublic);
                var data = typeof(AssetImporterEditor).GetProperty("extraDataTarget", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                using var draft = new SerializedObject((Object)data.GetValue(editor));
                select.Invoke(editor, new object[] { "torus" });
                draft.Update();
                draft.FindProperty("recipe.radius").floatValue = 0.83f;
                draft.ApplyModifiedProperties();
                select.Invoke(editor, new object[] { "cylinder" });
                select.Invoke(editor, new object[] { "torus" });
                draft.Update();
                Assert.That(draft.FindProperty("recipe.radius").floatValue, Is.EqualTo(0.83f));
                Assert.That(File.ReadAllText(path), Is.EqualTo(original));
                editor.SaveChanges();
                string saved = File.ReadAllText(path);
                Assert.That(saved, Does.Contain("\"torus\""));
                Assert.That(saved, Does.Not.Contain("\"bottomRadius\""));
                var applied = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(applied, out string newGuid, out long newId));
                Assert.That(newGuid, Is.EqualTo(guid));
                Assert.That(newId, Is.EqualTo(fileId));
                AssetDatabase.ImportAsset(consumerPath, ImportAssetOptions.ForceUpdate);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(consumerPath).GetComponent<MeshFilter>().sharedMesh, Is.EqualTo(applied));
                Assert.That(applied.colors32.Length, Is.EqualTo(applied.vertexCount));
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                AssetDatabase.DeleteAsset(directory);
            }
        }
    }
}
