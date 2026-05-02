using UnityEngine;

namespace jlinkdev.UnityUtilities.IK
{
    public sealed class AimIK : MonoBehaviour
    {
        [SerializeField, Tooltip("Transforms to rotate from root to tip in order.")]
        private Transform[] joints;
        [SerializeField, Tooltip("Transform to aim at.")]
        private Transform target;
        [SerializeField, Tooltip("Local axis that should point at the target.")]
        private Vector3 localAimAxis = Vector3.forward;
        [SerializeField, Tooltip("CCD-style iteration count.")]
        private int iterations = 5;
        [SerializeField, Tooltip("Blend from 0 (disabled) to 1 (full IK).")]
        private float weight = 1f;
        [SerializeField, Tooltip("Run solver every LateUpdate automatically.")]
        private bool solveInLateUpdate = true;
        [SerializeField, Tooltip("Draw gizmo debug visuals in the scene view.")]
        private bool drawGizmos = true;

        public void Solve()
        {
            if (joints == null || joints.Length == 0 || target == null) return;
            float solveWeight = IKMath.ClampWeight(weight);
            if (solveWeight <= 0f) return;

            Transform tip = joints[joints.Length - 1];
            for (int step = 0; step < iterations; step++)
            {
                for (int i = joints.Length - 1; i >= 0; i--)
                {
                    Vector3 currentAim = joints[i].TransformDirection(localAimAxis.normalized);
                    Vector3 toTarget = (target.position - tip.position).normalized;
                    if (toTarget.sqrMagnitude <= 0.00001f || currentAim.sqrMagnitude <= 0.00001f) continue;
                    Quaternion delta = Quaternion.FromToRotation(currentAim, toTarget);
                    joints[i].rotation = Quaternion.Slerp(joints[i].rotation, delta * joints[i].rotation, solveWeight);
                }
            }
        }

        private void LateUpdate() { if (solveInLateUpdate) Solve(); }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || joints == null || joints.Length == 0) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < joints.Length - 1; i++) if (joints[i] != null && joints[i + 1] != null) Gizmos.DrawLine(joints[i].position, joints[i + 1].position);
            Transform tip = joints[joints.Length - 1];
            if (tip != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(tip.position, tip.TransformDirection(localAimAxis.normalized) * 0.5f);
            }
            if (target != null && tip != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(tip.position, target.position);
            }
        }
    }
}
