using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace jlinkdev.UnityUtilities.VolumetricFogAndRain.Development
{
    // Development-only player smoke build. Not distributed with the UPM package.
    public static class RainBuildValidation
    {
        public static void Build()
        {
            const string root = "Assets/PackageSamples/com.jlinkdev.volumetric-fog-and-rain/Rain Laboratory/";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { root + "Scenes/Rain Laboratory.unity" },
                locationPathName = "Logs/VolumetricRain/Player/Rain.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Rain player build failed: " + report.summary.result);
            UnityEngine.Debug.Log("RAIN_PLAYER_BUILD_PASSED: " + report.summary.totalSize + " bytes");
        }
    }
}
