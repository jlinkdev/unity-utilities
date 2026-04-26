using System;
using UnityEngine;

namespace UnityUtilities.Pooling
{
    /// <summary>
    /// Unity-specific reusable pool for <see cref="GameObject"/> instances.
    /// </summary>
    public sealed class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _inactiveParent;
        private readonly bool _activateOnGet;
        private readonly bool _deactivateOnReturn;
        private readonly ObjectPool<GameObject> _pool;

        /// <summary>
        /// Initializes a new <see cref="GameObjectPool"/>.
        /// </summary>
        /// <param name="prefab">Prefab used to create pooled instances.</param>
        /// <param name="initialCapacity">Number of instances to prewarm.</param>
        /// <param name="maxCapacity">Maximum number of tracked instances.</param>
        /// <param name="inactiveParent">Optional parent for inactive pooled objects.</param>
        /// <param name="activateOnGet">Set active on retrieval.</param>
        /// <param name="deactivateOnReturn">Set inactive on return.</param>
        public GameObjectPool(
            GameObject prefab,
            int initialCapacity,
            int maxCapacity,
            Transform inactiveParent = null,
            bool activateOnGet = true,
            bool deactivateOnReturn = true)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            _prefab = prefab;
            _inactiveParent = inactiveParent;
            _activateOnGet = activateOnGet;
            _deactivateOnReturn = deactivateOnReturn;

            _pool = new ObjectPool<GameObject>(
                CreateInstance,
                initialCapacity,
                maxCapacity,
                OnGet,
                OnReturn,
                DestroyInstance,
                IsAlive);
        }

        /// <summary>
        /// Gets total tracked instances.
        /// </summary>
        public int CountAll => _pool.CountAll;

        /// <summary>
        /// Gets currently inactive instances.
        /// </summary>
        public int CountInactive => _pool.CountInactive;

        /// <summary>
        /// Gets checked-out active instances.
        /// </summary>
        public int CountActive => _pool.CountActive;

        /// <summary>
        /// Gets an instance from the pool.
        /// </summary>
        public GameObject Get()
        {
            return _pool.Get();
        }

        /// <summary>
        /// Gets an instance from the pool and sets transform data.
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject instance = _pool.Get();
            if (instance == null)
            {
                return null;
            }

            Transform transform = instance.transform;
            transform.SetParent(parent, false);
            transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        public bool Return(GameObject instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (!_pool.Contains(instance))
            {
                Debug.LogWarning($"Trying to return GameObject '{instance.name}' to a pool that did not create it.");
                return false;
            }

            if (!_pool.Return(instance))
            {
                Debug.LogWarning($"GameObject '{instance.name}' has already been returned to the pool.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prewarms the pool with additional instances.
        /// </summary>
        public int Prewarm(int count)
        {
            return _pool.Prewarm(count);
        }

        /// <summary>
        /// Destroys all inactive pooled instances.
        /// </summary>
        public void ClearInactive()
        {
            _pool.ClearInactive();
        }

        /// <summary>
        /// Destroys all tracked pooled instances, including checked-out instances.
        /// </summary>
        public void ClearAll()
        {
            _pool.ClearAll();
        }

        private GameObject CreateInstance()
        {
            return UnityEngine.Object.Instantiate(_prefab, _inactiveParent);
        }

        private void OnGet(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (_activateOnGet)
            {
                instance.SetActive(true);
            }
        }

        private void OnReturn(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (_inactiveParent != null)
            {
                instance.transform.SetParent(_inactiveParent, false);
            }

            if (_deactivateOnReturn)
            {
                instance.SetActive(false);
            }
        }

        private static void DestroyInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(instance);
        }

        private static bool IsAlive(GameObject instance)
        {
            return instance != null;
        }
    }
}
