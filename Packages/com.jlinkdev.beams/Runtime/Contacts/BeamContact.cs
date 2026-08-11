using System;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Neutral physics information for one collider intersected by one beam strand.</summary>
    [Serializable]
    public struct BeamContact
    {
        public BeamContact(
            Collider collider,
            Vector3 position,
            Vector3 normal,
            int strandIndex,
            int segmentIndex,
            float distanceAlongStrand)
        {
            Collider = collider;
            Position = position;
            Normal = normal;
            StrandIndex = strandIndex;
            SegmentIndex = segmentIndex;
            DistanceAlongStrand = distanceAlongStrand;
        }

        public Collider Collider;
        public Vector3 Position;
        public Vector3 Normal;
        public int StrandIndex;
        public int SegmentIndex;
        public float DistanceAlongStrand;
    }
}
