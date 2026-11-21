using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JLink.Plants.Editor
{
    public static class PlantPrefabBaker
    {
        public static GameObject Bake(ProceduralPlant source,string path,bool living)
        {
            if (!source || !source.CurrentMesh || string.IsNullOrEmpty(path) ||
                !path.StartsWith("Assets/",StringComparison.Ordinal) || path.Contains("..") ||
                !path.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase) || File.Exists(path))
                throw new ArgumentException("Choose a new prefab under Assets and an enabled plant.");
            GameObject root=null;
            string meshPath=null;
            try
            {
                if (living) root=PlantInstances.Living(source).gameObject;
                else
                {
                    root=new GameObject();
                    meshPath=AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(path,".asset"));
                    root.AddComponent<MeshFilter>().sharedMesh=PlantMeshBaker.Bake(source,meshPath);
                    root.AddComponent<MeshRenderer>().sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;
                }
                root.name=Path.GetFileNameWithoutExtension(path);
                if (living) root.GetComponent<MeshFilter>().sharedMesh=null; // Rebuilt from serialized nodes on enable.
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,path,out bool ok);
                if (!ok || !prefab) throw new IOException("Unity could not save the plant prefab.");
                return prefab;
            }
            catch
            {
                if (meshPath!=null && File.Exists(meshPath)) AssetDatabase.DeleteAsset(meshPath);
                if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
                throw;
            }
            finally { if (root) UnityEngine.Object.DestroyImmediate(root); }
        }

        public static void Choose(ProceduralPlant plant,bool living)
        {
            string name=plant.Growth.Recipe.species+" "+plant.Growth.Recipe.seed+(living?" Living":" Static");
            string path=EditorUtility.SaveFilePanelInProject("Save plant prefab",name,"prefab","Choose a destination under Assets.");
            if (!string.IsNullOrEmpty(path)) EditorGUIUtility.PingObject(Bake(plant,AssetDatabase.GenerateUniqueAssetPath(path),living));
        }
    }
}
