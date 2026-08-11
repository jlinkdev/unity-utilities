using System;
using System.Collections.Generic;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Reusable storage for the active strands produced by a beam.</summary>
    public sealed class BeamPathBuffer
    {
        private readonly List<BeamStrand> strands = new List<BeamStrand>(4);
        private int count;

        public int Count => count;

        public BeamStrand this[int index]
        {
            get
            {
                if ((uint)index >= (uint)count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return strands[index];
            }
        }

        public BeamStrand AddStrand(int parentStrandIndex = -1, float parentPosition = 0f, int seed = 0)
        {
            BeamStrand strand;
            if (count < strands.Count)
            {
                strand = strands[count];
                strand.Clear();
            }
            else
            {
                strand = new BeamStrand();
                strands.Add(strand);
            }

            strand.ParentStrandIndex = parentStrandIndex;
            strand.ParentPosition = parentPosition;
            strand.Seed = seed;
            strand.BranchDepth = parentStrandIndex >= 0 && parentStrandIndex < count
                ? strands[parentStrandIndex].BranchDepth + 1
                : 0;
            count++;
            return strand;
        }

        public void Clear()
        {
            for (int i = 0; i < count; i++)
                strands[i].Clear();
            count = 0;
        }
    }
}
