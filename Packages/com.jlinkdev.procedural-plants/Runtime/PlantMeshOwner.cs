using UnityEngine;

namespace JLink.Plants
{
    [AddComponentMenu("")]
    public sealed class PlantMeshOwner : MonoBehaviour
    {
        Mesh owned;
        internal void Own(Mesh mesh) => owned=mesh;
        void OnDestroy()
        {
            if (!owned) return;
            if (Application.isPlaying) Destroy(owned); else DestroyImmediate(owned);
        }
    }
}
