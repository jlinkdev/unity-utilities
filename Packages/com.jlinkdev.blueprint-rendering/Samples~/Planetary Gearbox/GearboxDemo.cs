using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.BlueprintRendering.Samples
{
    public sealed class GearboxDemo : MonoBehaviour
    {
        [Serializable]
        public struct Part
        {
            public Transform transform;
            public Vector3 assembled;
            public float lift;
        }

        public UniversalRenderPipelineAsset samplePipeline;
        public BlueprintCamera drawing;
        public Part[] parts;
        [Range(0f, 1f)] public float explode;
        [Range(-180f, 180f)] public float orbit = 38f;

        private RenderPipelineAsset previousDefault, previousQuality;
        private Camera view;
        private bool ownsPipeline;
        private float elevation = 27f;

        private void OnEnable()
        {
            if (drawing == null || samplePipeline == null) return;
            view = drawing.GetComponent<Camera>();
            previousDefault = GraphicsSettings.defaultRenderPipeline;
            previousQuality = QualitySettings.renderPipeline;
            ownsPipeline = true;
            GraphicsSettings.defaultRenderPipeline = samplePipeline;
            QualitySettings.renderPipeline = samplePipeline;
            UpdateView();
        }

        private void OnDisable()
        {
            if (!ownsPipeline) return;
            if (QualitySettings.renderPipeline == samplePipeline) QualitySettings.renderPipeline = previousQuality;
            if (GraphicsSettings.defaultRenderPipeline == samplePipeline) GraphicsSettings.defaultRenderPipeline = previousDefault;
            ownsPipeline = false;
        }

        private void LateUpdate() => UpdateView();

        public void UpdateView()
        {
            if (drawing == null) return;
            if (view == null) view = drawing.GetComponent<Camera>();
            float t = Mathf.SmoothStep(0f, 1f, explode);
            foreach (var part in parts)
                part.transform.localPosition = part.assembled + Vector3.up * (part.lift * t);

            Vector3 center = transform.TransformPoint(Vector3.up * (0.01f + 0.09f * t));
            Quaternion rotation = Quaternion.Euler(elevation - 11f * t, orbit, 0f);
            view.transform.SetPositionAndRotation(center - rotation * Vector3.forward * 0.4f, rotation);
            view.orthographicSize = Mathf.Max(0.067f + 0.059f * t, 0.065f / Mathf.Max(view.aspect, 0.1f));
        }

        private void OnGUI()
        {
            if (drawing == null) return;
            float scale = Mathf.Clamp(Screen.height / 780f, 0.85f, 1.7f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float height = Screen.height / scale;
            float width = Mathf.Min(356, Screen.width / scale - 24);
            var controls = new Rect(12, height - 104, width, 92);
            GUI.Box(controls, GUIContent.none);
            GUILayout.BeginArea(new Rect(controls.x + 12, controls.y + 6, width - 24, 80));
            GUILayout.Label("Planetary gearbox    /    4:1");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Explode", GUILayout.Width(58));
            explode = GUILayout.HorizontalSlider(explode, 0f, 1f);
            GUILayout.Label(explode.ToString("P0"), GUILayout.Width(44));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Layers", GUILayout.Width(58));
            drawing.surfaceLayers = GUILayout.Toolbar(drawing.surfaceLayers - 1, LayerLabels) + 1;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUILayout.BeginArea(new Rect(14, 10, 220, 28));
            GUILayout.Label("Drag to orbit");
            GUILayout.EndArea();

            Event e = Event.current;
            if (e.type == EventType.MouseDrag && e.button == 0 && GUIUtility.hotControl == 0 && !controls.Contains(e.mousePosition))
            {
                orbit += e.delta.x * 0.45f;
                elevation = Mathf.Clamp(elevation + e.delta.y * 0.35f, -75, 75);
                e.Use();
            }
        }

        private static readonly string[] LayerLabels = { "1", "2", "3", "4" };
    }
}
