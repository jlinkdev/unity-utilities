using System.Linq;
using jlinkdev.UnityUtilities.WorldScanning.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.WorldScanning.Editor
{
    public static class ScanSetupUtility
    {
        [MenuItem("Tools/jlinkdev/World Scanning/Validate Project Setup")]
        public static void ValidateSetup()
        {
            UniversalRendererData rendererData = FindDefaultRendererData();
            if (rendererData == null)
            {
                EditorUtility.DisplayDialog("World Scanning", "The active URP asset does not expose a Universal Renderer Data asset.", "OK");
                return;
            }

            bool configured = rendererData.rendererFeatures.Any(feature => feature is ScanRendererFeature);
            string message = configured
                ? $"Ready. '{rendererData.name}' contains the World Scan Renderer Feature."
                : $"'{rendererData.name}' is missing the World Scan Renderer Feature.";
            EditorUtility.DisplayDialog("World Scanning", message, "OK");
        }

        [MenuItem("Tools/jlinkdev/World Scanning/Add Renderer Feature")]
        public static void AddRendererFeature()
        {
            UniversalRendererData rendererData = FindDefaultRendererData();
            if (rendererData == null)
            {
                EditorUtility.DisplayDialog("World Scanning", "Assign a Universal Render Pipeline asset before running setup.", "OK");
                return;
            }

            if (rendererData.rendererFeatures.Any(feature => feature is ScanRendererFeature))
            {
                EditorUtility.DisplayDialog("World Scanning", $"'{rendererData.name}' is already configured.", "OK");
                return;
            }

            ScanRendererFeature feature = ScriptableObject.CreateInstance<ScanRendererFeature>();
            feature.name = "World Scan Renderer Feature";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            rendererData.rendererFeatures.Add(feature);
            feature.Create();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("World Scanning", $"Added the renderer feature to '{rendererData.name}'.", "OK");
        }

        internal static UniversalRendererData FindDefaultRendererData()
        {
            RenderPipelineAsset pipelineAsset = GraphicsSettings.defaultRenderPipeline ?? QualitySettings.renderPipeline;
            if (pipelineAsset is not UniversalRenderPipelineAsset universalAsset)
                return null;

            SerializedObject serializedAsset = new SerializedObject(universalAsset);
            SerializedProperty rendererList = serializedAsset.FindProperty("m_RendererDataList");
            if (rendererList == null || rendererList.arraySize == 0)
                return null;
            int defaultIndex = 0;
            SerializedProperty defaultRenderer = serializedAsset.FindProperty("m_DefaultRendererIndex");
            if (defaultRenderer != null)
                defaultIndex = Mathf.Clamp(defaultRenderer.intValue, 0, rendererList.arraySize - 1);
            return rendererList.GetArrayElementAtIndex(defaultIndex).objectReferenceValue as UniversalRendererData;
        }
    }
}
