using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace jlinkdev.UnityUtilities
{
    public static class PackageSamplePublisher
    {
        private const string Root = "Assets/PackageSamples/";
        private const string PublishMenu = "Assets/jlinkdev/Publish Package Sample";

        [MenuItem(PublishMenu, true)]
        private static bool CanPublishSelected() => SelectedSample() != null;

        [MenuItem(PublishMenu)]
        private static void PublishSelected() => Publish(SelectedSample());

        private static string SelectedSample()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!path.StartsWith(Root, StringComparison.Ordinal)) return null;
            var parts = path.Substring(Root.Length).Split('/');
            return parts.Length >= 2 ? Root + parts[0] + "/" + parts[1] : null;
        }

        public static void Publish(string sampleRoot)
        {
            if (sampleRoot == null || !sampleRoot.StartsWith(Root, StringComparison.Ordinal))
                throw new ArgumentException("Select an editable package sample.", nameof(sampleRoot));

            var parts = sampleRoot.Substring(Root.Length).Split('/');
            if (parts.Length != 2) throw new ArgumentException("Expected a package and sample folder.");
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + parts[0] + "/package.json");
            if (package == null) throw new InvalidOperationException("Cannot locate package " + parts[0]);
            var checkout = Directory.GetParent(package.resolvedPath).Parent.FullName;
            var script = Path.Combine(checkout, "scripts", "package_samples.py");
            if (!File.Exists(script))
                throw new FileNotFoundException("Publish from a local unity-utilities checkout.", script);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var project = Directory.GetParent(Application.dataPath).FullName;
            var start = new ProcessStartInfo
            {
                FileName = Application.platform == RuntimePlatform.WindowsEditor ? "python" : "python3",
                Arguments = $"\"{script}\" --publish --source-project \"{project}\" \"{parts[0]}\" \"{parts[1]}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(start);
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Sample publish failed. {errors.Result}\n{output.Result}");
            Debug.Log(output.Result.Trim());
        }
    }
}
