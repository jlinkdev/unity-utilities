using UnityEngine;

namespace jlinkdev.UnityUtilities.IK
{
    public sealed class RotationLimit : MonoBehaviour
    {
        public enum LimitMode { Hinge, Cone }

        [SerializeField, Tooltip("Limit mode: simple hinge or simple cone.")]
        private LimitMode mode = LimitMode.Cone;
        [SerializeField, Tooltip("Local axis used by the limit.")]
        private Vector3 localAxis = Vector3.forward;
        [SerializeField, Tooltip("Minimum hinge angle in degrees.")]
        private float hingeMin = -45f;
        [SerializeField, Tooltip("Maximum hinge angle in degrees.")]
        private float hingeMax = 45f;
        [SerializeField, Tooltip("Cone half-angle in degrees.")]
        private float coneAngle = 45f;
        [SerializeField, Tooltip("Draw gizmo debug visuals in the scene view.")]
        private bool drawGizmos = true;

        private Quaternion _initialLocalRotation;

        private void Awake() { _initialLocalRotation = transform.localRotation; }

        public void ApplyLimit()
        {
            Vector3 axis = localAxis.normalized;
            if (axis.sqrMagnitude <= 0.00001f) return;

            Quaternion relative = Quaternion.Inverse(_initialLocalRotation) * transform.localRotation;
            if (mode == LimitMode.Hinge)
            {
                relative.ToAngleAxis(out float angle, out Vector3 outAxis);
                angle = Mathf.DeltaAngle(0f, angle * Mathf.Sign(Vector3.Dot(outAxis, axis)));
                angle = Mathf.Clamp(angle, hingeMin, hingeMax);
                transform.localRotation = _initialLocalRotation * Quaternion.AngleAxis(angle, axis);
            }
            else
            {
                Vector3 dir = relative * axis;
                float angle = Vector3.Angle(axis, dir);
                if (angle > coneAngle)
                {
                    Vector3 cross = Vector3.Cross(axis, dir);
                    if (cross.sqrMagnitude > 0.00001f)
                    {
                        Quaternion clamp = Quaternion.AngleAxis(coneAngle, cross.normalized) * Quaternion.FromToRotation(cross, cross);
                        transform.localRotation = _initialLocalRotation * clamp;
                    }
                }
            }
        }

        private void LateUpdate() { ApplyLimit(); }

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
            if (!drawGizmos) return;
            Vector3 worldAxis = transform.TransformDirection(localAxis.normalized);
            if (mode == LimitMode.Hinge)
            {
                Gizmos.color = WithAlpha(Color.yellow, 1f * alphaScale);
                Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(hingeMin, worldAxis) * transform.up * 0.5f);
                Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(hingeMax, worldAxis) * transform.up * 0.5f);
            }
            else
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f * alphaScale);
                Gizmos.DrawRay(transform.position, worldAxis * 0.5f);
                Gizmos.DrawWireSphere(transform.position + worldAxis * 0.5f, Mathf.Sin(coneAngle * Mathf.Deg2Rad) * 0.5f);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}
