using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace jlinkdev.ObjectHighlighting.Samples
{
    public sealed class SelectionGallery : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField] private ObjectHighlight[] parts;
        [SerializeField] private GUISkin captionSkin;

        [SerializeField] private UIDocument identity;
        private readonly VisualElement[] stations = new VisualElement[3];
        private readonly Label[] badges = new Label[3];
        private VisualElement captionRoot;
        private ObjectHighlight hovered, selected;
        private Vector2 lastPointer;
        private bool pointerInitialized;
        private GUIStyle title, caption, label;

        private void OnEnable()
        {
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
        }

        private void LateUpdate()
        {
            // Preserve comfortable framing when the Game view changes aspect.
            float halfHeight = Mathf.Max(2.85f, 5.45f / Mathf.Max(view.aspect, 0.1f));
            if (view.orthographic) view.orthographicSize = halfHeight;
            else view.fieldOfView = 2 * Mathf.Rad2Deg * Mathf.Atan(halfHeight / 12f);

            var root = identity && identity.panelSettings ? identity.rootVisualElement : null;
            if (root == null || root.panel == null) { captionRoot = null; return; }
            if (root != captionRoot)
            {
                captionRoot = root;
                for (int i = 0; i < stations.Length; i++)
                {
                    stations[i] = root.Q("station-" + i);
                    badges[i] = stations[i]?.Q<Label>(className: "station-state");
                }
            }
            for (int i = 0; i < stations.Length; i++)
            {
                var part = parts[i];
                if (badges[i] == null || !part) continue;
                Vector3 point = view.WorldToViewportPoint(part.transform.position + new Vector3(0, -.74f, -1.27f));
                var station = stations[i];
                station.style.left = root.contentRect.width * view.WorldToViewportPoint(part.transform.position).x - 70f;
                station.style.top = root.contentRect.height * (1 - point.y);
                station.EnableInClassList("station-selected", part.IsSelected);
                station.EnableInClassList("station-hover", part.IsHovered && !part.IsSelected);
                badges[i].text = part.IsSelected ? "Selected" : part.IsHovered ? "Hovered" : "Idle";
            }
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
            if (captionRoot != null || Event.current.type != EventType.Repaint) return;

            if (title == null)
            {
                title = captionSkin ? captionSkin.FindStyle("sample-title") : null;
                caption = captionSkin ? captionSkin.FindStyle("sample-caption") : null;
                title ??= new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
                caption ??= new GUIStyle(GUI.skin.label) { fontSize = 14 };
                label = new GUIStyle(caption) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            }
            float scale = Mathf.Clamp(Screen.height / 720f, 0.85f, 2.5f);
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            if (!captionSkin) GUI.Label(new Rect(28, 18, 580, 44), "Object Highlighting / Selection Gallery", title);
            GUI.Label(new Rect(23, captionSkin ? 82 / scale : 60, 650, 30), "Hover to outline. Click to select. Click again or click empty space to clear.", caption);
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
