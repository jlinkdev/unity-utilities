using UnityEngine;

namespace jlinkdev.ObjectHighlighting
{
    [CreateAssetMenu(menuName = "Rendering/Object Highlight Style")]
    public sealed class HighlightStyle : ScriptableObject
    {
        [Tooltip("Exterior contour width in camera render pixels. Zero disables the contour.")]
        [Range(0, 8)] public float outlineWidth = 2;
        [Tooltip("Alpha controls contour opacity.")]
        public Color outlineColor = new Color(0.34f, 0.86f, 1f);
        [Tooltip("Soft halo radius in camera render pixels. Zero skips the blur.")]
        [Range(0, 32)] public float glowRadius = 8;
        [ColorUsage(true, true)] public Color glowColor = new Color(1f, 0.5f, 0.12f);
        [Tooltip("Additive halo strength. Zero skips the blur.")]
        [Range(0, 4)] public float glowIntensity = 0;

        internal float Width => outlineColor.a > 0 ? Mathf.Clamp(outlineWidth, 0, 8) : 0;
        internal float Radius => glowIntensity > 0 && glowColor.a > 0 ? Mathf.Clamp(glowRadius, 0, 32) : 0;
    }
}
