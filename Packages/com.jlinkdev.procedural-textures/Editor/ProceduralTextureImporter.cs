using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace JLink.ProceduralTextures.Editor
{
    [ScriptedImporter(TextureRecipe.CurrentSchema, "proctex")]
    public sealed class ProceduralTextureImporter : ScriptedImporter
    {
        [Serializable]
        sealed class RecipeHeader
        {
            public int schemaVersion = 0;
            public string generator = "";
            public int generatorVersion = 0;
        }

        [Serializable]
        sealed class RampStops
        {
            [Serializable]
            public struct ColorStop
            {
                public float time;
                public Color color;
            }

            [Serializable]
            public struct AlphaStop
            {
                public float time;
                public float alpha;
            }

            public GradientMode mode;
            public ColorSpace colorSpace;
            public ColorStop[] colors;
            public AlphaStop[] alphas;

            public static JObject Encode(Gradient ramp) => JObject.Parse(JsonUtility.ToJson(new RampStops
            {
                mode = ramp.mode,
                colorSpace = ramp.colorSpace,
                colors = ramp.colorKeys.Select(k => new ColorStop { time = k.time, color = k.color }).ToArray(),
                alphas = ramp.alphaKeys.Select(k => new AlphaStop { time = k.time, alpha = k.alpha }).ToArray()
            }));

            public Gradient Decode()
            {
                if (colors == null || colors.Length < 2 || alphas == null || alphas.Length < 2)
                    throw new InvalidDataException("A color ramp needs at least two color and alpha keys.");
                var ramp = new Gradient { mode = mode, colorSpace = colorSpace };
                ramp.SetKeys(colors.Select(k => new GradientColorKey(k.color, k.time)).ToArray(),
                    alphas.Select(k => new GradientAlphaKey(k.alpha, k.time)).ToArray());
                return ramp;
            }
        }

        internal static TextureRecipe Read(string path)
        {
            string json = File.ReadAllText(Path.IsPathRooted(path) ? path : FileUtil.GetPhysicalPath(path));
            var header = JsonUtility.FromJson<RecipeHeader>(json);
            if (header == null || string.IsNullOrEmpty(header.generator) || header.generatorVersion != 1 ||
                !TextureRecipe.SupportsSchema(header.schemaVersion, header.generator))
                throw new InvalidDataException("Unsupported or missing texture recipe header.");
            // New fields absent from older sources take the current typed defaults.
            var recipe = new TextureRecipe();
            var source = JObject.Parse(json);
            var stops = source["colorRamp"] as JObject;
            if (stops != null && (stops.ContainsKey("mode") || stops.ContainsKey("colors") || stops.ContainsKey("alphas")))
            {
                source.Remove("colorRamp");
                JsonUtility.FromJsonOverwrite(source.ToString(), recipe);
                recipe.colorRamp = JsonUtility.FromJson<RampStops>(stops.ToString()).Decode();
            }
            else
            {
                JsonUtility.FromJsonOverwrite(json, recipe);
                if (stops == null) recipe.colorRamp = null;
            }
            recipe.Validate();
            recipe.schemaVersion = TextureRecipe.CurrentSchema;
            return recipe;
        }

        internal static string Serialize(TextureRecipe recipe)
        {
            recipe.Validate();
            var data = JObject.Parse(JsonUtility.ToJson(recipe));
            var selected = new JObject
            {
                ["schemaVersion"] = TextureRecipe.CurrentSchema,
                ["generator"] = recipe.generator,
                ["generatorVersion"] = recipe.generatorVersion
            };
            foreach (string field in TextureCatalog.SharedFields.Concat(TextureCatalog.Find(recipe.generator).Fields))
            {
                if (!recipe.UsesField(field)) continue;
                selected.Add(field, field == "colorRamp" ? RampStops.Encode(recipe.colorRamp ?? recipe.CreateEndpointRamp()) : data[field]);
            }
            return selected.ToString(Formatting.Indented).Replace("\r\n", "\n") + "\n";
        }

        internal static void Write(string path, TextureRecipe recipe, bool create)
        {
            string contents = Serialize(recipe);
            string destination = Path.GetFullPath(FileUtil.GetPhysicalPath(path));
            string temporary = destination + "." + Guid.NewGuid().ToString("N") + "~";
            try
            {
                File.WriteAllText(temporary, contents, new UTF8Encoding(false));
                if (create) File.Move(temporary, destination);
                else File.Replace(temporary, destination, null);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        public override void OnImportAsset(AssetImportContext context)
        {
            try
            {
                var asset = TextureGenerator.Generate(Read(context.assetPath));
                asset.name = Path.GetFileNameWithoutExtension(context.assetPath);
                context.AddObjectToAsset("texture", asset);
                context.SetMainObject(asset);
            }
            catch (Exception error)
            {
                context.LogImportError(error.Message);
            }
        }

        [MenuItem("Assets/Create/jlinkdev/Procedural Texture")]
        static void CreateAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent("New Procedural Texture.proctex", Serialize(new TextureRecipe()));
        }
    }
}
