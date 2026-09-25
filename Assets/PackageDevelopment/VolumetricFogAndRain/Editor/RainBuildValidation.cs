using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain.Development
{
    // Development-only player smoke build. Not distributed with the UPM package.
    public static class RainBuildValidation
    {
        public static void Build()
        {
            const string root = "Assets/PackageDevelopment/VolumetricFogAndRain/SampleAuthoring/Rain Laboratory/";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(root + "Rain Pipeline.asset");
            if (pipeline == null) throw new InvalidOperationException("Missing authored rain laboratory pipeline.");
            string oldGraphicsPath = AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);
            string oldQualityPath = AssetDatabase.GetAssetPath(UnityEngine.QualitySettings.renderPipeline);
            int oldAA = UnityEngine.QualitySettings.antiAliasing;
            try
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
                UnityEngine.QualitySettings.renderPipeline = pipeline;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { root + "Rain Laboratory.unity" },
                    locationPathName = "Logs/VolumetricRain/Player/Rain.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Rain player build failed: " + report.summary.result);
                UnityEngine.Debug.Log("RAIN_PLAYER_BUILD_PASSED: " + report.summary.totalSize + " bytes");
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(oldGraphicsPath);
                UnityEngine.QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(oldQualityPath);
                UnityEngine.QualitySettings.antiAliasing = oldAA;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
