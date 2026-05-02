using UnityEngine;

namespace jlinkdev.UnityUtilities.IK
{
    public sealed class IKTarget : MonoBehaviour
    {
        [SerializeField, Tooltip("Draw gizmo in scene view for this target marker.")]
        private bool drawGizmo = true;

        private void OnDrawGizmos()
        {
            if (!drawGizmo) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.05f);
        }
    }
}
