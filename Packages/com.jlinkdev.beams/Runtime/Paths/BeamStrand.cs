using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>An ordered path. Branching beams are represented by multiple strands.</summary>
    public sealed class BeamStrand
    {
        private readonly List<BeamPoint> points = new List<BeamPoint>(16);

        public int ParentStrandIndex { get; internal set; } = -1;
        public float ParentPosition { get; internal set; }
        public int Seed { get; internal set; }
        public int BranchDepth { get; internal set; }
        public int Count => points.Count;
        public float Length => points.Count > 0 ? points[points.Count - 1].Distance : 0f;

        public BeamPoint this[int index]
        {
            get => points[index];
            set => points[index] = value;
        }

        public void Add(Vector3 position)
        {
            points.Add(new BeamPoint(position));
        }

        public void Add(BeamPoint point)
        {
            points.Add(point);
        }

        public void Clear()
        {
            points.Clear();
            ParentStrandIndex = -1;
            ParentPosition = 0f;
            Seed = 0;
            BranchDepth = 0;
        }
    }
}
