using UnityEngine;

namespace JLinkDev.HumanoidStudio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(CharacterController))]
    public sealed class HumanoidCharacterController : MonoBehaviour
    {
        public enum Gait { Walk, Jog, Sprint }

        public const float WalkSpeed = 1.5f;
        public const float JogSpeed = 3.2f;
        public const float SprintSpeed = 5.5f;
        static readonly int Speed = Animator.StringToHash("Speed");
        static readonly int InteractState = Animator.StringToHash("Base Layer.Interact");
        static readonly int SwordState = Animator.StringToHash("Base Layer.Sword Swipe");
        static readonly int ActionTag = Animator.StringToHash("Action");

        [SerializeField, Min(0.1f)] float acceleration = 12f;
        [SerializeField, Min(1f)] float turnDegreesPerSecond = 540f;
        Animator animator;
        CharacterController capsule;
        Vector3 desiredVelocity;
        float forwardSpeed;
        float verticalSpeed;
        bool actionRequested;

        void Awake()
        {
            animator = GetComponent<Animator>();
            capsule = GetComponent<CharacterController>();
        }

        // World-space direction. Magnitudes below one scale the selected gait.
        public void SetMovement(Vector3 direction, Gait gait = Gait.Walk)
        {
            direction.y = 0;
            float speed = gait switch { Gait.Sprint => SprintSpeed, Gait.Jog => JogSpeed, _ => WalkSpeed };
            desiredVelocity = Vector3.ClampMagnitude(direction, 1f) * speed;
        }

        public void Interact() => BeginAction(InteractState);
        public void SwordSwipe() => BeginAction(SwordState);

        bool PlayingAction()
        {
            var state = animator.IsInTransition(0)
                ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
            return state.tagHash == ActionTag;
        }

        void BeginAction(int state)
        {
            if (!isActiveAndEnabled || !animator.isInitialized || actionRequested || PlayingAction()) return;
            actionRequested = true;
            animator.CrossFadeInFixedTime(state, 0.08f, 0, 0);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || !capsule.enabled) return;
            bool acting = actionRequested || PlayingAction();
            actionRequested = false;
            float targetSpeed = acting ? 0f : desiredVelocity.magnitude;
            if (targetSpeed > 0f)
            {
                var facing = Quaternion.LookRotation(desiredVelocity);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnDegreesPerSecond * dt);
                targetSpeed *= Mathf.Max(0f, Vector3.Dot(transform.forward, desiredVelocity.normalized));
            }

            forwardSpeed = acting ? 0f : Mathf.MoveTowards(forwardSpeed, targetSpeed, acceleration * dt);
            verticalSpeed = capsule.isGrounded && verticalSpeed < 0f ? -2f : verticalSpeed + Physics.gravity.y * dt;
            capsule.Move((transform.forward * forwardSpeed + Vector3.up * verticalSpeed) * dt);
            var velocity = capsule.velocity;
            animator.SetFloat(Speed, new Vector2(velocity.x, velocity.z).magnitude, 0.08f, dt);
        }

        void OnDisable()
        {
            desiredVelocity = Vector3.zero;
            forwardSpeed = verticalSpeed = 0f;
            actionRequested = false;
        }
    }
}
