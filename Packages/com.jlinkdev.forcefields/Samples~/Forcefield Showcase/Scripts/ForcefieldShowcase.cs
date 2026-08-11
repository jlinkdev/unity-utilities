using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Forcefields.Samples
{
    public sealed class ForcefieldShowcase : MonoBehaviour
    {
        [SerializeField] private Material forcefieldMaterial;
        [SerializeField] private ForcefieldPreset[] presets;

        private readonly List<Forcefield> fields = new List<Forcefield>();
        private readonly List<Transform> rotatingCores = new List<Transform>();
        private GameObject galleryRoot;
        private GameObject stressRoot;
        private Material environmentMaterial;
        private Material accentMaterial;
        private Material coreMaterial;
        private Camera showcaseCamera;
        private float nextAutomaticImpact;
        private bool automaticImpacts = true;
        private bool stressMode;
        private int presetOffset;

        private void Start()
        {
            BuildShowcase();
        }

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

        private void OnGUI()
        {
            const float width = 310f;
            GUILayout.BeginArea(new Rect(18f, 18f, width, 250f), GUI.skin.box);
            GUILayout.Label("jlinkdev Forcefields", GUI.skin.label);
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
            if (current.type == EventType.MouseDown && current.button == 0 && !new Rect(18f, 18f, width, 250f).Contains(current.mousePosition))
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

        private void BuildShowcase()
        {
            if (forcefieldMaterial == null || presets == null || presets.Length == 0)
            {
                Debug.LogError("The Forcefield Showcase is missing its material or preset references.", this);
                enabled = false;
                return;
            }

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
                litShader = Shader.Find("Standard");
            environmentMaterial = new Material(litShader) { name = "Showcase Environment (Runtime)" };
            environmentMaterial.SetColor("_BaseColor", new Color(0.025f, 0.035f, 0.055f, 1f));
            accentMaterial = new Material(litShader) { name = "Showcase Refraction Accents (Runtime)" };
            accentMaterial.SetColor("_BaseColor", new Color(0.09f, 0.24f, 0.38f, 1f));
            accentMaterial.SetColor("_EmissionColor", new Color(0.015f, 0.12f, 0.24f, 1f));
            accentMaterial.EnableKeyword("_EMISSION");
            coreMaterial = new Material(litShader) { name = "Showcase Core (Runtime)" };
            coreMaterial.SetColor("_BaseColor", new Color(0.08f, 0.12f, 0.18f, 1f));
            coreMaterial.SetColor("_EmissionColor", new Color(0.03f, 0.18f, 0.28f, 1f));
            coreMaterial.EnableKeyword("_EMISSION");

            BuildCameraAndLighting();
            BuildEnvironment();
            BuildGallery();
            BuildStressWall();
            SetStressMode(false);
        }

        private void BuildCameraAndLighting()
        {
            GameObject cameraObject = new GameObject("Showcase Camera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(0f, 4.2f, -17.5f);
            cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.3f, 0f) - cameraObject.transform.position);
            showcaseCamera = cameraObject.AddComponent<Camera>();
            showcaseCamera.tag = "MainCamera";
            showcaseCamera.fieldOfView = 48f;
            showcaseCamera.clearFlags = CameraClearFlags.SolidColor;
            showcaseCamera.backgroundColor = new Color(0.004f, 0.007f, 0.015f, 1f);
            showcaseCamera.nearClipPlane = 0.1f;
            cameraObject.AddComponent<AudioListener>();

            GameObject lightObject = new GameObject("Key Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light key = lightObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.62f, 0.78f, 1f);
            key.intensity = 1.25f;

            GameObject fillObject = new GameObject("Warm Fill");
            fillObject.transform.SetParent(transform, false);
            fillObject.transform.position = new Vector3(-5f, 3f, -2f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(1f, 0.28f, 0.08f);
            fill.intensity = 4f;
            fill.range = 12f;
        }

        private void BuildEnvironment()
        {
            GameObject floor = CreatePrimitive("Floor", PrimitiveType.Cube, new Vector3(0f, -0.35f, 2f), new Vector3(22f, 0.5f, 18f), environmentMaterial, transform);
            Destroy(floor.GetComponent<Collider>());

            for (int i = 0; i < 11; i++)
            {
                float x = -10f + i * 2f;
                GameObject bar = CreatePrimitive(
                    "Refraction Bar " + i,
                    PrimitiveType.Cube,
                    new Vector3(x, 2.2f + (i % 2) * 0.55f, 3.4f),
                    new Vector3(0.38f, 5.5f, 0.38f),
                    accentMaterial,
                    transform);
                Destroy(bar.GetComponent<Collider>());
            }
        }

        private void BuildGallery()
        {
            galleryRoot = new GameObject("Preset Gallery");
            galleryRoot.transform.SetParent(transform, false);

            Vector3[] positions =
            {
                new Vector3(-6.4f, 1.35f, 0f),
                new Vector3(-3.2f, 1.35f, 0.1f),
                new Vector3(0f, 1.45f, 0f),
                new Vector3(3.2f, 1.35f, 0.1f),
                new Vector3(6.4f, 1.35f, 0f)
            };
            Vector3[] scales =
            {
                new Vector3(2.4f, 2.4f, 2.4f),
                new Vector3(2.25f, 2.75f, 2.25f),
                new Vector3(2.5f, 2.5f, 2.5f),
                new Vector3(2.2f, 2.8f, 2.2f),
                new Vector3(2.45f, 2.45f, 2.45f)
            };

            for (int i = 0; i < positions.Length; i++)
                CreateField("Gallery - " + presets[i % presets.Length].name, PrimitiveType.Sphere, positions[i], scales[i], presets[i % presets.Length], galleryRoot.transform, 16);
        }

        private void BuildStressWall()
        {
            stressRoot = new GameObject("Stress Wall");
            stressRoot.transform.SetParent(transform, false);

            const int columns = 5;
            const int rows = 4;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    Vector3 position = new Vector3(-6f + column * 3f, 0.6f + row * 1.95f, 2f);
                    ForcefieldPreset selectedPreset = presets[(row * columns + column) % presets.Length];
                    CreateField("Stress Field", PrimitiveType.Sphere, position, Vector3.one * 1.35f, selectedPreset, stressRoot.transform, 8);
                }
            }
        }

        private Forcefield CreateField(
            string objectName,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            ForcefieldPreset selectedPreset,
            Transform parent,
            int capacity)
        {
            GameObject shell = CreatePrimitive(objectName, primitiveType, position, scale, forcefieldMaterial, parent);
            Forcefield field = shell.AddComponent<Forcefield>();
            field.TargetRenderers = new[] { shell.GetComponent<Renderer>() };
            field.PropagationMode = ForcefieldPropagationMode.Spherical;
            field.ApplyPreset(selectedPreset);
            SetCapacity(field, capacity);
            field.Refresh();
            fields.Add(field);

            GameObject core = CreatePrimitive("Refractive Core", PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.58f, coreMaterial, shell.transform);
            core.transform.localPosition = Vector3.zero;
            core.transform.localRotation = Quaternion.Euler(25f, 35f, 12f);
            Destroy(core.GetComponent<Collider>());
            rotatingCores.Add(core.transform);
            return field;
        }

        private static void SetCapacity(Forcefield field, int capacity)
        {
            field.ImpactBufferCapacity = capacity <= 8
                ? ForcefieldImpactCapacity.Eight
                : ForcefieldImpactCapacity.Sixteen;
        }

        private static GameObject CreatePrimitive(
            string objectName,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
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

        private void OnDestroy()
        {
            if (environmentMaterial != null)
                Destroy(environmentMaterial);
            if (accentMaterial != null)
                Destroy(accentMaterial);
            if (coreMaterial != null)
                Destroy(coreMaterial);
        }
    }
}
