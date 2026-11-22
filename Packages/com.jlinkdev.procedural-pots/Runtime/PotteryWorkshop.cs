using System;
using System.Collections.Generic;
using UnityEngine;

namespace JLink.Pots
{
    /// <summary>Self-contained runtime workshop for a dedicated scene. Coordinates are local to its root.</summary>
    [DisallowMultipleComponent]
    public sealed partial class PotteryWorkshop : MonoBehaviour
    {
        public PotDesign startingDesign;
        [Tooltip("Optional Unity material for previewing finishes, including Substance-generated materials.")]
        public Material surfaceMaterial;
        public ProceduralPot Vessel { get; private set; }
        public static event Action<ProceduralPot, bool> ExportRequested;
        public static event Action<ProceduralPot, bool, bool> PaintableExportRequested;
        [SerializeField] ProceduralPot authoredVessel;
        [SerializeField] Camera view;
        [SerializeField] Transform stage;
        [SerializeField] UnityEngine.UIElements.UIDocument controlsDocument;
        GameObject set;
        Material stageMaterial;
        MaterialPropertyBlock previewProperties;
        bool cutaway;
        bool checker, oneTextureSet = true;
        [SerializeField] private Material previewClay;
        [SerializeField, Range(.01f,.08f)] private float turntableThickness = .025f;
        Material studioClay, shownMaterial;
        [SerializeField] Light finishLight;
        Texture2D checkerTexture;
        Bounds lastBounds;
        Rect lastArea;
        float yaw = 18, pitch = 16, framing, targetY;
        readonly List<string> undo = new List<string>(), redo = new List<string>();
        string gestureStart;
        int selected = -1;
        PotRecipe P => Vessel.recipe;
        const int HistoryLimit = 64;

        void OnEnable()
        {
            Vessel = authoredVessel;
            if (!Vessel) CreateRuntimeTurntable();
            else if (startingDesign && startingDesign.recipe != null) Vessel.Apply(startingDesign.recipe);
            else Vessel.Rebuild();
            studioClay = previewClay ? previewClay : Resources.Load<Material>("JLinkPots/Clay");
            checkerTexture = Resources.Load<Texture2D>("JLinkPots/UvChecker");
            shownMaterial = null;
            MakeThumbnails();
            LoadShelf();
            Fit();
            MakeInterface();
        }

        // Retains programmatic construction for callers creating a workshop on an empty object.
        void CreateRuntimeTurntable()
        {
            set = new GameObject("Pottery turntable");
            set.transform.SetParent(transform, false);
            Vessel = Child("Vessel").AddComponent<ProceduralPot>();
            Vessel.Apply(startingDesign && startingDesign.recipe != null ? startingDesign.recipe : PotRecipe.Create(PotForm.Nursery));
            finishLight = Child("Material preview light").AddComponent<Light>();
            finishLight.type = LightType.Directional;
            finishLight.intensity = 1.25f;
            finishLight.transform.localRotation = Quaternion.Euler(48,-35,0);
            view = Child("Workshop camera").AddComponent<Camera>();
            view.orthographic = true;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.147f, .17f, .17f);
            view.nearClipPlane = .01f;
            view.depth = 5;
            stage = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            stage.name = "Wheel";
            stage.SetParent(set.transform, false);
            Destroy(stage.GetComponent<Collider>());
            stageMaterial = new Material(Resources.Load<Material>("JLinkPots/Clay"));
            stageMaterial.color = new Color(.27f,.32f,.31f);
            stage.GetComponent<Renderer>().sharedMaterial = stageMaterial;
        }

        GameObject Child(string label)
        {
            var child = new GameObject(label);
            child.transform.SetParent(set.transform, false);
            return child;
        }

        void OnDisable()
        {
            if (studioKey && dimStudio) studioKey.intensity = studioKeyIntensity;
            dimStudio = false;
            rootUI?.RemoveFromHierarchy();
            rootUI = null;
            viewTools = null; identityHeading = null;
            if (set)
            {
                set.SetActive(false);
                Destroy(set);
                set = null;
                controlsDocument = null;
            }
            if (stageMaterial) Destroy(stageMaterial);
            if (panelSettings) Destroy(panelSettings);
            canvas = null;
            refreshFields.Clear();
            if (thumbnails != null && thumbnails != presetPreviews) foreach (var image in thumbnails) if (image) Destroy(image);
            undo.Clear(); redo.Clear();
            gestureStart = null;
            drag = Drag.None;
        }

        void Fit()
        {
            Bounds b = Vessel.GeneratedMesh.bounds;
            framing = Mathf.Max(b.size.y * .8f, Mathf.Max(b.size.x, b.size.z) * .78f, .06f);
            if (sampleStyle) framing *= .965f;
            targetY = b.center.y * .94f;
            UpdateView();
            canvas?.MarkDirtyRepaint();
        }

