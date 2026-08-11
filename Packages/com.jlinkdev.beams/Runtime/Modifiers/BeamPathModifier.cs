using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Transforms strands after a path provider has built them.</summary>
    public abstract class BeamPathModifier : MonoBehaviour
    {
        public abstract void Modify(in BeamPathContext context, BeamPathBuffer paths);
    }
}
