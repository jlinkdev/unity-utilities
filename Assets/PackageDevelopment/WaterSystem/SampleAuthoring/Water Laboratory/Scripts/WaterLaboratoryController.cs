using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace jlinkdev.UnityUtilities.WaterSystem.Samples
{
    /// <summary>Mouse-only laboratory controls and a scoped sample pipeline override.</summary>
    public sealed class WaterLaboratoryController : MonoBehaviour
    {
        public WaterBody Water;
        public Camera ViewCamera;
        public UniversalRenderPipelineAsset Pipeline;
        public WaterProfile[] Presets;
        public Rigidbody[] FloatingProps;

        private RenderPipelineAsset previousGraphics;
        private RenderPipelineAsset previousQuality;
        private UniversalRenderPipelineAsset appliedPipeline;
        private int previousQualityLevel;
        private WaterProfile previousProfile;
        private WaterProfile assignedProfile;
        private Vector3[] propPositions;
        private Quaternion[] propRotations;
        private WaterCamera waterCamera;
        private bool pipelineApplied;
        private bool autoTour;
        private float cameraHeight = 1.6f;
        private float yaw;
        private float tourTime;
        private int presetIndex;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;

        private void OnEnable()
        {
            if (Pipeline != null)
            {
                previousGraphics = GraphicsSettings.defaultRenderPipeline;
                previousQuality = QualitySettings.renderPipeline;
                previousQualityLevel = QualitySettings.GetQualityLevel();
                appliedPipeline = Pipeline;
                GraphicsSettings.defaultRenderPipeline = Pipeline;
                QualitySettings.renderPipeline = Pipeline;
                pipelineApplied = true;
            }
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
            if (pipelineApplied)
            {
                // Respect a deliberate pipeline change by other code while this sample is active.
                if (GraphicsSettings.defaultRenderPipeline == appliedPipeline)
                    GraphicsSettings.defaultRenderPipeline = previousGraphics;
                if (QualitySettings.GetRenderPipelineAssetAt(previousQualityLevel) == appliedPipeline)
                {
                    // Restore the setting we changed even if another script selected a new quality level.
                    int activeLevel = QualitySettings.GetQualityLevel();
                    try
                    {
                        if (activeLevel != previousQualityLevel)
                            QualitySettings.SetQualityLevel(previousQualityLevel, false);
                        QualitySettings.renderPipeline = previousQuality;
                    }
                    finally
                    {
                        if (activeLevel != previousQualityLevel)
                            QualitySettings.SetQualityLevel(activeLevel, false);
                    }
                }
                pipelineApplied = false;
                appliedPipeline = null;
            }
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

        private void OnGUI()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
                labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 13 };
            }
            float scale = Mathf.Clamp(Screen.height / 800f, 0.7f, 1.6f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(20f, 20f, 325f, 590f), GUI.skin.box);
            GUILayout.Label("WATER LABORATORY", titleStyle);
            GUILayout.Label("A continuous surface, waterline and underwater volume", labelStyle);
            GUILayout.Space(12f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Above", GUILayout.Height(30f))) SetHeight(2.1f);
            if (GUILayout.Button("Waterline", GUILayout.Height(30f))) SetHeight(0.02f);
            if (GUILayout.Button("Below", GUILayout.Height(30f))) SetHeight(-1.6f);
            GUILayout.EndHorizontal();
            GUILayout.Label($"Camera height  {cameraHeight:+0.00;-0.00;0.00} m", labelStyle);
            float newHeight = GUILayout.HorizontalSlider(cameraHeight, -2.6f, 3.5f);
            if (!Mathf.Approximately(newHeight, cameraHeight))
            {
                cameraHeight = newHeight;
                autoTour = false;
            }
            GUILayout.Label("Look left / right", labelStyle);
            yaw = GUILayout.HorizontalSlider(yaw, -65f, 65f);
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
            GUILayout.Label("All controls use the mouse. The sample pipeline is active only while this controller is enabled and restores the previous settings on exit.", labelStyle);
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }
    }
}