        void UpdateView()
        {
            var area = CanvasArea;
            lastArea = area;
            view.rect = new Rect(area.x * PixelsPerPoint / Screen.width, 1 - area.yMax * PixelsPerPoint / Screen.height,
                area.width * PixelsPerPoint / Screen.width, area.height * PixelsPerPoint / Screen.height);
            view.orthographicSize = framing / Mathf.Min(1, area.width / area.height);
            var rot = Quaternion.Euler(pitch, yaw, 0);
            var localTarget = Vector3.up * targetY;
            // Leave space on the right of the vessel for its editable profile handles.
            if (sampleStyle) localTarget += rot * Vector3.right * (.07f * view.orthographicSize * view.aspect);
            view.transform.localRotation = rot;
            view.transform.localPosition = localTarget - rot * Vector3.forward * (framing * 5 + 1);
            view.farClipPlane = Mathf.Max(view.clearFlags == CameraClearFlags.Skybox ? 150f : 0f, framing * 14 + 5);
            var b = Vessel.GeneratedMesh.bounds;
            lastBounds = b;
            float diameter = Mathf.Max(b.size.x, b.size.z) * 1.26f;
            float thickness = Mathf.Max(.003f, diameter * turntableThickness);
            if (stage)
            {
                stage.localScale = new Vector3(diameter, thickness / 2, diameter);
                stage.localPosition = Vector3.up * (b.min.y - thickness * .5f - .0001f);
            }
            if (previewProperties == null) previewProperties = new MaterialPropertyBlock();
            Vector3 normal = Vector3.ProjectOnPlane(view.transform.forward,transform.up).normalized;
            previewProperties.SetVector("_SectionPlane",new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(normal,Vessel.transform.position)));
            previewProperties.SetFloat("_Section",cutaway ? 1 : 0);
            previewProperties.SetFloat("_ShowChecker",checker ? 1 : 0);
            previewProperties.SetFloat("_StudioExposure", dimStudio ? .64f : 1f);
            if (checkerTexture) previewProperties.SetTexture("_CheckerTex",checkerTexture);
            ApplyFinish();
            if (shownMaterial == studioClay) Vessel.GetComponent<Renderer>().SetPropertyBlock(previewProperties);
        }

        Material VisibleMaterial => checker || cutaway || !surfaceMaterial ? studioClay : surfaceMaterial;

        void ApplyFinish()
        {
            var next = VisibleMaterial;
            if (shownMaterial == next) return;
            var renderer = Vessel.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { next,next,next,next };
            renderer.SetPropertyBlock(next == studioClay ? previewProperties : null);
            shownMaterial = next;
            finishLight.enabled = next != studioClay;
        }

        void LateUpdate()
        {
            PlaceViewTools();
            if (lastArea != CanvasArea || lastBounds != Vessel.GeneratedMesh.bounds) UpdateView();
            else if (shownMaterial != VisibleMaterial) ApplyFinish();
        }

        void BeginGesture()
        {
            if (gestureStart == null) gestureStart = JsonUtility.ToJson(P);
        }

        void EndGesture()
        {
            if (gestureStart != null && gestureStart != JsonUtility.ToJson(P))
            {
                if (undo.Count == HistoryLimit) undo.RemoveAt(0);
                undo.Add(gestureStart);
                redo.Clear();
            }
            gestureStart = null;
        }

        void Restore(List<string> from, List<string> to)
        {
            if (from.Count == 0) return;
            to.Add(JsonUtility.ToJson(P));
            int i = from.Count - 1;
            Vessel.Apply(JsonUtility.FromJson<PotRecipe>(from[i]));
            from.RemoveAt(i);
            selected = -1;
            Fit();
        }

        void Replace(PotRecipe recipe)
        {
            BeginGesture();
            Vessel.Apply(recipe);
            selected = -1;
            EndGesture();
            Fit();
        }

        [Serializable]
        sealed class Collection { public List<PotRecipe> pots = new List<PotRecipe>(); }
        Collection shelf;
        const string ShelfKey = "com.jlinkdev.procedural-pots.workshop.shelf.v1";

        void LoadShelf()
        {
            try { shelf = JsonUtility.FromJson<Collection>(PlayerPrefs.GetString(ShelfKey, "{}")); }
            catch (ArgumentException) { shelf = null; }
            if (shelf == null) shelf = new Collection();
            if (shelf.pots == null) shelf.pots = new List<PotRecipe>();
            shelf.pots.RemoveAll(p => p == null);
        }

        void KeepCopy()
        {
            var copy = P.Copy();
            shelf.pots.Add(copy);
            PlayerPrefs.SetString(ShelfKey, JsonUtility.ToJson(shelf));
            PlayerPrefs.Save();
        }
    }
}
