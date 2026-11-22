using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace jlinkdev.UnityUtilities
{
    // Explicit scene builders call this before saving their newly created scene.
    public static class SampleScenePresentation
    {
        private const string Presentation = "Packages/com.jlinkdev.sample-framework/Runtime/Presentation/";

        public static void Apply(Scene scene, string sampleRoot)
        {
            var identity = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(sampleRoot + "/UI/Sample Identity.uxml");
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(Presentation + "UI/Sample Panel.asset");
            if (!identity || !panel) return;

            var root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Sample Identity");
            if (!root)
            {
                root = new GameObject("Sample Identity");
                SceneManager.MoveGameObjectToScene(root, scene);
            }
            if (!root.TryGetComponent<UIDocument>(out var document))
                document = root.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = identity;

            var skin = AssetDatabase.LoadAssetAtPath<GUISkin>(Presentation + "UI/Sample Controls.guiskin");
            var slider = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Presentation + "UI/IMGUI Slider.asset");
            foreach (var script in scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if (!script) continue;
                var serialized = new SerializedObject(script);
                var property = serialized.FindProperty("sampleSkin");
                if (property == null) continue;
                property.objectReferenceValue = skin;
                var presenter = serialized.FindProperty("sliderPresenter");
                if (presenter != null) presenter.objectReferenceValue = slider;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
