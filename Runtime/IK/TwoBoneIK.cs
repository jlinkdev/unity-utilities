using UnityEngine;

namespace jlinkdev.UnityUtilities.IK
{
    public sealed class TwoBoneIK : MonoBehaviour
    {
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

        [SerializeField, Tooltip("Blend from 0 (disabled) to 1 (full IK).")]
        private float weight = 1f;

        [SerializeField, Tooltip("When enabled, tip rotation will match target rotation.")]
        private bool matchTipRotation = true;

        [SerializeField, Tooltip("Run solver every LateUpdate automatically.")]
        private bool solveInLateUpdate = true;

        [SerializeField, Tooltip("Draw gizmo debug visuals in the scene view.")]
        private bool drawGizmos = true;

        public Transform Root
        {
            get => root;
            set => root = value;
        }

        public Transform Mid
        {
            get => mid;
            set => mid = value;
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

        public float Weight
        {
            get => weight;
            set => weight = IKMath.ClampWeight(value);
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

        public void Solve()
        {
            if (root == null || mid == null || tip == null || target == null)
            {
                return;
            }

            float solveWeight = IKMath.ClampWeight(weight);
            if (solveWeight <= 0f)
            {
                return;
            }

            Vector3 rootPos = root.position;
            Vector3 midPos = mid.position;
            Vector3 tipPos = tip.position;

            float upperLen = Vector3.Distance(rootPos, midPos);
            float lowerLen = Vector3.Distance(midPos, tipPos);
            Vector3 toTarget = target.position - rootPos;
            float targetDist = Mathf.Max(0.0001f, toTarget.magnitude);
            float maxReach = upperLen + lowerLen;
            float clampedDist = Mathf.Min(targetDist, maxReach - 0.0001f);

            Vector3 dirToTarget = toTarget.normalized;
            Vector3 bendNormal = Vector3.Cross(midPos - rootPos, tipPos - midPos).normalized;
            if (bendNormal.sqrMagnitude < 0.0001f)
            {
                bendNormal = Vector3.up;
            }

            if (pole != null)
            {
                Vector3 poleDir = IKMath.ProjectOntoPlane(pole.position - rootPos, dirToTarget).normalized;
                if (poleDir.sqrMagnitude > 0.0001f)
                {
                    bendNormal = Vector3.Cross(dirToTarget, poleDir).normalized;
                }
            }

            float cosAngle = ((upperLen * upperLen) + (clampedDist * clampedDist) - (lowerLen * lowerLen)) / (2f * upperLen * clampedDist);
            float angle = Mathf.Acos(Mathf.Clamp(cosAngle, -1f, 1f));

            Vector3 bendDir = Quaternion.AngleAxis(Mathf.Rad2Deg * angle, bendNormal) * dirToTarget;
            Vector3 solvedMidPos = rootPos + (bendDir * upperLen);

            Quaternion rootRotation = Quaternion.FromToRotation(midPos - rootPos, solvedMidPos - rootPos) * root.rotation;
            root.rotation = Quaternion.Slerp(root.rotation, rootRotation, solveWeight);

            midPos = mid.position;
            tipPos = tip.position;
            Quaternion midRotation = Quaternion.FromToRotation(tipPos - midPos, target.position - midPos) * mid.rotation;
            mid.rotation = Quaternion.Slerp(mid.rotation, midRotation, solveWeight);

            if (matchTipRotation)
            {
                tip.rotation = Quaternion.Slerp(tip.rotation, target.rotation, solveWeight);
            }
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

            float reach = Vector3.Distance(root.position, mid.position) + Vector3.Distance(mid.position, tip.position);
            Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            Gizmos.DrawWireSphere(root.position, reach);

            if (target != null)
            {
                Gizmos.color = Color.green;
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
