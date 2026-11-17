using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using jlinkdev.UnityUtilities.InteractiveFog.Editor;

namespace jlinkdev.UnityUtilities.InteractiveFog.Development
{
    /// <summary>Copy only this helper into a clean consumer project after installing the package archive.</summary>
    public static class InteractiveFogPackageImportValidation
    {
        public static void ImportSample()
        {
            var sample=Sample.FindByPackage("com.jlinkdev.interactive-fog","0.4.0").Single(s=>s.displayName=="Mist Sanctuary");
            if(!sample.Import(Sample.ImportOptions.OverridePreviousImports|Sample.ImportOptions.HideImportWindow)) throw new Exception("Sample import failed");
            string path=sample.importPath.Replace('\\','/');
            string root=Path.GetDirectoryName(Application.dataPath).Replace('\\','/')+"/";
            if(path.StartsWith(root,StringComparison.OrdinalIgnoreCase)) path=path.Substring(root.Length);
            File.WriteAllText("FogImportedSamplePath.txt",path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        public static void Verify()
        {
            string sample=File.ReadAllText("FogImportedSamplePath.txt").Trim();
            string scenePath=sample+"/Scenes/Mist Sanctuary.unity";
            var scene=EditorSceneManager.OpenScene(scenePath);
            if(UnityEngine.Object.FindFirstObjectByType<CharacterController>()) throw new Exception("The sample must use only its animation path, not a character controller");
            foreach(var root in scene.GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new Exception("Missing script: "+t.name);
                foreach(var component in t.GetComponents<Component>())
                {
                    var properties=new SerializedObject(component).GetIterator();
                    while(properties.Next(true)) if(properties.propertyType==SerializedPropertyType.ObjectReference && properties.objectReferenceValue==null && properties.objectReferenceInstanceIDValue!=0)
                        throw new Exception("Unresolved reference: "+t.name+"."+properties.propertyPath);
                }
            }
            foreach(var path in AssetDatabase.GetDependencies(scenePath,true))
                if(path.StartsWith("Assets/")&&!path.StartsWith(sample+"/")) throw new Exception("Sample dependency outside import: "+path);
            var fog=UnityEngine.Object.FindFirstObjectByType<FogVolume>();
            if(!fog||!fog.simulation||!fog.sun) throw new Exception("Missing fog configuration");
            fog.Initialize(); fog.Advance(1f/30); if(!fog.IsReady||fog.StepCount!=1) throw new Exception("GPU simulation did not initialize");
            var controller=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>()).Single(c=>c.GetType().Name=="MistSanctuary");
            var serialized=new SerializedObject(controller);
            if(!serialized.FindProperty("view").objectReferenceValue || !serialized.FindProperty("actor").objectReferenceValue) throw new Exception("Missing sample controls references");
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(!pipeline) throw new Exception("Import the sample into a URP project before running setup validation");
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(sample+"/Settings/Mist Renderer.asset");
            if(!renderer || !renderer.rendererFeatures.OfType<FogRendererFeature>().Any()) throw new Exception("Missing sample fog renderer");
            var graphics=GraphicsSettings.defaultRenderPipeline;var quality=QualitySettings.renderPipeline;
            var camera=serialized.FindProperty("view").objectReferenceValue as Camera;
            int index=FogSetup.AssignRendererToCamera(pipeline,renderer,camera);
            if(index<0 || GraphicsSettings.defaultRenderPipeline!=graphics || QualitySettings.renderPipeline!=quality) throw new Exception("Camera setup changed global pipelines");
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText("FogPackageImportValidation.txt","PASS: archive import, sample scripts/references, independent Assets dependencies, explicit camera renderer assignment, and GPU initialization. Unity "+Application.unityVersion);
            Debug.Log("FOG_IMPORT_VERIFIED");
        }
    }
}
