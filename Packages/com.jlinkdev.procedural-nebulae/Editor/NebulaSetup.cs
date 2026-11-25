using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.ProceduralNebulae.Editor
{
    public static class NebulaSetup
    {
        public const string PresetPath = "Packages/com.jlinkdev.procedural-nebulae/Runtime/Presets/Starlit Banks.prefab";

        [MenuItem("GameObject/Effects/Procedural Nebula",false,20)]
        static void CreateVolume(MenuCommand command)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PresetPath);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            GameObjectUtility.SetParentAndAlign(root,command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(root,"Create procedural nebula");
            Selection.activeGameObject = root;
        }

        [MenuItem("Tools/jlinkdev/Procedural Nebulae/Configure Camera Renderer")]
        public static void ConfigureCameraRenderer()
        {
            var renderer = Selection.activeObject as UniversalRendererData;
            if (!renderer)
            {
                var selected = Selection.activeGameObject;
                var camera = selected ? selected.GetComponent<Camera>() : null;
                renderer = ResolveRenderer(camera ? camera : Camera.main);
            }
            if (!renderer || !AssetDatabase.IsOpenForEdit(renderer))
            {
                Debug.LogWarning("Select an editable Universal Renderer Data asset, or a base camera using one.");
                return;
            }
            Install(renderer);
            Debug.Log("Procedural Nebulae enabled on " + renderer.name, renderer);
        }

        static UniversalRendererData ResolveRenderer(Camera camera)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (!pipeline) return null;
            int index = -1;
            if (camera && camera.TryGetComponent<UniversalAdditionalCameraData>(out var additional))
            {
                using var cameraData = new SerializedObject(additional);
                index = cameraData.FindProperty("m_RendererIndex").intValue;
            }
            using var settings = new SerializedObject(pipeline);
            var list = settings.FindProperty("m_RendererDataList");
            if (index<0 || index>=list.arraySize || !list.GetArrayElementAtIndex(index).objectReferenceValue)
                index = settings.FindProperty("m_DefaultRendererIndex").intValue;
            return list.GetArrayElementAtIndex(index).objectReferenceValue as UniversalRendererData;
        }

        public static void Install(UniversalRendererData renderer)
        {
            Undo.RecordObject(renderer,"Configure nebula renderer");
            NebulaRendererFeature feature = null;
            foreach (var existing in renderer.rendererFeatures)
                if (existing is NebulaRendererFeature found) { feature=found; break; }
            if (!feature)
            {
                feature = ScriptableObject.CreateInstance<NebulaRendererFeature>();
                feature.name = "Procedural Nebulae";
                AssetDatabase.AddObjectToAsset(feature,renderer);
                Undo.RegisterCreatedObjectUndo(feature,"Add nebula renderer");
                renderer.rendererFeatures.Add(feature);
            }
            Undo.RecordObject(feature,"Enable nebula renderer");
            feature.sceneViewOnly = false;
            feature.SetActive(true);
            using var settings = new SerializedObject(renderer);
            var map = settings.FindProperty("m_RendererFeatureMap");
            map.arraySize = renderer.rendererFeatures.Count;
            for (int i=0;i<map.arraySize;i++)
            {
                long id = 0;
                if (renderer.rendererFeatures[i])
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i],out string _,out id);
                map.GetArrayElementAtIndex(i).longValue = id;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            renderer.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssetIfDirty(renderer);
        }
    }
}
