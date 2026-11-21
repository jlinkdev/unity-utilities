using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JLink.Plants.Editor
{
    public static class PlantMeshBaker
    {
        public static Mesh Bake(ProceduralPlant plant, string assetPath)
        {
            if (!plant || !plant.CurrentMesh) throw new ArgumentException("Select an enabled plant to bake.");
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                assetPath.Contains("..") || !assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose a .asset file within Assets.");
            if (File.Exists(assetPath)) throw new IOException("A file already exists at that path.");
            var mesh = UnityEngine.Object.Instantiate(plant.CurrentMesh);
            mesh.hideFlags = HideFlags.None;
            mesh.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(mesh, assetPath);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        public static void ChooseDestination(ProceduralPlant plant)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save plant mesh", plant.Growth.Recipe.species +
                " " + plant.Growth.Recipe.seed, "asset", "Save the current growth as a reusable Unity mesh.");
            if (string.IsNullOrEmpty(path)) return;
            var mesh = Bake(plant, AssetDatabase.GenerateUniqueAssetPath(path));
            EditorGUIUtility.PingObject(mesh);
        }
    }

    [CustomEditor(typeof(ProceduralPlant))]
    sealed class ProceduralPlantEditor : UnityEditor.Editor
    {
        int cutNode;
        bool pruning;
        [MenuItem("GameObject/3D Object/Procedural Plant", false, 12)]
        static void CreatePlant(MenuCommand command)
        {
            var go = new GameObject("Procedural Plant");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create procedural plant");
            Undo.AddComponent<ProceduralPlant>(go);
            Selection.activeGameObject = go;
        }

        public override void OnInspectorGUI()
        {
            var plant = (ProceduralPlant)target;
            serializedObject.Update();
            EditorGUILayout.LabelField("Next planting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("recipe"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startingGrowth"));
            serializedObject.ApplyModifiedProperties();
            if (GUILayout.Button("Generate from recipe")) Change(plant, plant.GenerateFromInspector);

            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Growing conditions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("conditions"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("supports"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("leafDetail"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("surface"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stemMaterial"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("leafMaterial"));
            serializedObject.ApplyModifiedProperties();

            if (plant.Growth == null || !plant.isActiveAndEnabled) return;
            var g = plant.Growth;
            EditorGUILayout.HelpBox($"{g.Recipe.species}   ·   Seed {g.Recipe.seed}   ·   Age {g.Age:0.0}   ·   {g.OrganCount}/{g.Capacity} organs", MessageType.None);
            bool care=EditorGUILayout.Toggle("Simulate plant care",plant.Care.Enabled);
            if(care!=plant.Care.Enabled) Change(plant,()=>plant.SetCareEnabled(care));
            if(care)
            {
                EditorGUILayout.LabelField("Soil moisture",plant.Care.Soil.ToString("P0"));
                if(GUILayout.Button("Water pot")) Change(plant,()=>plant.Water());
            }
            using (new EditorGUI.DisabledScope(!plant.CanAdvance))
                if (GUILayout.Button("Advance 1 growth unit")) Change(plant, () => plant.Advance(1));
            if (GUILayout.Button("Save mesh with UVs…")) PlantMeshBaker.ChooseDestination(plant);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save living prefab…")) PlantPrefabBaker.Choose(plant,true);
            if (GUILayout.Button("Save static prefab…")) PlantPrefabBaker.Choose(plant,false);
            EditorGUILayout.EndHorizontal();
            if(g.Recipe.species==PlantSpecies.SpiderPlant)
            {
                for(int i=0;i<g.OrganCount;i++)
                {
                    int runner=g.NodeAt(i).Id;
                    if(g.CanDetachPlantlet(runner) && GUILayout.Button("Detach plantlet from runner "+runner))
                        Change(plant,()=>CreateDaughter(plant,plant.DetachPlantlet(runner),"Spider plantlet"));
                }
            }
            if (!g.Profile.IsVine) return;
            pruning=EditorGUILayout.Foldout(pruning,"Prune a branch",true);
            if (!pruning) return;
            cutNode=EditorGUILayout.IntField("Node ID",cutNode);
            EditorGUILayout.HelpBox("Select a node in the Scene view. Orange marks the branch that will become a separate plant.",MessageType.None);
            using (new EditorGUI.DisabledScope(!g.CanCut(cutNode)))
                if (GUILayout.Button("Cut branch into new plant")) Change(plant,()=>Detach(plant));
            SceneView.RepaintAll();
        }

        void Detach(ProceduralPlant plant)
        {
            CreateDaughter(plant,plant.Cut(cutNode,plantUpright:true),plant.Growth.Recipe.species+" cutting");
            cutNode=0;
        }

        static void CreateDaughter(ProceduralPlant plant,PlantGrowth cutting,string label)
        {
            var copy=PlantInstances.Living(plant,plant.transform.parent,cutting);
            copy.transform.SetPositionAndRotation(plant.transform.position+plant.transform.right*.4f,plant.transform.rotation);
            copy.transform.localScale=plant.transform.localScale;
            copy.name=label;
            Undo.RegisterCreatedObjectUndo(copy.gameObject,"Plant cutting");
        }

        void OnSceneGUI()
        {
            var plant=(ProceduralPlant)target;
            if (!pruning || plant.Growth==null) return;
            var g=plant.Growth;
            var branch=new System.Collections.Generic.HashSet<int>(g.PreviewCut(cutNode));
            var previous=Handles.color;
            for (int i=0;i<g.OrganCount;i++)
            {
                var n=g.NodeAt(i);
                if (!g.CanCut(n.Id)) continue;
                Vector3 p=plant.transform.TransformPoint(n.Base);
                Handles.color=branch.Contains(n.Id)?new Color(1,.45f,.08f):Color.white;
                if (branch.Contains(n.Id)) Handles.DrawLine(p,plant.transform.TransformPoint(n.Tip),2);
                float size=HandleUtility.GetHandleSize(p)*.035f;
                if(Handles.Button(p,Quaternion.identity,size,size,Handles.DotHandleCap)) {cutNode=n.Id;Repaint();}
            }
            Handles.color=previous;
        }

        static void Change(ProceduralPlant plant, Action change)
        {
            Undo.RecordObject(plant, "Change procedural plant");
            change();
            if (!Application.isPlaying)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(plant);
                EditorUtility.SetDirty(plant);
                if (plant.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(plant.gameObject.scene);
            }
        }
    }
}
