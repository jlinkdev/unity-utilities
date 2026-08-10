using UnityEngine;

namespace jlinkdev.UnityUtilities.Samples.IK
{
    /// <summary>
    /// Applies a simple color override to demo renderers without needing material assets.
    /// </summary>
    public sealed class IKDemoRendererTint : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] [Tooltip("Renderer to tint. Uses a renderer on this object when left empty.")]
        private Renderer _renderer;
        [SerializeField] [Tooltip("Color applied through a material property block.")]
        private Color _color = Color.white;

        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        public void Apply()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_renderer == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(ColorId, _color);
            _propertyBlock.SetColor(BaseColorId, _color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
