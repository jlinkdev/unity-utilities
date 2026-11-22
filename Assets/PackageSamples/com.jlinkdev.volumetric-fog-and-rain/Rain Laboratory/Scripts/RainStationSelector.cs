using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace jlinkdev.UnityUtilities.VolumetricRain.EditableSamples
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class RainStationSelector : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private Transform courtyardView;
        [SerializeField] private Transform shelterView;
        [SerializeField, Range(1f, 179f)] private float courtyardFieldOfView = 44f;
        [SerializeField, Range(1f, 179f)] private float shelterFieldOfView = 53f;

        private Camera view;
        private VisualElement root;
        private Button courtyardButton;
        private Button shelterButton;
        private bool inShelter;

        private void Awake() => view = GetComponent<Camera>();
        private void Start() => ShowCourtyard();

        private void Update()
        {
            // A UIDocument recreates its visual tree when it is enabled again.
            var currentRoot = document ? document.rootVisualElement : null;
            if (root != currentRoot) Bind(currentRoot);

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.digit1Key.wasPressedThisFrame) ShowCourtyard();
            if (keyboard.digit2Key.wasPressedThisFrame) ShowShelter();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Alpha1)) ShowCourtyard();
            if (Input.GetKeyDown(KeyCode.Alpha2)) ShowShelter();
#endif
        }

        public void ShowCourtyard() => Select(courtyardView, courtyardFieldOfView, false);
        public void ShowShelter() => Select(shelterView, shelterFieldOfView, true);

        private void Select(Transform station, float fieldOfView, bool sheltered)
        {
            if (!station) return;
            transform.SetPositionAndRotation(station.position, station.rotation);
            view.fieldOfView = fieldOfView;
            inShelter = sheltered;
            UpdateSelection();
        }

        private void OnDisable() => Bind(null);

        private void Bind(VisualElement tree)
        {
            if (courtyardButton != null) courtyardButton.clicked -= ShowCourtyard;
            if (shelterButton != null) shelterButton.clicked -= ShowShelter;
            root = tree;
            courtyardButton = root?.Q<Button>("courtyard-station");
            shelterButton = root?.Q<Button>("shelter-station");
            if (courtyardButton != null) courtyardButton.clicked += ShowCourtyard;
            if (shelterButton != null) shelterButton.clicked += ShowShelter;
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            courtyardButton?.EnableInClassList("selected", !inShelter);
            shelterButton?.EnableInClassList("selected", inShelter);
        }
    }
}
