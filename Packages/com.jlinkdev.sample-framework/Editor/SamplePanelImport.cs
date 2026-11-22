using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace JLinkDev.SampleFramework.Editor
{
    internal sealed class SamplePanelImport : AssetPostprocessor
    {
        private const string UI = "Packages/com.jlinkdev.sample-framework/Runtime/Presentation/UI/";
        private const string Panel = UI + "Sample Panel.asset";
        private const string StudioPanel = UI + "Studio Panel.asset";
        private const string Theme = UI + "Sample Theme.tss";

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved,
            string[] movedFrom, bool didDomainReload)
        {
            if (didDomainReload || Array.IndexOf(imported, Panel) >= 0 || Array.IndexOf(imported, StudioPanel) >= 0 || Array.IndexOf(imported, Theme) >= 0)
            {
                EditorApplication.delayCall -= ResolveTheme;
                EditorApplication.delayCall += ResolveTheme;
            }
        }

        private static void ResolveTheme()
        {
            // A scene opened before Git installation can load the panel while its TSS is still importing.
            ResolvePanel(Panel);
            ResolvePanel(StudioPanel);
        }

        private static void ResolvePanel(string path)
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (panel && !panel.themeStyleSheet)
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(Theme);
        }
    }
}
