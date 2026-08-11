using UnityEngine;

namespace jlinkdev.UnityUtilities.Beams
{
    /// <summary>Resolves a beam endpoint without imposing a targeting system.</summary>
    public abstract class BeamEndpointProvider : MonoBehaviour
    {
        public abstract bool TryGetEndpoint(out BeamEndpoint endpoint);
    }
}
