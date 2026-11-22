using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields.Samples
{
    public sealed class ForcefieldShowcase : MonoBehaviour
    {
        [SerializeField] private ForcefieldPreset[] presets;

        [SerializeField] private List<Forcefield> fields = new List<Forcefield>();
        [SerializeField] private List<Transform> rotatingCores = new List<Transform>();
        [SerializeField] private GameObject galleryRoot;
        [SerializeField] private GameObject stressRoot;
        [SerializeField] private Camera showcaseCamera;
        private float nextAutomaticImpact;
        private bool automaticImpacts = true;
        private bool stressMode;
        private int presetOffset;

        private void Update()
        {
            for (int i = 0; i < rotatingCores.Count; i++)
            {
                if (rotatingCores[i] != null && rotatingCores[i].gameObject.activeInHierarchy)
                    rotatingCores[i].Rotate(13f * Time.deltaTime, 24f * Time.deltaTime, -9f * Time.deltaTime, Space.Self);
            }

            if (automaticImpacts && Time.time >= nextAutomaticImpact)
            {
                nextAutomaticImpact = Time.time + (stressMode ? 0.12f : 0.42f);
                AddRandomImpact();
            }
        }

        [SerializeField] private GUISkin sampleSkin;

        private void OnGUI()
        {
            var previous = GUI.skin;
            if (sampleSkin) GUI.skin = sampleSkin;
            try { DrawControls(); }
            finally { GUI.skin = previous; }
        }

        private void DrawControls()
        {
            var controls = new Rect(18f, Screen.height - 268f, 310f, 250f);
            GUILayout.BeginArea(controls, GUI.skin.box);
            GUILayout.Label("Shield controls", GUI.skin.label);
            GUILayout.Label("Click a shield to place an impact. The gallery uses one shared material and per-instance preset data.");
            GUILayout.Space(6f);

            automaticImpacts = GUILayout.Toggle(automaticImpacts, " Automatic impacts");

            if (GUILayout.Button("Cycle Presets (blended)"))
                CyclePresets();
            if (GUILayout.Button("Clear All Impacts"))
                ClearImpacts();
            if (GUILayout.Button(stressMode ? "Return to Preset Gallery" : "Show 20-Field Stress Wall"))
                SetStressMode(!stressMode);

            GUILayout.Space(6f);
            GUILayout.Label(stressMode
                ? "Stress wall: 20 active fields with independent 8-slot histories."
                : "Gallery: Clean, Hex, Plasma, Stealth, and Overloaded presets.");
            GUILayout.EndArea();

            if (!stressMode)
                DrawPresetLabels();

            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && !controls.Contains(current.mousePosition))
            {
                Vector3 screenPoint = new Vector3(current.mousePosition.x, Screen.height - current.mousePosition.y, 0f);
                Ray ray = showcaseCamera.ScreenPointToRay(screenPoint);
                if (Physics.Raycast(ray, out RaycastHit hit, 200f))
                {
                    Forcefield forcefield = hit.collider.GetComponent<Forcefield>();
                    if (forcefield != null)
                    {
                        forcefield.AddImpact(hit.point, hit.normal, 1.25f, 0.05f);
                        current.Use();
                    }
                }
            }
        }

        private void AddRandomImpact()
        {
            if (fields.Count == 0)
                return;

            for (int attempt = 0; attempt < fields.Count; attempt++)
            {
                Forcefield field = fields[Random.Range(0, fields.Count)];
                if (field == null || !field.gameObject.activeInHierarchy)
                    continue;

                Vector3 direction = Random.onUnitSphere;
                Renderer renderer = field.TargetRenderers[0];
                float radius = Mathf.Max(renderer.bounds.extents.x, Mathf.Max(renderer.bounds.extents.y, renderer.bounds.extents.z));
                Vector3 point = renderer.bounds.center + direction * radius;
                field.AddImpact(point, direction, Random.Range(0.7f, 1.4f), Random.Range(0.025f, 0.07f));
                return;
            }
        }

        private void CyclePresets()
        {
            if (presets.Length == 0)
                return;

            presetOffset = (presetOffset + 1) % presets.Length;
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i] != null && fields[i].gameObject.activeInHierarchy)
                    fields[i].BlendToPreset(presets[(i + presetOffset) % presets.Length], 0.65f);
            }
        }

        private void DrawPresetLabels()
        {
            for (int i = 0; i < fields.Count; i++)
            {
                Forcefield field = fields[i];
                if (field == null || !field.gameObject.activeInHierarchy || field.Preset == null)
                    continue;

                Renderer renderer = field.TargetRenderers[0];
                Vector3 labelPosition = renderer.bounds.center + Vector3.up * renderer.bounds.extents.y * 1.3f;
                Vector3 screenPosition = showcaseCamera.WorldToScreenPoint(labelPosition);
                if (screenPosition.z <= 0f)
                    continue;

                GUI.Label(
                    new Rect(screenPosition.x - 70f, Screen.height - screenPosition.y, 140f, 24f),
                    field.Preset.name);
            }
        }

        private void ClearImpacts()
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i] != null)
                    fields[i].ClearImpacts();
            }
        }

        private void SetStressMode(bool enabledState)
        {
            stressMode = enabledState;
            galleryRoot.SetActive(!stressMode);
            stressRoot.SetActive(stressMode);
            showcaseCamera.transform.position = stressMode ? new Vector3(0f, 4.2f, -18.5f) : new Vector3(0f, 4.2f, -17.5f);
        }
    }
}
