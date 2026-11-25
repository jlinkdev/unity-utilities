using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JLink.ProceduralTextures.Tests
{
    public sealed class GeneratorTests
    {
        static string[] Generators => TextureCatalog.All.Select(item => item.Id).ToArray();

        [TestCaseSource(nameof(Generators))]
        public void RasterAndPreviewUseTheSameField(string id)
        {
            var recipe = new TextureRecipe { generator = id, width = 64, height = 48, cellsX = 5, cellsY = 3 };
            var asset = TextureGenerator.Generate(recipe);
            var preview = TextureGenerator.GeneratePreview(recipe, 64);
            try
            {
                var pixels = asset.GetPixels32();
                Assert.That(pixels.Select(p => p.r).Distinct().Count(), Is.GreaterThan(1), "Uniform output");
                CollectionAssert.AreEqual(pixels, preview.GetPixels32());
                Assert.That(asset.width, Is.EqualTo(64));
                Assert.That(asset.height, Is.EqualTo(48));
            }
            finally { Object.DestroyImmediate(asset); Object.DestroyImmediate(preview); }
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void NoiseAndPatternsRepeatOnSelectedAxes(bool repeatU, bool repeatV)
        {
            foreach (var definition in TextureCatalog.All.Where(d => d.Menu.StartsWith("Noise/") || d.Menu.StartsWith("Patterns/")))
            {
                var r = new TextureRecipe { generator = definition.Id, cellsX = 5, cellsY = 3, seamlessX = repeatU, seamlessY = repeatV };
                var field = new TextureField(r);
                for (int i = 0; i < 7; i++)
                {
                    float u = 0.0625f + i / 16f, v = -0.09375f + i / 16f;
                    Assert.That(field.Sample(u + (repeatU ? 1 : 0), v + (repeatV ? 1 : 0)),
                        Is.EqualTo(field.Sample(u,v)).Within(0.0002f), definition.Id);
                }
            }
        }

        [Test]
        public void RingKeepsAspectAndEncodesInsideAsPositiveDistance()
        {
            var r = new TextureRecipe { generator = "ring", width = 512, height = 256, sizeX = 0.8f,
                thickness = 0.25f, signedDistance = true, distanceRange = 0.1f };
            var field = new TextureField(r);
            Assert.That(field.Sample(0.7f, 0.5f), Is.EqualTo(0.5f).Within(0.00001f));
            Assert.That(field.Sample(0.5f, 0.9f), Is.EqualTo(0.5f).Within(0.00001f));
            Assert.That(field.Sample(0.5f, 0.85f), Is.GreaterThan(0.5f));
            Assert.That(field.Sample(0.5f, 0.5f), Is.Zero);
        }

        [Test]
        public void ScalarPreviewKeepsTheProcessedFieldAndAuthoredColorOutputIndependent()
        {
            var r = new TextureRecipe { generator = "linear", width = 256, height = 1, fitTexelEndpoints = true,
                sizeX = 1, invert = true, colorMode = TextureColorMode.ColorRamp, lowColor = Color.red, highColor = Color.blue };
            var color = TextureGenerator.GeneratePreview(r, 32);
            var scalar = TextureGenerator.GeneratePreview(r, 32, true);
            try
            {
                Assert.That(r.colorMode, Is.EqualTo(TextureColorMode.ColorRamp));
                Assert.That(scalar.width, Is.EqualTo(32));
                var pixels = scalar.GetPixels32();
                for (int i = 0; i < 32; i++)
                {
                    byte level = (byte)Mathf.RoundToInt(255 * (1 - i / 31f));
                    Assert.That(pixels[i], Is.EqualTo(new Color32(level, level, level, 255)));
                }
                Assert.That(color.GetPixel(0,0), Is.EqualTo(Color.blue));
            }
            finally { Object.DestroyImmediate(color); Object.DestroyImmediate(scalar); }
        }

        [Test]
        public void LegacyRampStillBakesTheOriginalTwoColors()
        {
            string path = "Assets/LegacyRamp-" + Guid.NewGuid().ToString("N") + ".proctex";
            const string json = "{\"schemaVersion\":2,\"generatorVersion\":1,\"generator\":\"linear\",\"width\":8,\"height\":8,\"sizeX\":1,\"colorMode\":2," +
                "\"lowColor\":{\"r\":0.2,\"g\":0.4,\"b\":0.7,\"a\":0.15},\"highColor\":{\"r\":0.9,\"g\":0.1,\"b\":0.3,\"a\":0.85}}";
            try
            {
                File.WriteAllText(path, json);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var pixels = AssetDatabase.LoadAssetAtPath<Texture2D>(path).GetPixels32();
                for (int i = 0; i < 8; i++)
                {
                    Color expected = Color.Lerp(new Color(0.2f,0.4f,0.7f,0.15f), new Color(0.9f,0.1f,0.3f,0.85f), (i + 0.5f) / 8);
                    Assert.That(pixels[i], Is.EqualTo((Color32)expected));
                }
                Assert.That(File.ReadAllText(path), Is.EqualTo(json));
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        [TestCase(GradientMode.Blend)]
        [TestCase(GradientMode.Fixed)]
        [TestCase(GradientMode.PerceptualBlend)]
        public void InspectorLutPreservesEveryStopThroughApplyAndReimport(GradientMode mode)
        {
            string path = "Assets/Ramp-" + Guid.NewGuid().ToString("N") + ".proctex";
            AssetImporterEditor editor = null;
            try
            {
                var recipe = new TextureRecipe { generator = "linear", width = 256, height = 64 };
                File.WriteAllText(path, JsonUtility.ToJson(recipe));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                editor = (AssetImporterEditor)UnityEditor.Editor.CreateEditor(AssetImporter.GetAtPath(path));
                var target = typeof(AssetImporterEditor).GetProperty("extraDataTarget",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                using var draft = new SerializedObject((Object)target.GetValue(editor));
                var fields = draft.FindProperty("recipe");
                fields.FindPropertyRelative("height").intValue = 1;
                fields.FindPropertyRelative("fitTexelEndpoints").boolValue = true;
                fields.FindPropertyRelative("sizeX").floatValue = 1f;
                fields.FindPropertyRelative("colorMode").enumValueIndex = (int)TextureColorMode.ColorRamp;
                var ramp = new Gradient { mode = mode };
                ramp.SetKeys(new[] { new GradientColorKey(Color.blue, 0), new GradientColorKey(Color.red, 0.3f),
                    new GradientColorKey(Color.green, 0.75f), new GradientColorKey(Color.white, 1) },
                    new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.17f), new GradientAlphaKey(0.5f, 1) });
                fields.FindPropertyRelative("colorRamp").gradientValue = ramp;
                draft.ApplyModifiedProperties();
                var select = editor.GetType().GetMethod("SelectGenerator", BindingFlags.Instance | BindingFlags.NonPublic);
                select.Invoke(editor, new object[] { "ring" });
                select.Invoke(editor, new object[] { "linear" });
                editor.SaveChanges();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture.height, Is.EqualTo(1));
                Assert.That(texture.width, Is.EqualTo(256));
                var pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                    Assert.That(pixels[i], Is.EqualTo((Color32)ramp.Evaluate(i / 255f)), $"texel {i}");
                Assert.That(File.ReadAllText(path), Does.Contain("\"alphas\""));
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void RecipeActionsEditAnUndoableDraftAndKeepItsSourceUntouched()
        {
            string path = "Assets/RecipeActions-" + Guid.NewGuid().ToString("N") + ".proctex";
            string template = path + ".template~";
            string original = JsonUtility.ToJson(new TextureRecipe());
            var reusable = new TextureRecipe { generator = "star", points = 9, width = 88, domainWarp = true };
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
                Assert.That(draft.FindProperty("recipe.points").intValue, Is.EqualTo(new TextureRecipe().points));
                Assert.That(draft.FindProperty("recipe.domainWarp").boolValue, Is.True);
                Undo.PerformUndo();
                draft.Update();
                Assert.That(draft.FindProperty("recipe.points").intValue, Is.EqualTo(reusable.points));
                reset.Invoke(editor, new object[] { false });
                draft.Update();
                Assert.That(draft.FindProperty("recipe.domainWarp").boolValue, Is.False);
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
            string path = directory + "/Source.proctex";
            const string original = "{\"schemaVersion\":1,\"generator\":\"value-noise\",\"generatorVersion\":1,\"width\":60,\"height\":48,\"seed\":73,\"cellsX\":5,\"cellsY\":7,\"seamlessX\":true,\"seamlessY\":false}";
            AssetImporterEditor editor = null;
            try
            {
                File.WriteAllText(path, original);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var native = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(native, out string guid, out long fileId));
                var consumer = new Material(Shader.Find("Unlit/Texture")) { mainTexture = native };
                string consumerPath = directory + "/Saved.mat";
                AssetDatabase.CreateAsset(consumer, consumerPath);
                editor = (AssetImporterEditor)UnityEditor.Editor.CreateEditor(AssetImporter.GetAtPath(path));
                var select = editor.GetType().GetMethod("SelectGenerator", BindingFlags.Instance | BindingFlags.NonPublic);
                var data = typeof(AssetImporterEditor).GetProperty("extraDataTarget", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                using var draft = new SerializedObject((Object)data.GetValue(editor));
                select.Invoke(editor, new object[] { "ring" });
                draft.Update();
                draft.FindProperty("recipe.sizeX").floatValue = 0.83f;
                draft.FindProperty("recipe.domainWarp").boolValue = true;
                draft.FindProperty("recipe.warpSeed").intValue = 123;
                draft.FindProperty("recipe.blur").enumValueIndex = (int)BlurMode.Directional;
                draft.FindProperty("recipe.blurAngle").floatValue = 52f;
                draft.FindProperty("recipe.detectEdges").boolValue = true;
                draft.ApplyModifiedProperties();
                select.Invoke(editor, new object[] { "value-noise" });
                select.Invoke(editor, new object[] { "ring" });
                draft.Update();
                Assert.That(draft.FindProperty("recipe.sizeX").floatValue, Is.EqualTo(0.83f));
                Assert.That(File.ReadAllText(path), Is.EqualTo(original));
                editor.SaveChanges();
                string saved = File.ReadAllText(path);
                Assert.That(saved, Does.Contain("\"ring\""));
                Assert.That(saved, Does.Contain("\"schemaVersion\": 4"));
                Assert.That(saved, Does.Contain("\"warpSeed\": 123"));
                Assert.That(saved, Does.Contain("\"blurAngle\": 52"));
                Assert.That(saved, Does.Contain("\"detectEdges\": true"));
                Assert.That(saved, Does.Not.Contain("\"seed\""));
                var applied = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var expected = TextureGenerator.Generate(new TextureRecipe { generator = "ring", width = 60, height = 48,
                    sizeX = 0.83f, domainWarp = true, warpSeed = 123, blur = BlurMode.Directional, blurAngle = 52, detectEdges = true });
                try { CollectionAssert.AreEqual(expected.GetPixels32(), applied.GetPixels32()); }
                finally { Object.DestroyImmediate(expected); }
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(applied, out string newGuid, out long newId));
                Assert.That(newGuid, Is.EqualTo(guid));
                Assert.That(newId, Is.EqualTo(fileId));
                AssetDatabase.ImportAsset(consumerPath, ImportAssetOptions.ForceUpdate);
                Assert.That(AssetDatabase.LoadAssetAtPath<Material>(consumerPath).mainTexture, Is.EqualTo(applied));
                Assert.That(applied.width, Is.EqualTo(60));
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                AssetDatabase.DeleteAsset(directory);
            }
        }
    }
}
