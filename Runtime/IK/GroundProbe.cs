using UnityEngine;

namespace jlinkdev.UnityUtilities.IK
{
    public sealed class GroundProbe : MonoBehaviour
    {
        [SerializeField, Tooltip("Origin transform used to cast toward the ground.")]
        private Transform rayOrigin;
        [SerializeField, Tooltip("Maximum cast distance.")]
        private float rayDistance = 2f;
        [SerializeField, Tooltip("Layer mask used for ground hits.")]
        private LayerMask groundMask = ~0;
        [SerializeField, Tooltip("World-space offset applied from hit point along normal.")]
        private float surfaceOffset = 0.05f;
        [SerializeField, Tooltip("Align this transform to the hit surface normal.")]
        private bool alignToNormal = true;
        [SerializeField, Tooltip("Position smoothing speed (0 = no smoothing).")]
        private float positionSmooth = 15f;
        [SerializeField, Tooltip("Rotation smoothing speed (0 = no smoothing).")]
        private float rotationSmooth = 15f;
        [SerializeField, Tooltip("Draw gizmo debug visuals in the scene view.")]
        private bool drawGizmos = true;

        private bool _hasHit;
        private RaycastHit _hit;

        public Transform RayOrigin
        {
            get => rayOrigin;
            set => rayOrigin = value;
        }

        public float RayDistance
        {
            get => rayDistance;
            set => rayDistance = Mathf.Max(0f, value);
        }

        public LayerMask GroundMask
        {
            get => groundMask;
            set => groundMask = value;
        }

        public float SurfaceOffset
        {
            get => surfaceOffset;
            set => surfaceOffset = value;
        }

        public bool AlignToNormal
        {
            get => alignToNormal;
            set => alignToNormal = value;
        }

        public float PositionSmooth
        {
            get => positionSmooth;
            set => positionSmooth = Mathf.Max(0f, value);
        }

        public float RotationSmooth
        {
            get => rotationSmooth;
            set => rotationSmooth = Mathf.Max(0f, value);
        }

        public bool DrawGizmos
        {
            get => drawGizmos;
            set => drawGizmos = value;
        }

        public bool HasHit => _hasHit;
        public RaycastHit CurrentHit => _hit;

        private void LateUpdate()
        {
            if (rayOrigin == null) return;
            if (Physics.Raycast(rayOrigin.position, Vector3.down, out _hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                _hasHit = true;
                Vector3 targetPos = _hit.point + (_hit.normal * surfaceOffset);
                if (positionSmooth > 0f)
                {
                    transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * positionSmooth);
                }
                else
                {
                    transform.position = targetPos;
                }

                if (alignToNormal)
                {
                    Quaternion targetRot = IKMath.SafeLookRotation(Vector3.ProjectOnPlane(rayOrigin.forward, _hit.normal), _hit.normal);
                    if (rotationSmooth > 0f) transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmooth);
                    else transform.rotation = targetRot;
                }
            }
            else
            {
                _hasHit = false;
            }
        }

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
            if (!drawGizmos || rayOrigin == null) return;
            Gizmos.color = WithAlpha(Color.white, 1f * alphaScale);
            Gizmos.DrawLine(rayOrigin.position, rayOrigin.position + Vector3.down * rayDistance);
            if (_hasHit)
            {
                Gizmos.color = WithAlpha(Color.green, 1f * alphaScale);
                Gizmos.DrawWireSphere(_hit.point, 0.03f);
                Gizmos.DrawRay(_hit.point, _hit.normal * 0.3f);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}
