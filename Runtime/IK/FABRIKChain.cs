using UnityEngine;

namespace jlinkdev.UnityUtilities.IK
{
    public sealed class FABRIKChain : MonoBehaviour
    {
        [SerializeField, Tooltip("Joint chain from root to tip in order.")]
        private Transform[] joints;

        [SerializeField, Tooltip("Transform the chain tip should reach.")]
        private Transform target;

        [SerializeField, Tooltip("Optional pole transform to bias bend direction.")]
        private Transform pole;

        [SerializeField, Tooltip("Number of solver iterations per update.")]
        private int iterations = 8;

        [SerializeField, Tooltip("Stop early when tip is within this distance to target.")]
        private float tolerance = 0.01f;

        [SerializeField, Tooltip("Keep the root joint fixed in world space.")]
        private bool lockRoot = true;

        [SerializeField, Tooltip("Blend from 0 (disabled) to 1 (full IK).")]
        private float weight = 1f;

        [SerializeField, Tooltip("Run solver every LateUpdate automatically.")]
        private bool solveInLateUpdate = true;

        [SerializeField, Tooltip("Draw gizmo debug visuals in the scene view.")]
        private bool drawGizmos = true;

        public Transform[] Joints
        {
            get => joints;
            set => joints = value;
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

        public int Iterations
        {
            get => iterations;
            set => iterations = Mathf.Max(1, value);
        }

        public float Tolerance
        {
            get => tolerance;
            set => tolerance = Mathf.Max(0f, value);
        }

        public bool LockRoot
        {
            get => lockRoot;
            set => lockRoot = value;
        }

        public float Weight
        {
            get => weight;
            set => weight = IKMath.ClampWeight(value);
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
            if (joints == null || joints.Length < 2 || target == null) return;

            float solveWeight = IKMath.ClampWeight(weight);
            if (solveWeight <= 0f) return;

            int count = joints.Length;
            Vector3[] points = new Vector3[count];
            float[] lengths = new float[count - 1];
            for (int i = 0; i < count; i++) points[i] = joints[i].position;
            for (int i = 0; i < count - 1; i++) lengths[i] = Vector3.Distance(points[i], points[i + 1]);

            Vector3 rootStart = points[0];
            float totalLen = 0f;
            for (int i = 0; i < lengths.Length; i++) totalLen += lengths[i];
            Vector3 targetPos = target.position;

            if (Vector3.Distance(rootStart, targetPos) > totalLen)
            {
                Vector3 dir = (targetPos - rootStart).normalized;
                for (int i = 1; i < count; i++) points[i] = points[i - 1] + (dir * lengths[i - 1]);
            }
            else
            {
                for (int iteration = 0; iteration < iterations; iteration++)
                {
                    points[count - 1] = targetPos;
                    for (int i = count - 2; i >= 0; i--)
                    {
                        Vector3 dir = (points[i] - points[i + 1]).normalized;
                        points[i] = points[i + 1] + (dir * lengths[i]);
                    }

                    if (lockRoot) points[0] = rootStart;
                    for (int i = 1; i < count; i++)
                    {
                        Vector3 dir = (points[i] - points[i - 1]).normalized;
                        points[i] = points[i - 1] + (dir * lengths[i - 1]);
                    }

                    if ((points[count - 1] - targetPos).sqrMagnitude < tolerance * tolerance) break;
                }
            }

            if (pole != null)
            {
                for (int i = 1; i < count - 1; i++)
                {
                    Plane plane = new Plane(points[i + 1] - points[i - 1], points[i - 1]);
                    Vector3 projectedPole = plane.ClosestPointOnPlane(pole.position);
                    Vector3 projectedJoint = plane.ClosestPointOnPlane(points[i]);
                    Vector3 fromJoint = projectedJoint - points[i - 1];
                    Vector3 fromPole = projectedPole - points[i - 1];
                    float angle = Vector3.SignedAngle(fromJoint, fromPole, plane.normal);
                    points[i] = Quaternion.AngleAxis(angle, plane.normal) * (points[i] - points[i - 1]) + points[i - 1];
                }
            }

            for (int i = 0; i < count - 1; i++)
            {
                Quaternion solved = Quaternion.FromToRotation(joints[i + 1].position - joints[i].position, points[i + 1] - points[i]) * joints[i].rotation;
                joints[i].rotation = Quaternion.Slerp(joints[i].rotation, solved, solveWeight);
            }
        }

        private void LateUpdate() { if (solveInLateUpdate) Solve(); }

        private void OnDrawGizmos()
        {
            DrawDebugGizmos(0.25f);
        }

        private void OnDrawGizmosSelected()
        {
            DrawDebugGizmos(1f);
        }

        private void DrawDebugGizmos(float alphaScale)
        {
            if (!drawGizmos || joints == null || joints.Length < 2) return;
            Gizmos.color = WithAlpha(Color.cyan, 1f * alphaScale);
            for (int i = 0; i < joints.Length - 1; i++) if (joints[i] != null && joints[i + 1] != null) Gizmos.DrawLine(joints[i].position, joints[i + 1].position);
            if (target != null && joints[joints.Length - 1] != null)
            {
                Gizmos.color = WithAlpha(Color.green, 1f * alphaScale);
                Gizmos.DrawLine(joints[joints.Length - 1].position, target.position);
                Gizmos.DrawWireSphere(target.position, 0.04f);
            }

            if (joints[0] != null)
            {
                Vector3[] p = new Vector3[joints.Length];
                for (int i = 0; i < joints.Length; i++) p[i] = joints[i] != null ? joints[i].position : joints[0].position;
                Gizmos.color = WithAlpha(Color.yellow, 0.6f * alphaScale);
                Gizmos.DrawWireSphere(joints[0].position, IKMath.SumLengths(p));
            }

            if (pole != null)
            {
                Gizmos.color = WithAlpha(Color.magenta, 1f * alphaScale);
                Gizmos.DrawWireSphere(pole.position, 0.03f);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}
