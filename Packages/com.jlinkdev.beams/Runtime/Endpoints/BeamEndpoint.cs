using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>A resolved point and orientation used by a beam path.</summary>
    public readonly struct BeamEndpoint
    {
        public BeamEndpoint(Vector3 position, Vector3 forward, Transform anchor = null)
            : this(position, forward, Vector3.zero, null, anchor)
        {
        }

        public BeamEndpoint(
            Vector3 position,
            Vector3 forward,
            Vector3 surfaceNormal,
            Collider surfaceCollider,
            Transform anchor = null)
        {
            Position = position;
            Forward = forward.sqrMagnitude > 0.000001f ? forward.normalized : Vector3.forward;
            SurfaceNormal = surfaceNormal.sqrMagnitude > 0.000001f ? surfaceNormal.normalized : Vector3.zero;
            SurfaceCollider = surfaceCollider;
            Anchor = anchor;
        }

        public Vector3 Position { get; }
        public Vector3 Forward { get; }
        public Vector3 SurfaceNormal { get; }
        public Collider SurfaceCollider { get; }
        public Transform Anchor { get; }
        public bool HasSurface => SurfaceCollider != null;
    }
}
