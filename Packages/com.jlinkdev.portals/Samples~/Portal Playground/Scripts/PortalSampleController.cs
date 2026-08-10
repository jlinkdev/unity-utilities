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
        private float pitch;
        private Vector3 velocity;

        public Vector3 PortalVelocity
        {
            get => velocity;
            set => velocity = value;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null)
                cameraPivot = Camera.main.transform;
            CapturePointer(true);
        }

        private void Update()
        {
            HandlePointer();
            Vector2 moveInput = ReadMove();
            Vector2 lookInput = ReadLook();

            transform.Rotate(Vector3.up, lookInput.x * lookSensitivity, Space.World);
            pitch = Mathf.Clamp(pitch - lookInput.y * lookSensitivity, -85f, 85f);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector3 planar = (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;
            velocity.x = planar.x;
            velocity.z = planar.z;
            if (controller.isGrounded && velocity.y < 0f)
                velocity.y = -2f;
            else
                velocity.y -= gravity * Time.deltaTime;

            controller.Move(velocity * Time.deltaTime);
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
