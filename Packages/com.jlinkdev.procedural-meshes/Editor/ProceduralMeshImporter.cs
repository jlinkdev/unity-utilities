using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace JLink.ProceduralMeshes.Editor
{
    [ScriptedImporter(2, "procmesh")]
    public sealed class ProceduralMeshImporter : ScriptedImporter
    {
        [Serializable]
        sealed class RecipeHeader
        {
            public int schemaVersion = 0;
            public string generator = "";
            public int generatorVersion = 0;
        }

        internal static MeshRecipe Read(string path)
        {
            string json = File.ReadAllText(Path.IsPathRooted(path) ? path : FileUtil.GetPhysicalPath(path));
            var header = JsonUtility.FromJson<RecipeHeader>(json);
            if (header == null || string.IsNullOrEmpty(header.generator) || header.generatorVersion != 1 ||
                (header.schemaVersion != 2 && !(header.schemaVersion == 1 && header.generator == "cylinder")))
                throw new InvalidDataException("Unsupported or missing mesh recipe header.");
            // New fields absent from older sources take the current typed defaults.
            var recipe = new MeshRecipe();
            JsonUtility.FromJsonOverwrite(json, recipe);
            recipe.Validate();
            recipe.schemaVersion = 2;
            return recipe;
        }

        internal static string Serialize(MeshRecipe recipe)
        {
            recipe.Validate();
            var data = JObject.Parse(JsonUtility.ToJson(recipe));
            var selected = new JObject
            {
                ["schemaVersion"] = 2,
                ["generator"] = recipe.generator,
                ["generatorVersion"] = recipe.generatorVersion
            };
            foreach (string field in MeshCatalog.SharedFields.Concat(MeshCatalog.Find(recipe.generator).Fields))
                selected.Add(field, data[field]);
            return selected.ToString(Formatting.Indented).Replace("\r\n", "\n") + "\n";
        }

        internal static void Write(string path, MeshRecipe recipe, bool create)
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
                var asset = MeshGenerator.Generate(Read(context.assetPath));
                asset.name = Path.GetFileNameWithoutExtension(context.assetPath);
                context.AddObjectToAsset("mesh", asset, MeshThumbnail.Render(asset));
                context.SetMainObject(asset);
            }
            catch (Exception error)
            {
                context.LogImportError(error.Message);
            }
        }

        [MenuItem("Assets/Create/jlinkdev/Procedural Mesh")]
        static void CreateAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent("New Procedural Mesh.procmesh", Serialize(new MeshRecipe()));
        }
    }
}
