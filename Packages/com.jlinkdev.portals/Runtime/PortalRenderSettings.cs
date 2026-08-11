using UnityEngine;

namespace jlinkdev.UnityUtilities.Portals
{
    [CreateAssetMenu(fileName = "Portal Render Settings", menuName = "jlinkdev/Portals/Render Settings")]
    public sealed class PortalRenderSettings : ScriptableObject
    {
        public const float MinimumNearClipOffset = 0.00001f;

        [SerializeField, Range(0, 8)] private int recursionLimit = 3;
        [SerializeField, Range(0.25f, 1f)] private float renderScale = 0.75f;
        [SerializeField, Min(MinimumNearClipOffset)] private float nearClipOffset = MinimumNearClipOffset;
        [SerializeField] private LayerMask cullingMask = ~0;
        [SerializeField] private bool renderShadows = true;
        [SerializeField] private bool renderInSceneView;
        [SerializeField] private bool useHdr = true;
        [Header("Recursion End")]
        [SerializeField] private Color recursionEndColor = new Color(0.005f, 0.018f, 0.045f, 1f);
        [SerializeField, ColorUsage(true, true)] private Color recursionEndGlowColor = new Color(0.04f, 0.8f, 1.6f, 1f);
        [SerializeField, Range(0f, 4f)] private float recursionEndGlowIntensity = 1.15f;

        public int RecursionLimit => recursionLimit;
        public float RenderScale => renderScale;
        public float NearClipOffset => Mathf.Max(nearClipOffset, MinimumNearClipOffset);
        public LayerMask CullingMask => cullingMask;
        public bool RenderShadows => renderShadows;
        public bool RenderInSceneView => renderInSceneView;
        public bool UseHdr => useHdr;
        public Color RecursionEndColor => recursionEndColor;
        public Color RecursionEndGlowColor => recursionEndGlowColor;
        public float RecursionEndGlowIntensity => recursionEndGlowIntensity;

        internal static int DefaultRecursionLimit => 3;
        internal static float DefaultRenderScale => 0.75f;
        internal static float DefaultNearClipOffset => MinimumNearClipOffset;
        internal static Color DefaultRecursionEndColor => new Color(0.005f, 0.018f, 0.045f, 1f);
        internal static Color DefaultRecursionEndGlowColor => new Color(0.04f, 0.8f, 1.6f, 1f);
        internal static float DefaultRecursionEndGlowIntensity => 1.15f;
    }
}
