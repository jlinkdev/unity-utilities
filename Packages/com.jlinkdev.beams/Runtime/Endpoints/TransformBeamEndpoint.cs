using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    [ExecuteAlways]
    [AddComponentMenu("jlinkdev/Beams/Endpoints/Transform Beam Endpoint")]
    public sealed class TransformBeamEndpoint : BeamEndpointProvider
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private Vector3 localForward = Vector3.forward;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public Vector3 LocalOffset
        {
            get => localOffset;
            set => localOffset = value;
        }

        public override bool TryGetEndpoint(out BeamEndpoint endpoint)
        {
            Transform resolved = target != null ? target : transform;
            endpoint = new BeamEndpoint(
                resolved.TransformPoint(localOffset),
                resolved.TransformDirection(localForward),
                resolved);
            return resolved.gameObject.activeInHierarchy;
        }

        private void OnValidate()
        {
            if (localForward.sqrMagnitude < 0.000001f)
                localForward = Vector3.forward;
        }
    }
}
