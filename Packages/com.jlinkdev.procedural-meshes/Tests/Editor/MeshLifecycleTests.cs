using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JLink.ProceduralMeshes.Tests
{
    public sealed class MeshLifecycleTests
    {
        [Test]
        public void DraftUndoRedoAndApplyUpdateTheNativeAsset()
        {
            string path = "Assets/Draft-" + Guid.NewGuid().ToString("N") + ".procmesh";
            string initial = JsonUtility.ToJson(new MeshRecipe(), true);
            var editor = default(UnityEditor.AssetImporters.AssetImporterEditor);
            try
            {
                File.WriteAllText(path, initial);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                editor = (UnityEditor.AssetImporters.AssetImporterEditor)UnityEditor.Editor.CreateEditor(AssetImporter.GetAtPath(path));
                // Unity stores the editable source-file data on its protected extra-data target.
                var property = typeof(UnityEditor.AssetImporters.AssetImporterEditor).GetProperty("extraDataTarget",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                using var draft = new SerializedObject((Object)property.GetValue(editor));
                var value = draft.FindProperty("recipe.heightSegments");
                int original = value.intValue;
                int initialOutput = AssetDatabase.LoadAssetAtPath<Mesh>(path).vertexCount;
                Undo.IncrementCurrentGroup();
                value.intValue = original * 2;
                draft.ApplyModifiedProperties();
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                draft.Update();
                Assert.That(value.intValue, Is.EqualTo(original));
                Undo.PerformRedo();
                draft.Update();
                Assert.That(value.intValue, Is.EqualTo(original * 2));
                Assert.That(File.ReadAllText(path), Is.EqualTo(initial));
                editor.DiscardChanges();
                draft.Update();
                Assert.That(value.intValue, Is.EqualTo(original));
                value.intValue = original * 2;
                draft.ApplyModifiedProperties();
                editor.SaveChanges();
                Assert.That(AssetDatabase.LoadAssetAtPath<Mesh>(path).vertexCount, Is.GreaterThan(initialOutput));
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void ReimportPreservesIdentityAndDuplicateIsIndependent()
        {
            string folder = "Assets/RecipeTest-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(folder);
            try
            {
                string path = folder + "/Original.procmesh";
                string copy = folder + "/Copy.procmesh";
                var recipe = new MeshRecipe();
                File.WriteAllText(path, JsonUtility.ToJson(recipe, true));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var first = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                Assert.That(first, Is.Not.Null);
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(first, out string guid, out long fileId));
                Assert.That(AssetDatabase.CopyAsset(path, copy));
                recipe.heightSegments *= 2;
                File.WriteAllText(path, JsonUtility.ToJson(recipe, true));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var revised = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                var duplicate = AssetDatabase.LoadAssetAtPath<Mesh>(copy);
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(revised, out string nextGuid, out long nextId));
                Assert.That(nextGuid, Is.EqualTo(guid));
                Assert.That(nextId, Is.EqualTo(fileId));
                Assert.That(AssetDatabase.AssetPathToGUID(copy), Is.Not.EqualTo(guid));
                Assert.That(duplicate.vertexCount, Is.LessThan(revised.vertexCount));
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }

        [TestCase(0f, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(1f, 0.5f)]
        public void SidewallsHaveOutwardFacesAndAnExactUvSeam(float bottom, float top)
        {
            var recipe = new MeshRecipe { bottomRadius = bottom, topRadius = top, seamDegrees = 37f, heightInColorR = true };
            var mesh = CylinderGenerator.Generate(recipe);
            try
            {
                var triangles = mesh.triangles;
                var positions = mesh.vertices;
                var normals = mesh.normals;
                var uv = mesh.uv;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    Vector3 face = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
                    Assert.That(Vector3.Dot(face, normals[a]), Is.GreaterThan(0f));
                }
                for (int row = 0; row <= recipe.heightSegments; row++)
                {
                    int a = row * (recipe.sides + 1);
                    int b = a + recipe.sides;
                    Assert.That(positions[b], Is.EqualTo(positions[a]));
                    Assert.That(normals[b], Is.EqualTo(normals[a]));
                    Assert.That(uv[b].x - uv[a].x, Is.EqualTo(1f));
                }
                Assert.That(mesh.colors32[0].r, Is.Zero);
                Assert.That(mesh.colors32[mesh.vertexCount - 1].r, Is.EqualTo(255));
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
