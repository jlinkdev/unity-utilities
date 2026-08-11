using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Produces one or more base strands between resolved endpoints.</summary>
    public abstract class BeamPathProvider : MonoBehaviour
    {
        public abstract void BuildPath(in BeamPathContext context, BeamPathBuffer output);
    }
}
