using SliderDraw = System.Func<UnityEngine.Rect, string, string, float, float, float, float>;
using System;
using UnityEngine;

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

        public BlueprintCamera drawing;
        public Part[] parts;
        [Range(0f, 1f)] public float explode;
        [Range(-180f, 180f)] public float orbit = 38f;

        private Camera view;
        private float elevation = 27f;

        private void OnEnable()
        {
            if (drawing == null) return;
            view = drawing.GetComponent<Camera>();
            UpdateView();
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

        [SerializeField] private GUISkin sampleSkin;
        [SerializeField] private ScriptableObject sliderPresenter;
        private SliderDraw drawSampleSlider;
        private SliderDraw SampleSlider
        {
            get
            {
                if (drawSampleSlider == null && sliderPresenter)
                    drawSampleSlider = (SliderDraw)System.Delegate.CreateDelegate(typeof(SliderDraw), sliderPresenter, "Draw", false);
                return drawSampleSlider;
            }
        }


        private void OnGUI()
        {
            var previous = GUI.skin;
            if (sampleSkin) GUI.skin = sampleSkin;
            try { DrawControls(); }
            finally { GUI.skin = previous; }
        }

        private void DrawControls()
        {
            if (drawing == null) return;
            float scale = Mathf.Clamp(Screen.height / 780f, 0.85f, 1.7f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float height = Screen.height / scale;
            float width = Mathf.Min(356, Screen.width / scale - 24);
            var controls = new Rect(12, height - 152, width, 140);
            GUI.Box(controls, GUIContent.none);
            GUILayout.BeginArea(new Rect(controls.x + 12, controls.y + 6, width - 24, 128));
            GUILayout.Label("Planetary gearbox    /    4:1");
            if (SampleSlider != null)
                explode = SampleSlider(GUILayoutUtility.GetRect(0, 35, GUILayout.ExpandWidth(true)),"Explode",explode.ToString("P0"),explode,0,1);
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Explode", GUILayout.Width(58));
                explode = GUILayout.HorizontalSlider(explode, 0f, 1f);
                GUILayout.Label(explode.ToString("P0"), GUILayout.Width(44));
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Layers", GUILayout.Width(58));
            drawing.surfaceLayers = GUILayout.Toolbar(drawing.surfaceLayers - 1, LayerLabels) + 1;
            GUILayout.EndHorizontal();
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
