using UnityEditor;
using UnityEngine;

namespace JLink.Pots.Editor
{
    [InitializeOnLoad]
    public static class PotWorkshopEditor
    {
        static PotWorkshopEditor()
        {
            PotteryWorkshop.ExportRequested += PromptSave;
            PotteryWorkshop.PaintableExportRequested += PromptPaintable;
        }

        static void PromptPaintable(ProceduralPot source,bool singleMaterial,bool obj)
        {
            string path = obj
                ? EditorUtility.SaveFilePanel("Export for Substance 3D Painter","","Pot","obj")
                : EditorUtility.SaveFilePanelInProject("Save paintable mesh","Paintable pot","asset","Choose a mesh asset location.");
            if (string.IsNullOrEmpty(path)) return;
            var mesh = new Mesh { name = "Paintable pot" };
            try
            {
                new PotMesh().Build(source.recipe.Copy(),mesh,PotUvMode.Paintable,singleMaterial);
                if (obj)
                {
                    PotObjExporter.Write(mesh,path);
                    string relative = FileUtil.GetProjectRelativePath(path);
                    if (!string.IsNullOrEmpty(relative) && relative.StartsWith("Assets/",System.StringComparison.Ordinal))
                        AssetDatabase.ImportAsset(relative);
                }
                else
                {
                    AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(path));
                    AssetDatabase.SaveAssetIfDirty(mesh);
                    EditorGUIUtility.PingObject(mesh);
                }
            }
            finally { if (!AssetDatabase.Contains(mesh)) Object.DestroyImmediate(mesh); }
        }

        static void PromptSave(ProceduralPot source, bool mesh)
        {
            string kind = mesh ? "mesh" : "design";
            string path = EditorUtility.SaveFilePanelInProject("Save pot " + kind,
                "Pot " + kind, "asset", "Choose where to keep the pot " + kind + ".");
            if (string.IsNullOrEmpty(path)) return;
            var asset = SaveCopy(source,mesh,AssetDatabase.GenerateUniqueAssetPath(path));
            EditorGUIUtility.PingObject(asset);
        }

        public static Object SaveCopy(ProceduralPot source, bool mesh, string path)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                throw new System.ArgumentException("Choose an unused asset path.", nameof(path));
            source.Rebuild();
            Object asset;
            if (mesh)
            {
                var copy = Object.Instantiate(source.GeneratedMesh);
                copy.hideFlags = HideFlags.None;
                asset = copy;
            }
            else
            {
                var design = ScriptableObject.CreateInstance<PotDesign>();
                design.recipe = source.recipe.Copy();
                asset = design;
            }
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset,path);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        [CustomEditor(typeof(ProceduralPot))]
        sealed class PotInspector : UnityEditor.Editor
        {
            bool singleMaterial = true;
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                var pot = (ProceduralPot)target;
                var design = (PotDesign)EditorGUILayout.ObjectField("Apply saved design", null, typeof(PotDesign), false);
                if (design)
                {
                    Undo.RecordObject(pot,"Apply pot design");
                    pot.Apply(design.recipe);
                    EditorUtility.SetDirty(pot);
                }
                if (GUILayout.Button("Save editable design")) PromptSave(pot,false);
                if (GUILayout.Button("Save baked mesh")) PromptSave(pot,true);
                if (GUILayout.Button("Refit UVs to current shape"))
                {
                    Undo.RecordObject(pot,"Refit pot UVs");
                    pot.recipe.RefitUVs();
                    pot.Rebuild();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pot);
                    EditorUtility.SetDirty(pot);
                }
                singleMaterial = EditorGUILayout.Toggle("Single Painter texture set",singleMaterial);
                if (GUILayout.Button("Export Painter OBJ")) PromptPaintable(pot,singleMaterial,true);
                if (GUILayout.Button("Save paintable mesh")) PromptPaintable(pot,singleMaterial,false);
            }
        }
    }
}
