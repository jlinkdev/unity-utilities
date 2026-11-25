using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JLink.ProceduralTextures.Tests
{
    public sealed class TextureLifecycleTests
    {
        [Test]
        public void DraftUndoRedoAndApplyUpdateTheNativeAsset()
        {
            string path = "Assets/Draft-" + Guid.NewGuid().ToString("N") + ".proctex";
            string initial = JsonUtility.ToJson(new TextureRecipe(), true);
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
                var value = draft.FindProperty("recipe.width");
                int original = value.intValue;
                int initialOutput = AssetDatabase.LoadAssetAtPath<Texture2D>(path).width;
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
                Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(path).width, Is.GreaterThan(initialOutput));
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
                string path = folder + "/Original.proctex";
                string copy = folder + "/Copy.proctex";
                var recipe = new TextureRecipe();
                File.WriteAllText(path, JsonUtility.ToJson(recipe, true));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var first = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(first, Is.Not.Null);
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(first, out string guid, out long fileId));
                Assert.That(AssetDatabase.CopyAsset(path, copy));
                recipe.width *= 2;
                File.WriteAllText(path, JsonUtility.ToJson(recipe, true));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var revised = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var duplicate = AssetDatabase.LoadAssetAtPath<Texture2D>(copy);
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(revised, out string nextGuid, out long nextId));
                Assert.That(nextGuid, Is.EqualTo(guid));
                Assert.That(nextId, Is.EqualTo(fileId));
                Assert.That(AssetDatabase.AssetPathToGUID(copy), Is.Not.EqualTo(guid));
                Assert.That(duplicate.width, Is.LessThan(revised.width));
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void PeriodicityAndFeaturesSurviveResolutionChange(bool horizontal, bool vertical)
        {
            var recipe = new TextureRecipe { seamlessX = horizontal, seamlessY = vertical };
            float sample = ValueNoiseGenerator.Sample(recipe, -0.125f, 0.375f);
            Assert.That(ValueNoiseGenerator.Sample(recipe, -0.125f + (horizontal ? 1 : 0),
                0.375f + (vertical ? 1 : 0)), Is.EqualTo(sample).Within(0.000001f));
            recipe.width *= 2;
            Assert.That(ValueNoiseGenerator.Sample(recipe, -0.125f, 0.375f), Is.EqualTo(sample));
            var a = ValueNoiseGenerator.Generate(recipe);
            var b = ValueNoiseGenerator.Generate(recipe);
            try { CollectionAssert.AreEqual(a.GetPixels32(), b.GetPixels32()); }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }
    }
}
