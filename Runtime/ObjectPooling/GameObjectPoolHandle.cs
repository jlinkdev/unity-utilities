using UnityEngine;

namespace jlinkdev.UnityUtilities.ObjectPooling
{
    /// <summary>
    /// Inspector-friendly pooled prefab provider.
    /// </summary>
    public sealed class GameObjectPoolHandle : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] [Min(0)] private int _initialCapacity = 8;
        [SerializeField] [Min(1)] private int _maxCapacity = 64;
        [SerializeField] private Transform _inactiveParent;
        [SerializeField] private bool _activateOnGet = true;
        [SerializeField] private bool _deactivateOnReturn = true;
        [SerializeField] private bool _prewarmOnAwake = true;

        private GameObjectPool _pool;

        /// <summary>
        /// Gets total tracked instances.
        /// </summary>
        public int CountAll => _pool != null ? _pool.CountAll : 0;

        /// <summary>
        /// Gets currently inactive instances.
        /// </summary>
        public int CountInactive => _pool != null ? _pool.CountInactive : 0;

        /// <summary>
        /// Gets checked-out active instances.
        /// </summary>
        public int CountActive => _pool != null ? _pool.CountActive : 0;

        private void Awake()
        {
            EnsureInitialized(_prewarmOnAwake);
        }

        /// <summary>
        /// Spawns an instance from the pool.
        /// </summary>
        public GameObject Spawn()
        {
            EnsureInitialized(true);
            return _pool.Get();
        }

        /// <summary>
        /// Spawns an instance from the pool with transform values.
        /// </summary>
        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            EnsureInitialized(true);
            return _pool.Get(position, rotation, parent);
        }

        /// <summary>
        /// Returns an instance to this pool.
        /// </summary>
        public bool Despawn(GameObject instance)
        {
            if (_pool == null)
            {
                Debug.LogWarning("Pool is not initialized.", this);
                return false;
            }

            return _pool.Return(instance);
        }

        /// <summary>
        /// Prewarms this pool with additional instances.
        /// </summary>
        public int Prewarm(int count)
        {
            EnsureInitialized(false);
            return _pool.Prewarm(count);
        }

        /// <summary>
        /// Destroys inactive pooled instances.
        /// </summary>
        public void ClearInactive()
        {
            if (_pool == null)
            {
                return;
            }

            _pool.ClearInactive();
        }

        /// <summary>
        /// Destroys all tracked pooled instances.
        /// </summary>
        public void ClearAll()
        {
            if (_pool == null)
            {
                return;
            }

            _pool.ClearAll();
        }

        private void OnDestroy()
        {
            if (_pool == null)
            {
                return;
            }

            _pool.ClearAll();
            _pool = null;
        }

        private void EnsureInitialized(bool prewarmIfNeeded)
        {
            if (_pool != null)
            {
                return;
            }

            if (_prefab == null)
            {
                Debug.LogWarning("Cannot initialize GameObjectPoolHandle without a prefab.", this);
                return;
            }

            _pool = new GameObjectPool(
                _prefab,
                prewarmIfNeeded ? _initialCapacity : 0,
                _maxCapacity,
                _inactiveParent,
                _activateOnGet,
                _deactivateOnReturn);
        }
    }
}
