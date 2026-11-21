using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace jlinkdev.ObjectHighlighting.Samples
{
    public sealed class SelectionGallery : MonoBehaviour
    {
        [SerializeField] private UniversalRenderPipelineAsset pipeline;
        [SerializeField] private Camera view;
        [SerializeField] private ObjectHighlight[] parts;

        private RenderPipelineAsset previousDefault, previousQuality;
        private ObjectHighlight hovered, selected;
        private Vector2 lastPointer;
        private bool pointerInitialized;
        private GUIStyle title, caption, label;

        private void OnEnable()
        {
            previousDefault = GraphicsSettings.defaultRenderPipeline;
            previousQuality = QualitySettings.renderPipeline;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            hovered = parts[0];
            selected = parts[1];
            hovered.SetHovered(true);
            selected.SetSelected(true);
            pointerInitialized = false;
        }

        private void OnDisable()
        {
            if (hovered) hovered.SetHovered(false);
            if (selected) selected.SetSelected(false);
            if (QualitySettings.renderPipeline == pipeline) QualitySettings.renderPipeline = previousQuality;
            if (GraphicsSettings.defaultRenderPipeline == pipeline) GraphicsSettings.defaultRenderPipeline = previousDefault;
        }

        private void LateUpdate()
        {
            // Preserve comfortable framing when the Game view changes aspect.
            view.orthographicSize = Mathf.Max(2.85f, 5.55f / Mathf.Max(view.aspect, 0.1f));
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null) return;
            UpdatePointer(mouse.position.ReadValue(), mouse.leftButton.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            UpdatePointer(Input.mousePosition, Input.GetMouseButtonDown(0));
#endif
        }

        private void UpdatePointer(Vector2 pointer, bool click)
        {
            if (click || (pointerInitialized && pointer != lastPointer))
            {
                var ray = view.ScreenPointToRay(pointer);
                var target = Physics.Raycast(ray, out var hit) ? hit.collider.GetComponentInParent<ObjectHighlight>() : null;
                if (hovered != target)
                {
                    if (hovered) hovered.SetHovered(false);
                    hovered = target;
                    if (hovered) hovered.SetHovered(true);
                }
                if (click)
                {
                    var next = target == selected ? null : target;
                    if (selected) selected.SetSelected(false);
                    selected = next;
                    if (selected) selected.SetSelected(true);
                }
            }
            pointerInitialized = true;
            lastPointer = pointer;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;

            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
                caption = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                label = new GUIStyle(caption) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            }
            float scale = Mathf.Clamp(Screen.height / 720f, 0.85f, 2.5f);
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUI.Label(new Rect(28, 18, 330, 44), "Selection gallery", title);
            GUI.Label(new Rect(30, 60, 650, 30), "Hover to outline. Click to select. Click again or click empty space to clear.", caption);
            foreach (var part in parts)
            {
                Vector3 p = view.WorldToScreenPoint(part.transform.position + new Vector3(0, -0.95f, -1.5f));
                string state = part.IsSelected ? "Selected" : part.IsHovered ? "Hovered" : "Idle";
                label.normal.textColor = part.IsSelected ? new Color(1, 0.78f, 0.34f) :
                    part.IsHovered ? new Color(0.32f, 0.86f, 1) : Color.gray;
                GUI.Label(new Rect(p.x / scale - 90, (Screen.height - p.y) / scale, 180, 25), part.name, label);
                GUI.Label(new Rect(p.x / scale - 90, (Screen.height - p.y) / scale + 22, 180, 25), state, label);
            }
            GUI.matrix = previous;
        }
    }
}
