using SliderDraw = System.Func<UnityEngine.Rect, string, string, float, float, float, float>;
using UnityEngine;

namespace jlinkdev.UnityUtilities.WaterSystem.Samples
{
    /// <summary>Mouse-only laboratory controls.</summary>
    public sealed class WaterLaboratoryController : MonoBehaviour
    {
        public WaterBody Water;
        public Camera ViewCamera;
        public WaterProfile[] Presets;
        public Rigidbody[] FloatingProps;

        private WaterProfile previousProfile;
        private WaterProfile assignedProfile;
        private Vector3[] propPositions;
        private Quaternion[] propRotations;
        private WaterCamera waterCamera;
        private bool autoTour;
        private float cameraHeight = 1.6f;
        private float yaw;
        private float tourTime;
        private int presetIndex;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;

        private void OnEnable()
        {
            if (Water != null)
            {
                previousProfile = Water.Profile;
                assignedProfile = previousProfile;
                presetIndex = -1;
                if (Presets != null)
                    for (int i = 0; i < Presets.Length; i++)
                        if (Presets[i] == previousProfile) presetIndex = i;
            }
            if (ViewCamera != null)
                waterCamera = ViewCamera.GetComponent<WaterCamera>();
        }

        private void Start()
        {
            int count = FloatingProps == null ? 0 : FloatingProps.Length;
            propPositions = new Vector3[count];
            propRotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
            {
                if (FloatingProps[i] == null) continue;
                propPositions[i] = FloatingProps[i].position;
                propRotations[i] = FloatingProps[i].rotation;
            }
            UpdateCamera();
        }

        private void OnDisable()
        {
            if (Water != null && Water.Profile == assignedProfile)
                Water.Profile = previousProfile;
        }

        private void Update()
        {
            if (autoTour)
            {
                tourTime += Time.deltaTime;
                cameraHeight = 1.9f * Mathf.Cos(tourTime * 0.24f);
            }
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            if (ViewCamera == null || Water == null) return;
            Vector3 center = Water.transform.position;
            ViewCamera.transform.position = center + new Vector3(0f, cameraHeight, -8f);
            ViewCamera.transform.rotation = Quaternion.Euler(Mathf.Max(0f, cameraHeight) * 6f, yaw, 0f);
        }

        private void SetHeight(float height)
        {
            autoTour = false;
            cameraHeight = height;
            yaw = 0f;
        }

        private void ResetProps()
        {
            if (FloatingProps == null || propPositions == null) return;
            for (int i = 0; i < Mathf.Min(FloatingProps.Length, propPositions.Length); i++)
            {
                var body = FloatingProps[i];
                if (body == null) continue;
                body.position = propPositions[i];
                body.rotation = propRotations[i];
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
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

        private float DrawValueSlider(string label,string formatted,float value,float min,float max)
        {
            if (SampleSlider != null) return SampleSlider(GUILayoutUtility.GetRect(0, 35, GUILayout.ExpandWidth(true)),label,formatted,value,min,max);
            GUILayout.Label(label+"  "+formatted,labelStyle);
            return GUILayout.HorizontalSlider(value,min,max);
        }

        private void DrawControls()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
                labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 13 };
            }
            float scale = Mathf.Min(Screen.height / 800f, Screen.width / 440f, 1.6f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(20f, Screen.height / scale - 610f, 325f, 590f), GUI.skin.box);
            GUILayout.Label("WATER LABORATORY", titleStyle);
            GUILayout.Label("A continuous surface, waterline and underwater volume", labelStyle);
            GUILayout.Space(12f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Above", GUILayout.Height(30f))) SetHeight(2.1f);
            if (GUILayout.Button("Waterline", GUILayout.Height(30f))) SetHeight(0.02f);
            if (GUILayout.Button("Below", GUILayout.Height(30f))) SetHeight(-1.6f);
            GUILayout.EndHorizontal();
            float newHeight = DrawValueSlider("Camera height",cameraHeight.ToString("+0.00;-0.00;0.00")+" m",cameraHeight,-2.6f,3.5f);
            if (!Mathf.Approximately(newHeight, cameraHeight))
            {
                cameraHeight = newHeight;
                autoTour = false;
            }
            yaw = DrawValueSlider("Look left / right",yaw.ToString("0")+"°",yaw,-65f,65f);
            autoTour = GUILayout.Toggle(autoTour, "  Automatic above / below tour");
            GUILayout.Space(12f);
            GUILayout.Label("Water profile", labelStyle);
            GUILayout.BeginHorizontal();
            if (Presets != null)
            {
                for (int i = 0; i < Presets.Length; i++)
                {
                    string label = Presets[i] != null ? Presets[i].name.Replace(" Water", "") : "Missing";
                    bool selected = GUILayout.Toggle(presetIndex == i, label, GUI.skin.button, GUILayout.Height(30f));
                    if (selected && presetIndex != i && Water != null && Presets[i] != null)
                    {
                        presetIndex = i;
                        Water.Profile = Presets[i];
                        assignedProfile = Presets[i];
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(12f);
            if (GUILayout.Button("Reset floating props", GUILayout.Height(28f))) ResetProps();
            GUILayout.Space(12f);
            GUILayout.Label(waterCamera != null ? $"Camera state: {waterCamera.State}\nNear-plane submersion: {waterCamera.Submersion01:P0}" : "Add WaterCamera for state readout.", labelStyle);
            if (waterCamera != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Normal view")) waterCamera.DebugMode = WaterDebugMode.None;
                if (GUILayout.Button("Water mask")) waterCamera.DebugMode = WaterDebugMode.UnderwaterMask;
                if (GUILayout.Button("Path length")) waterCamera.DebugMode = WaterDebugMode.WaterPathLength;
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(8f);
            GUILayout.Label("Look across the floating crates, then lower the camera through the waves. Submerged steps and columns reveal absorption and caustics.", labelStyle);
            GUILayout.Space(8f);
            GUILayout.Label("All controls use the mouse.", labelStyle);
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }
    }
}
