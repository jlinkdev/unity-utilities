using UnityEngine;

namespace jlinkdev.BlueprintRendering
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [AddComponentMenu("Rendering/Blueprint Camera")]
    public sealed class BlueprintCamera : MonoBehaviour
    {
        [Tooltip("Opaque geometry to include in this drawing.")]
        public LayerMask layers = ~0;
        [Range(1, 4), Tooltip("Number of surfaces captured per pixel, including the front surface.")]
        public int surfaceLayers = 3;
        [Range(0f, 1f)] public float hiddenInk = 0.36f;
        [Range(1, 3)] public int lineWidth = 1;
        [Range(1f, 89f)] public float creaseAngle = 32f;
        public Color paperColor = new Color(0.035f, 0.12f, 0.23f);
        public Color inkColor = new Color(0.85f, 0.96f, 1f);
    }
}
