using UnityEditor;
using UnityEditor.SceneManagement;

namespace jlinkdev.UnityUtilities.ObjectHighlighting.Development
{
    internal static class ObjectHighlightingGalleryMenu
    {
        private const string MenuPath = "Tools/jlinkdev/Object Highlighting/Open Selection Gallery";
        private const string ScenePath =
            "Assets/PackageDevelopment/ObjectHighlighting/SampleAuthoring/Selection Gallery/Selection Gallery.unity";

        [MenuItem(MenuPath)]
        private static void OpenGallery()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanOpenGallery() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
