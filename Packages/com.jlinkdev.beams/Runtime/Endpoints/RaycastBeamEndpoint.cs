using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Resolves an endpoint from a ray or sphere cast and optionally remains valid at maximum range.</summary>
    [ExecuteAlways]
    [AddComponentMenu("jlinkdev/Beams/Endpoints/Raycast Beam Endpoint")]
    public sealed class RaycastBeamEndpoint : BeamEndpointProvider
    {
        [SerializeField] private Transform origin;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private Vector3 localDirection = Vector3.forward;
        [SerializeField, Min(0f)] private float maximumDistance = 25f;
        [SerializeField, Min(0f)] private float radius;
        [SerializeField] private LayerMask layerMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal;
        [SerializeField] private bool validWhenUnobstructed = true;
        [SerializeField] private bool useSurfaceNormalAsForward = true;

        public RaycastHit LastHit { get; private set; }
        public bool HasHit { get; private set; }

        public Transform Origin
        {
            get => origin;
            set => origin = value;
        }

        public float MaximumDistance
        {
            get => maximumDistance;
            set => maximumDistance = Mathf.Max(0f, value);
        }

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        public LayerMask LayerMask
        {
            get => layerMask;
            set => layerMask = value;
        }

        public override bool TryGetEndpoint(out BeamEndpoint endpoint)
        {
            Transform resolvedOrigin = origin != null ? origin : transform;
            Vector3 worldOrigin = resolvedOrigin.TransformPoint(localOffset);
            Vector3 direction = resolvedOrigin.TransformDirection(localDirection);
            if (direction.sqrMagnitude < 0.000001f)
                direction = resolvedOrigin.forward;
            direction.Normalize();

            HasHit = radius > 0f
                ? Physics.SphereCast(worldOrigin, radius, direction, out RaycastHit hit, maximumDistance, layerMask, triggerInteraction)
                : Physics.Raycast(worldOrigin, direction, out hit, maximumDistance, layerMask, triggerInteraction);
            LastHit = hit;

            if (HasHit)
            {
                Vector3 forward = useSurfaceNormalAsForward ? hit.normal : -direction;
                endpoint = new BeamEndpoint(hit.point, forward, hit.normal, hit.collider, hit.transform);
                return true;
            }

            endpoint = new BeamEndpoint(worldOrigin + direction * maximumDistance, -direction, resolvedOrigin);
            return validWhenUnobstructed;
        }

        private void OnValidate()
        {
            maximumDistance = Mathf.Max(0f, maximumDistance);
            radius = Mathf.Max(0f, radius);
            if (localDirection.sqrMagnitude < 0.000001f)
                localDirection = Vector3.forward;
        }

        private void OnDrawGizmosSelected()
        {
            Transform resolvedOrigin = origin != null ? origin : transform;
            Vector3 worldOrigin = resolvedOrigin.TransformPoint(localOffset);
            Vector3 direction = resolvedOrigin.TransformDirection(localDirection).normalized;
            Gizmos.color = HasHit ? new Color(0.2f, 1f, 0.55f, 0.7f) : new Color(0.2f, 0.75f, 1f, 0.6f);
            Gizmos.DrawLine(worldOrigin, HasHit ? LastHit.point : worldOrigin + direction * maximumDistance);
            if (HasHit)
                Gizmos.DrawWireSphere(LastHit.point, Mathf.Max(0.025f, radius));
        }
    }
}
