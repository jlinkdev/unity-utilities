using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals
{
    [CreateAssetMenu(fileName = "Portal Render Settings", menuName = "jlinkdev/Portals/Render Settings")]
    public sealed class PortalRenderSettings : ScriptableObject
    {
        [SerializeField, Range(0, 8)] private int recursionLimit = 3;
        [SerializeField, Range(0.25f, 1f)] private float renderScale = 0.75f;
        [SerializeField, Min(0.001f)] private float nearClipOffset = 0.05f;
        [SerializeField] private LayerMask cullingMask = ~0;
        [SerializeField] private bool renderShadows = true;
        [SerializeField] private bool renderInSceneView;
        [SerializeField] private bool useHdr = true;

        public int RecursionLimit => recursionLimit;
        public float RenderScale => renderScale;
        public float NearClipOffset => nearClipOffset;
        public LayerMask CullingMask => cullingMask;
        public bool RenderShadows => renderShadows;
        public bool RenderInSceneView => renderInSceneView;
        public bool UseHdr => useHdr;

        internal static int DefaultRecursionLimit => 3;
        internal static float DefaultRenderScale => 0.75f;
        internal static float DefaultNearClipOffset => 0.05f;
    }
}
