using UnityEngine;
using UnityEngine.Serialization;

namespace jlinkdev.UnityUtilities.IK
{
    public sealed class TwoBoneIK : MonoBehaviour
    {
        private const float LengthEpsilon = 0.000001f;

        [SerializeField, Tooltip("Upper joint of the limb (for example shoulder or hip).")]
        private Transform root;

        [SerializeField, Tooltip("Middle joint of the limb (for example elbow or knee).")]
        private Transform mid;

        [SerializeField, Tooltip("End joint of the limb (for example wrist or ankle).")]
        private Transform tip;

        [SerializeField, Tooltip("Transform the tip should move toward.")]
        private Transform target;

        [SerializeField, Tooltip("Optional pole transform that controls bend direction.")]
        private Transform pole;

        [FormerlySerializedAs("weight")]
        [SerializeField, Range(0f, 1f), Tooltip("Blend applied to target position solving.")]
        private float positionWeight = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("Blend applied when matching the target rotation.")]
        private float rotationWeight = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("Influence of the pole over the animated or remembered bend direction.")]
        private float poleWeight = 1f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Fraction of total reach used to soften the chain near full extension. Set to zero for hard reach clamping.")]
        private float softReach = 0.05f;

        [SerializeField, Tooltip("When enabled, tip rotation can blend toward target rotation.")]
        private bool matchTipRotation = true;

        [SerializeField, Tooltip("Run solver every LateUpdate automatically.")]
        private bool solveInLateUpdate = true;

        [SerializeField, Tooltip("Draw gizmo debug visuals in the scene view.")]
        private bool drawGizmos = true;

        private bool _hasLastBendDirection;
        private Vector3 _lastBendDirectionLocal;

        public Transform Root
        {
            get => root;
            set
            {
                root = value;
                ClearBendHistory();
            }
        }

        public Transform Mid
        {
            get => mid;
            set
            {
                mid = value;
                ClearBendHistory();
            }
        }

        public Transform Tip
        {
            get => tip;
            set => tip = value;
        }

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public Transform Pole
        {
            get => pole;
            set => pole = value;
        }

        /// <summary>
        /// Backwards-compatible combined weight. Setting it changes both position and rotation weights.
        /// </summary>
        public float Weight
        {
            get => positionWeight;
            set
            {
                float clampedValue = IKMath.ClampWeight(value);
                positionWeight = clampedValue;
                rotationWeight = clampedValue;
            }
        }

        public float PositionWeight
        {
            get => positionWeight;
            set => positionWeight = IKMath.ClampWeight(value);
        }

        public float RotationWeight
        {
            get => rotationWeight;
            set => rotationWeight = IKMath.ClampWeight(value);
        }

        public float PoleWeight
        {
            get => poleWeight;
            set => poleWeight = IKMath.ClampWeight(value);
        }

        /// <summary>
        /// Fraction of total chain length used to soften the approach to maximum reach.
        /// </summary>
        public float SoftReach
        {
            get => softReach;
            set => softReach = Mathf.Clamp(value, 0f, 0.5f);
        }

        public bool MatchTipRotation
        {
            get => matchTipRotation;
            set => matchTipRotation = value;
        }

        public bool SolveInLateUpdate
        {
            get => solveInLateUpdate;
            set => solveInLateUpdate = value;
        }

        public bool DrawGizmos
        {
            get => drawGizmos;
            set => drawGizmos = value;
        }

        /// <summary>
        /// True when the most recent target distance was inside the chain's geometric reach range.
        /// </summary>
        public bool IsTargetReachable { get; private set; }

        /// <summary>
        /// World-space distance from the tip to the target after the most recent solve attempt.
        /// </summary>
        public float ReachError { get; private set; } = float.PositiveInfinity;

        /// <summary>
        /// True when the assigned transforms form a usable two-bone hierarchy with non-zero lengths.
        /// </summary>
        public bool HasValidChain => TryGetChainLengths(out _, out _);

        public void ClearBendHistory()
        {
            _hasLastBendDirection = false;
            _lastBendDirectionLocal = Vector3.zero;
        }

        public void Solve()
        {
            if (target == null || !TryGetChainLengths(out float upperLength, out float lowerLength))
            {
                IsTargetReachable = false;
                ReachError = float.PositiveInfinity;
                return;
            }

            Vector3 rootPosition = root.position;
            Vector3 targetOffset = target.position - rootPosition;
            float targetDistance = targetOffset.magnitude;
            float minimumReach = Mathf.Abs(upperLength - lowerLength);
            float maximumReach = upperLength + lowerLength;
            float reachEpsilon = Mathf.Max(LengthEpsilon, Mathf.Min(upperLength, lowerLength) * 0.0001f);

            if (!IsFinite(targetDistance) || !IKMath.IsFinite(target.position) || !IKMath.IsFinite(target.rotation))
            {
                IsTargetReachable = false;
                ReachError = float.PositiveInfinity;
                return;
            }

            IsTargetReachable = targetDistance >= minimumReach - reachEpsilon &&
                                targetDistance <= maximumReach + reachEpsilon;
            ReachError = Vector3.Distance(tip.position, target.position);

            float solvePositionWeight = IKMath.ClampWeight(positionWeight);
            float solveRotationWeight = matchTipRotation ? IKMath.ClampWeight(rotationWeight) : 0f;
            if (solvePositionWeight <= 0f && solveRotationWeight <= 0f)
            {
                return;
            }

            if (solvePositionWeight > 0f)
            {
                Vector3 aimDirection = ResolveAimDirection(targetOffset);
                float solveDistance = ResolveSolveDistance(
                    targetDistance,
                    minimumReach,
                    maximumReach,
                    reachEpsilon);
                Vector3 bendDirection = ResolveBendDirection(aimDirection, rootPosition);
                Vector3 solvedTipPosition = rootPosition + aimDirection * solveDistance;

                float distanceAlongAim =
                    ((upperLength * upperLength) - (lowerLength * lowerLength) + (solveDistance * solveDistance)) /
                    (2f * solveDistance);
                float bendHeightSquared = Mathf.Max(0f, (upperLength * upperLength) - (distanceAlongAim * distanceAlongAim));
                float bendHeight = Mathf.Sqrt(bendHeightSquared);
                Vector3 solvedMidPosition = rootPosition +
                                            (aimDirection * distanceAlongAim) +
                                            (bendDirection * bendHeight);

                if (IKMath.IsFinite(solvedMidPosition) && IKMath.IsFinite(solvedTipPosition))
                {
                    CacheBendDirection(bendDirection);
                    ApplyPositionSolve(solvedMidPosition, solvedTipPosition, solvePositionWeight);
                }
            }

            if (solveRotationWeight > 0f)
            {
                tip.rotation = Quaternion.Slerp(tip.rotation, target.rotation, solveRotationWeight);
            }

            ReachError = Vector3.Distance(tip.position, target.position);
            if (!IsFinite(ReachError) || !IKMath.IsFinite(root.rotation) ||
                !IKMath.IsFinite(mid.rotation) || !IKMath.IsFinite(tip.rotation))
            {
                ReachError = float.PositiveInfinity;
            }
        }

        private bool TryGetChainLengths(out float upperLength, out float lowerLength)
        {
            upperLength = 0f;
            lowerLength = 0f;

            if (root == null || mid == null || tip == null ||
                root == mid || root == tip || mid == tip ||
                !mid.IsChildOf(root) || !tip.IsChildOf(mid) ||
                !IKMath.IsFinite(root.position) || !IKMath.IsFinite(mid.position) || !IKMath.IsFinite(tip.position))
            {
                return false;
            }

            upperLength = Vector3.Distance(root.position, mid.position);
            lowerLength = Vector3.Distance(mid.position, tip.position);
            return IsFinite(upperLength) && IsFinite(lowerLength) &&
                   upperLength > LengthEpsilon && lowerLength > LengthEpsilon;
        }

        private Vector3 ResolveAimDirection(Vector3 targetOffset)
        {
            if (targetOffset.sqrMagnitude > LengthEpsilon * LengthEpsilon)
            {
                return targetOffset.normalized;
            }

            Vector3 animatedDirection = tip.position - root.position;
            if (animatedDirection.sqrMagnitude > LengthEpsilon * LengthEpsilon)
            {
                return animatedDirection.normalized;
            }

            animatedDirection = mid.position - root.position;
            if (animatedDirection.sqrMagnitude > LengthEpsilon * LengthEpsilon)
            {
                return animatedDirection.normalized;
            }

            return root.forward;
        }

        private float ResolveSolveDistance(
            float targetDistance,
            float minimumReach,
            float maximumReach,
            float reachEpsilon)
        {
            float minimumSolveDistance = Mathf.Min(maximumReach, minimumReach + reachEpsilon);
            float maximumSolveDistance = Mathf.Max(minimumSolveDistance, maximumReach - reachEpsilon);
            float solveDistance = Mathf.Clamp(targetDistance, minimumSolveDistance, maximumSolveDistance);

            float softDistance = maximumReach * Mathf.Clamp(softReach, 0f, 0.5f);
            softDistance = Mathf.Min(softDistance, Mathf.Max(0f, maximumSolveDistance - minimumSolveDistance));
            if (softDistance <= reachEpsilon)
            {
                return solveDistance;
            }

            float softStart = maximumSolveDistance - softDistance;
            if (targetDistance <= softStart)
            {
                return solveDistance;
            }

            float softenedDistance = softStart + softDistance *
                (1f - Mathf.Exp(-(targetDistance - softStart) / softDistance));
            return Mathf.Clamp(softenedDistance, minimumSolveDistance, maximumSolveDistance);
        }

        private Vector3 ResolveBendDirection(Vector3 aimDirection, Vector3 rootPosition)
        {
            Vector3 animatedBend = IKMath.ProjectOntoPlane(mid.position - rootPosition, aimDirection);
            Vector3 baseBend = NormalizeOrZero(animatedBend);

            if (baseBend == Vector3.zero && _hasLastBendDirection)
            {
                Vector3 rememberedWorldDirection = root.TransformDirection(_lastBendDirectionLocal);
                baseBend = NormalizeOrZero(IKMath.ProjectOntoPlane(rememberedWorldDirection, aimDirection));
            }

            if (baseBend == Vector3.zero)
            {
                baseBend = FindPerpendicular(aimDirection);
            }

            if (pole == null || poleWeight <= 0f || !IKMath.IsFinite(pole.position))
            {
                return baseBend;
            }

            Vector3 poleBend = NormalizeOrZero(IKMath.ProjectOntoPlane(pole.position - rootPosition, aimDirection));
            if (poleBend == Vector3.zero)
            {
                return baseBend;
            }

            float influence = IKMath.ClampWeight(poleWeight);
            Vector3 blendedBend = Vector3.Lerp(baseBend, poleBend, influence);
            if (blendedBend.sqrMagnitude <= LengthEpsilon * LengthEpsilon)
            {
                return influence >= 0.5f ? poleBend : baseBend;
            }

            return blendedBend.normalized;
        }

        private Vector3 FindPerpendicular(Vector3 aimDirection)
        {
            Vector3 perpendicular = IKMath.ProjectOntoPlane(root.up, aimDirection);
            if (perpendicular.sqrMagnitude <= LengthEpsilon * LengthEpsilon)
            {
                perpendicular = IKMath.ProjectOntoPlane(root.right, aimDirection);
            }

            if (perpendicular.sqrMagnitude <= LengthEpsilon * LengthEpsilon)
            {
                perpendicular = Vector3.Cross(aimDirection, Vector3.forward);
            }

            if (perpendicular.sqrMagnitude <= LengthEpsilon * LengthEpsilon)
            {
                perpendicular = Vector3.Cross(aimDirection, Vector3.up);
            }

            return perpendicular.normalized;
        }

        private void ApplyPositionSolve(Vector3 solvedMidPosition, Vector3 solvedTipPosition, float solveWeight)
        {
            Vector3 currentUpperDirection = mid.position - root.position;
            Vector3 solvedUpperDirection = solvedMidPosition - root.position;
            if (currentUpperDirection.sqrMagnitude > LengthEpsilon * LengthEpsilon &&
                solvedUpperDirection.sqrMagnitude > LengthEpsilon * LengthEpsilon)
            {
                Quaternion solvedRootRotation =
                    Quaternion.FromToRotation(currentUpperDirection, solvedUpperDirection) * root.rotation;
                root.rotation = Quaternion.Slerp(root.rotation, solvedRootRotation, solveWeight);
            }

            Vector3 currentLowerDirection = tip.position - mid.position;
            Vector3 solvedLowerDirection = solvedTipPosition - mid.position;
            if (currentLowerDirection.sqrMagnitude > LengthEpsilon * LengthEpsilon &&
                solvedLowerDirection.sqrMagnitude > LengthEpsilon * LengthEpsilon)
            {
                Quaternion solvedMidRotation =
                    Quaternion.FromToRotation(currentLowerDirection, solvedLowerDirection) * mid.rotation;
                mid.rotation = Quaternion.Slerp(mid.rotation, solvedMidRotation, solveWeight);
            }
        }

        private void CacheBendDirection(Vector3 worldBendDirection)
        {
            Vector3 localDirection = root.InverseTransformDirection(worldBendDirection);
            if (localDirection.sqrMagnitude <= LengthEpsilon * LengthEpsilon)
            {
                return;
            }

            _lastBendDirectionLocal = localDirection.normalized;
            _hasLastBendDirection = true;
        }

        private static Vector3 NormalizeOrZero(Vector3 value)
        {
            return value.sqrMagnitude > LengthEpsilon * LengthEpsilon ? value.normalized : Vector3.zero;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void OnValidate()
        {
            positionWeight = IKMath.ClampWeight(positionWeight);
            rotationWeight = IKMath.ClampWeight(rotationWeight);
            poleWeight = IKMath.ClampWeight(poleWeight);
            softReach = Mathf.Clamp(softReach, 0f, 0.5f);
        }

        private void LateUpdate()
        {
            if (solveInLateUpdate)
            {
                Solve();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || root == null || mid == null || tip == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(root.position, mid.position);
            Gizmos.DrawLine(mid.position, tip.position);

            float upperLength = Vector3.Distance(root.position, mid.position);
            float lowerLength = Vector3.Distance(mid.position, tip.position);
            float maximumReach = upperLength + lowerLength;
            float minimumReach = Mathf.Abs(upperLength - lowerLength);

            Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            Gizmos.DrawWireSphere(root.position, maximumReach);
            if (minimumReach > LengthEpsilon)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
                Gizmos.DrawWireSphere(root.position, minimumReach);
            }

            if (target != null)
            {
                Gizmos.color = IsTargetReachable ? Color.green : Color.red;
                Gizmos.DrawLine(tip.position, target.position);
                Gizmos.DrawWireSphere(target.position, 0.04f);
            }

            if (pole != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(mid.position, pole.position);
                Gizmos.DrawWireSphere(pole.position, 0.03f);
            }
        }
    }
}
