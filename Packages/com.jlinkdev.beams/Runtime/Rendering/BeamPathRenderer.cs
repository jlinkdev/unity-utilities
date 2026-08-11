using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Consumes resolved beam strands without owning their generation.</summary>
    public abstract class BeamPathRenderer : MonoBehaviour
    {
        public abstract void Render(BeamPathBuffer paths, in BeamRenderContext context);
        public abstract void Clear();
    }
}
