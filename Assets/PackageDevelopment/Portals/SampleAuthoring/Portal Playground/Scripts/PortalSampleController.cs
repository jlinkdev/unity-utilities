using jlinkdev.UnityUtilities.Portals;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace jlinkdev.UnityUtilities.Portals.Samples
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PortalSampleController : MonoBehaviour, IPortalVelocityProvider
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float lookSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float gravity = 18f;

        private CharacterController controller;
        private Camera gameplayCamera;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 startScale;
        private float referenceWorldScale;
        private float baseNearClipPlane;
        private float pitch;
        private Vector3 velocity;

        public static PortalSampleController Active { get; private set; }

        public float CurrentScaleRatio => referenceWorldScale > 0f
            ? WorldScale(transform) / referenceWorldScale
            : 1f;

        public Vector3 PortalVelocity
        {
            get => velocity;
            set => velocity = value;
        }

        private void Awake()
        {
            Active = this;
            controller = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null)
                cameraPivot = Camera.main.transform;

            gameplayCamera = cameraPivot != null ? cameraPivot.GetComponent<Camera>() : Camera.main;
            startPosition = transform.position;
            startRotation = transform.rotation;
            startScale = transform.localScale;
            referenceWorldScale = WorldScale(transform);
            baseNearClipPlane = gameplayCamera != null ? gameplayCamera.nearClipPlane : 0.05f;
            ApplyScaleSettings();
            CapturePointer(true);
        }

        private void OnDestroy()
        {
            if (Active == this)
                Active = null;
        }

        private void Update()
        {
            HandlePointer();
            if (ReadReset())
            {
                ResetExplorer();
                return;
            }

            Vector2 moveInput = ReadMove();
            Vector2 lookInput = ReadLook();

            transform.Rotate(Vector3.up, lookInput.x * lookSensitivity, Space.World);
            pitch = Mathf.Clamp(pitch - lookInput.y * lookSensitivity, -85f, 85f);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            float scale = Mathf.Max(CurrentScaleRatio, 0.001f);
            Vector3 planar = (transform.right * moveInput.x + transform.forward * moveInput.y) * (moveSpeed * scale);
            velocity.x = planar.x;
            velocity.z = planar.z;
            if (controller.isGrounded && velocity.y < 0f)
                velocity.y = -2f * scale;
            else
                velocity.y -= gravity * scale * Time.deltaTime;

            controller.Move(velocity * Time.deltaTime);
            ApplyScaleSettings();
        }

        private void ResetExplorer()
        {
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(startPosition, startRotation);
            transform.localScale = startScale;
            controller.enabled = wasEnabled;
            velocity = Vector3.zero;
            pitch = 0f;
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.identity;
            ApplyScaleSettings();
            Physics.SyncTransforms();
        }

        private void ApplyScaleSettings()
        {
            if (gameplayCamera == null)
                return;

            gameplayCamera.nearClipPlane = Mathf.Max(0.001f, baseNearClipPlane * CurrentScaleRatio);
        }

        private static float WorldScale(Transform target)
        {
            Vector3 scale = target.lossyScale;
            return Mathf.Pow(Mathf.Max(Mathf.Abs(scale.x * scale.y * scale.z), 0.000000001f), 1f / 3f);
        }

        private static Vector2 ReadMove()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2.zero;
            return new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f)).normalized;
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#endif
        }

        private static Vector2 ReadLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
#endif
        }

        private static bool ReadReset()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        private static void HandlePointer()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                CapturePointer(false);
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                CapturePointer(true);
#else
            if (Input.GetKeyDown(KeyCode.Escape))
                CapturePointer(false);
            else if (Input.GetMouseButtonDown(0))
                CapturePointer(true);
#endif
        }

        private static void CapturePointer(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }
    }
}
