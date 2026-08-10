using jlinkdev.UnityUtilities.ObjectPooling;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Samples.ObjectPooling
{
    /// <summary>
    /// Simple visual behavior for spawned sample objects.
    /// </summary>
    public sealed class PooledDemoObject : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] [Tooltip("Renderer tinted when this object is spawned. Uses a child renderer automatically when left empty.")]
        private Renderer _renderer;

        private MaterialPropertyBlock _propertyBlock;
        private ObjectPoolingDemoController _owner;
        private GameObjectPoolHandle _poolHandle;
        private GameObjectPoolRegistry _poolRegistry;
        private Vector3 _baseScale;
        private Vector3 _velocity;
        private float _spinSpeed;
        private float _returnAt;
        private bool _isCheckedOut;

        private void Awake()
        {
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }

            _propertyBlock = new MaterialPropertyBlock();
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            if (!_isCheckedOut)
            {
                return;
            }

            transform.position += _velocity * Time.deltaTime;
            transform.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.World);

            if (Time.time >= _returnAt)
            {
                ForceReturn();
            }
        }

        public void Initialize(
            ObjectPoolingDemoController owner,
            GameObjectPoolHandle poolHandle,
            float lifetime,
            Vector3 velocity,
            float spinSpeed)
        {
            _owner = owner;
            _poolHandle = poolHandle;
            _poolRegistry = null;
            InitializeLifetime(lifetime, velocity, spinSpeed, false, Color.white);
        }

        public void Initialize(
            ObjectPoolingDemoController owner,
            GameObjectPoolRegistry poolRegistry,
            float lifetime,
            Vector3 velocity,
            float spinSpeed,
            Color colorOverride)
        {
            _owner = owner;
            _poolHandle = null;
            _poolRegistry = poolRegistry;
            InitializeLifetime(lifetime, velocity, spinSpeed, true, colorOverride);
        }

        private void InitializeLifetime(
            float lifetime,
            Vector3 velocity,
            float spinSpeed,
            bool hasColorOverride,
            Color colorOverride)
        {
            _velocity = velocity;
            _spinSpeed = spinSpeed;
            _returnAt = Time.time + lifetime;
            _isCheckedOut = true;

            if (_baseScale == Vector3.zero)
            {
                _baseScale = transform.localScale;
            }

            transform.localScale = _baseScale;
            ApplyColor(hasColorOverride ? colorOverride : Color.HSVToRGB(Random.value, 0.65f, 1f));
        }

        public void ForceReturn()
        {
            if (!_isCheckedOut)
            {
                return;
            }

            _isCheckedOut = false;
            transform.localScale = _baseScale;
            _owner?.NotifyReturned(this);

            if (_poolHandle != null)
            {
                _poolHandle.Despawn(gameObject);
                return;
            }

            if (_poolRegistry != null)
            {
                _poolRegistry.Despawn(gameObject);
                return;
            }

            gameObject.SetActive(false);
        }

        private void ApplyColor(Color color)
        {
            if (_renderer == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(ColorId, color);
            _propertyBlock.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
