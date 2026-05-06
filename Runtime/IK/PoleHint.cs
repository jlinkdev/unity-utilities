using UnityEngine;

namespace jlinkdev.UnityUtilities.IK
{
    public sealed class PoleHint : MonoBehaviour
    {
        [SerializeField, Tooltip("Draw gizmo in scene view for this pole hint marker.")]
        private bool drawGizmo = true;

        private void OnDrawGizmos()
        {
            if (!drawGizmo) return;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.06f);
        }
    }
}
