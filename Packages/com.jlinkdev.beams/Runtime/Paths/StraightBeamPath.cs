using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    [AddComponentMenu("jlinkdev/Beams/Paths/Straight Beam Path")]
    public sealed class StraightBeamPath : BeamPathProvider
    {
        public override void BuildPath(in BeamPathContext context, BeamPathBuffer output)
        {
            BeamStrand strand = output.AddStrand(seed: context.Seed);
            strand.Add(context.Source.Position);
            strand.Add(context.Target.Position);
            BeamPathUtility.RecalculateMetrics(strand, context.Source.Forward);
        }
    }
}
