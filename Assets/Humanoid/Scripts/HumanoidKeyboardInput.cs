using UnityEngine;
using UnityEngine.InputSystem;
using static jlinkdev.HumanoidStudio.HumanoidCharacterController;

namespace jlinkdev.HumanoidStudio
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(HumanoidCharacterController))]
    public sealed class HumanoidKeyboardInput : MonoBehaviour
    {
        HumanoidCharacterController character;

        void Awake() => character = GetComponent<HumanoidCharacterController>();

        void Update()
        {
            var keys = Keyboard.current;
            if (keys == null)
            {
                character.SetMovement(Vector3.zero);
                return;
            }
            float x = (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
            float z = (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0);
            var gait = keys.ctrlKey.isPressed ? Gait.Sprint : keys.shiftKey.isPressed ? Gait.Jog : Gait.Walk;
            character.SetMovement(new Vector3(x, 0, z), gait);
            if (x == 0 && z == 0)
            {
                if (keys.leftArrowKey.wasPressedThisFrame) character.FaceDirection(-transform.right);
                else if (keys.rightArrowKey.wasPressedThisFrame) character.FaceDirection(transform.right);
            }
            if (keys.eKey.wasPressedThisFrame) character.Interact();
            if (keys.spaceKey.wasPressedThisFrame) character.SwordSwipe();
        }

        void OnDisable()
        {
            if (character) character.SetMovement(Vector3.zero);
        }
    }
}
