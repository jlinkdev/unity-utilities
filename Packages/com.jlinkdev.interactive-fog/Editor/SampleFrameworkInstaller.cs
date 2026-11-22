using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Settings = JLinkDev.SampleInstallation.SampleFrameworkSettings;

namespace JLinkDev.SampleInstallation
{
    // Maintained in Object Highlighting and distributed by scripts/sample_installers.py.
    internal sealed class SampleFrameworkImport : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved,
            string[] movedFrom, bool didDomainReload)
        {
            if (didDomainReload || imported.Any(IsReadme) || moved.Any(IsReadme))
                SampleFrameworkInstaller.ScheduleCheck();
        }

        private static bool IsReadme(string path) =>
            path.StartsWith("Assets/", StringComparison.Ordinal) && path.EndsWith("/README.md", StringComparison.Ordinal);
    }

    internal sealed class SampleFrameworkInstaller : ScriptableSingleton<SampleFrameworkInstaller>
    {
        // Unity serializes UPM requests (including their native operation ID) across script reloads.
        [SerializeField] private AddRequest request;
        private static string GitUrl => Settings.GitRepository + "#" + Settings.GitRevision;
        private static string OperationKey => "jlinkdev.SampleInstall.Active." + Settings.PackageId;
        private static string Owner => typeof(SampleFrameworkInstaller).Assembly.GetName().Name;
        private static string OfferedKey => "jlinkdev.SampleInstall.Offered." + Application.dataPath + "." +
            Settings.PackageId + "." + Settings.GitRevision;
        private static string ManualHint => $"Retry with {Settings.Menu}, or use Package Manager > Install package from git URL:\n{GitUrl}";
        internal static Func<string, bool?> RequestConsent = Prompt;

        internal static void ScheduleCheck()
        {
            EditorApplication.update -= CheckWhenReady;
            EditorApplication.update += CheckWhenReady;
        }

        private static void CheckWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            EditorApplication.update -= CheckWhenReady;

            // One owner across all utilities embedding the helper, even during a domain reload.
            if (SessionState.GetString(OperationKey, "") == Owner)
            {
                if (instance.request != null)
                {
                    Monitor();
                    return;
                }
                SessionState.EraseString(OperationKey);
                if (!CheckInstalled())
                    Debug.LogWarning("Sample Framework installation was interrupted. " + ManualHint);
                return;
            }

            foreach (var sample in Settings.Samples)
            {
                var marker = AssetDatabase.GUIDToAssetPath(sample.Value);
                if (string.IsNullOrEmpty(marker) || !marker.StartsWith("Assets/", StringComparison.Ordinal) || !File.Exists(marker))
                    continue;
                Offer(false, sample.Key);
                return;
            }
        }

        [MenuItem(Settings.Menu)]
        private static void ManualInstall() => Offer(true, Settings.SampleName);

        private static void Offer(bool manual, string sampleName)
        {
            if (SessionState.GetString(OperationKey, "").Length != 0 || CheckInstalled(manual)) return;
            if (!manual && EditorPrefs.GetBool(OfferedKey, false)) return;

            var message = $"{sampleName} uses the shared Sample Framework for its presentation. " +
                $"Install {Settings.PackageId} {Settings.Version} in this project?\n\n{GitUrl}\n\n" +
                "The utility works independently. You can install the framework later from Package Manager.";
            var approved = RequestConsent(message);
            if (!approved.HasValue)
            {
                if (!SessionState.GetBool(OfferedKey, false))
                    Debug.Log("Sample Framework is optional. " + ManualHint);
                SessionState.SetBool(OfferedKey, true);
                return;
            }
            EditorPrefs.SetBool(OfferedKey, true);
            if (approved.Value) InstallApproved();
        }

        // Headless imports can compile and report the manual path without granting consent.
        private static bool? Prompt(string message)
        {
            if (Application.isBatchMode) return null;
            return EditorUtility.DisplayDialog("Install Sample Framework?", message, "Install", "Later");
        }

        private static void InstallApproved()
        {
            if (SessionState.GetString(OperationKey, "").Length != 0 || CheckInstalled()) return;
            SessionState.SetString(OperationKey, Owner);
            try
            {
                instance.request = Client.Add(GitUrl);
                Monitor();
            }
            catch (Exception error)
            {
                Finish(error.Message);
            }
        }

        private static void Monitor()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            var operation = instance.request;
            if (operation == null) { Finish("The UPM request was interrupted."); return; }
            if (!operation.IsCompleted) return;
            if (operation.Status == StatusCode.Failure)
                Finish(operation.Error?.message ?? "Unity Package Manager reported a failure.");
            else if (operation.Result?.name != Settings.PackageId || operation.Result.version != Settings.Version)
                Finish($"The Git revision resolved to {operation.Result?.packageId}; expected {Settings.PackageId}@{Settings.Version}.");
            else
                Finish(null);
        }

        private static bool CheckInstalled(bool manual = false)
        {
            var installed = PackageInfo.GetAllRegisteredPackages().FirstOrDefault(p => p.name == Settings.PackageId);
            if (installed == null) return false;
            if (installed.version != Settings.Version && (manual || !SessionState.GetBool(OfferedKey + installed.version, false)))
            {
                SessionState.SetBool(OfferedKey + installed.version, true);
                Debug.LogWarning($"{Settings.SampleName} supports {Settings.PackageId}@{Settings.Version}. " +
                    $"This project has {installed.version}. Review the version in Package Manager before switching to {GitUrl}.");
            }
            return true;
        }

        private static void Finish(string error)
        {
            EditorApplication.update -= Poll;
            instance.request = null;
            SessionState.EraseString(OperationKey);
            if (error == null) Debug.Log($"Installed {Settings.PackageId}@{Settings.Version} for {Settings.SampleName}.");
            else Debug.LogWarning("Sample Framework installation failed. " + error + "\n" + ManualHint);
        }
    }
}
