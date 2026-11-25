using UnityEngine;

namespace jlinkdev.ProceduralNebulae
{
    [DisallowMultipleComponent]
    public sealed class NebulaStar : MonoBehaviour
    {
        [ColorUsage(false, true)] public Color color = new Color(1f, .84f, .68f);
        [Min(0)] public float intensity = 2.6f;
        [Tooltip("Radius of the visible stellar surface in world units."), Min(.01f)]
        public float radius = .58f;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
