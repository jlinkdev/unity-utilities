using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>A sampled point along a strand, expressed in world space.</summary>
    public struct BeamPoint
    {
        public BeamPoint(Vector3 position)
        {
            Position = position;
            Tangent = Vector3.forward;
            Normal = Vector3.up;
            Distance = 0f;
            NormalizedDistance = 0f;
        }

        public Vector3 Position;
        public Vector3 Tangent;
        public Vector3 Normal;
        public float Distance;
        public float NormalizedDistance;
    }
}
