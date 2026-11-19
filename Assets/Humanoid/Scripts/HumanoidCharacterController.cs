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
        static readonly int[,] TurnStates = {
            { TurnState("Left", 45), TurnState("Left", 90), TurnState("Left", 135), TurnState("Left", 180) },
            { TurnState("Right", 45), TurnState("Right", 90), TurnState("Right", 135), TurnState("Right", 180) }
        };

        static int TurnState(string side, int degrees) => Animator.StringToHash($"Base Layer.Turn {side} {degrees}");

        [SerializeField, Min(0.1f)] float acceleration = 12f;
        [SerializeField, Min(1f)] float turnDegreesPerSecond = 540f;
        Animator animator;
        CharacterController capsule;
        Vector3 desiredVelocity;
        Vector3 requestedFacing;
        float forwardSpeed;
        float verticalSpeed;
        float steeringVelocity;
        int stepState;
        float stepAngle;
        Quaternion stepStart;
        int queuedAction;
        bool actionRequested;

        void Awake()
        {
            animator = GetComponent<Animator>();
            // The motor reads the turn clip's playhead even when the mesh is off screen.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            capsule = GetComponent<CharacterController>();
        }

        // World-space direction. Magnitudes below one scale the selected gait.
        public void SetMovement(Vector3 direction, Gait gait = Gait.Walk)
        {
            direction.y = 0;
            float speed = gait switch { Gait.Sprint => SprintSpeed, Gait.Jog => JogSpeed, _ => WalkSpeed };
            desiredVelocity = Vector3.ClampMagnitude(direction, 1f) * speed;
            if (desiredVelocity.sqrMagnitude > 0f) requestedFacing = Vector3.zero;
        }

        public void FaceDirection(Vector3 worldDirection)
        {
            worldDirection.y = 0;
            if (worldDirection.sqrMagnitude < .0001f) return;
            desiredVelocity = Vector3.zero;
            requestedFacing = worldDirection.normalized;
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
            if (stepState != 0)
            {
                queuedAction = state;
                return;
            }
            actionRequested = true;
            animator.CrossFadeInFixedTime(state, 0.08f, 0, 0);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || !capsule.enabled) return;
            bool acting = actionRequested || PlayingAction();
            actionRequested = false;
            float targetSpeed = acting || stepState != 0 ? 0f : Steer(dt);
            forwardSpeed = acting || stepState != 0 ? 0f : Mathf.MoveTowards(forwardSpeed, targetSpeed, acceleration * dt);
            verticalSpeed = capsule.isGrounded && verticalSpeed < 0f ? -2f : verticalSpeed + Physics.gravity.y * dt;
            capsule.Move((transform.forward * forwardSpeed + Vector3.up * verticalSpeed) * dt);
            var velocity = capsule.velocity;
            animator.SetFloat(Speed, new Vector2(velocity.x, velocity.z).magnitude, 0.08f, dt);
        }

        float Steer(float dt)
        {
            float speed = desiredVelocity.magnitude;
            Vector3 heading = speed > 0f ? desiredVelocity / speed : requestedFacing;
            if (heading.sqrMagnitude == 0f)
            {
                steeringVelocity = 0;
                return 0;
            }
            float angle = Vector3.SignedAngle(transform.forward, heading, Vector3.up);
            float magnitude = Mathf.Abs(angle);
            if (forwardSpeed <= .15f && magnitude >= (speed > 0f ? 35f : 20f))
            {
                if (animator.isInitialized)
                {
                    int size = Mathf.Clamp(Mathf.RoundToInt(magnitude / 45f), 1, 4);
                    stepState = TurnStates[angle < 0f ? 0 : 1, size - 1];
                    stepStart = transform.rotation;
                    stepAngle = angle;
                    steeringVelocity = 0;
                    animator.CrossFadeInFixedTime(stepState, .055f, 0, 0);
                }
                return 0;
            }
            // Brake along the current heading before planting for a reversal.
            if (magnitude >= 115f || (speed == 0 && forwardSpeed > .15f))
            {
                steeringVelocity = 0;
                return 0;
            }
            float yaw = transform.eulerAngles.y;
            float limit = Mathf.Lerp(turnDegreesPerSecond, turnDegreesPerSecond * .5f, forwardSpeed / SprintSpeed);
            float nextYaw = Mathf.SmoothDampAngle(yaw, yaw + angle, ref steeringVelocity, .12f, limit, dt);
            transform.rotation = Quaternion.AngleAxis(Mathf.DeltaAngle(yaw, nextYaw), Vector3.up) * transform.rotation;
            if (magnitude < .2f) requestedFacing = Vector3.zero;
            return speed * Mathf.Clamp01(Vector3.Dot(transform.forward, heading));
        }

        void LateUpdate()
        {
            if (stepState == 0) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (animator.IsInTransition(0))
            {
                var next = animator.GetNextAnimatorStateInfo(0);
                if (next.fullPathHash == stepState) state = next;
            }
            if (state.fullPathHash != stepState) return;
            // The clips counter-rotate the planted shoe against this same yaw curve.
            float yaw = Mathf.SmoothStep(0, stepAngle, state.normalizedTime);
            transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * stepStart;
            if (state.normalizedTime < 1f) return;
            stepState = 0;
            animator.CrossFadeInFixedTime(Animator.StringToHash("Base Layer.Locomotion"), .08f, 0, 0);
            if (queuedAction != 0)
            {
                int action = queuedAction;
                queuedAction = 0;
                BeginAction(action);
            }
        }

        void OnDisable()
        {
            desiredVelocity = Vector3.zero;
            requestedFacing = Vector3.zero;
            if (stepState != 0 && animator && animator.isActiveAndEnabled && animator.isInitialized)
                animator.Play("Base Layer.Locomotion", 0, 0);
            stepState = queuedAction = 0;
            steeringVelocity = 0;
            forwardSpeed = verticalSpeed = 0f;
            actionRequested = false;
        }
    }
}
