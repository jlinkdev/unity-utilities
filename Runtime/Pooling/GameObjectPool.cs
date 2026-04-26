using System;
using UnityEngine;

namespace UnityUtilities.Pooling
{
    /// <summary>
    /// Unity-specific pool for <see cref="GameObject"/> instances.
    /// </summary>
    public sealed class GameObjectPool
    {
        private readonly ObjectPool<GameObject> _pool;
        private readonly GameObject _prefab;
        private readonly Transform _inactiveParent;
        private readonly bool _activateOnGet;
        private readonly bool _deactivateOnReturn;

        /// <summary>
        /// Creates a pool for the provided prefab.
        /// </summary>
        /// <param name="prefab">Prefab used to create instances.</param>
        /// <param name="initialCapacity">Number of instances to prewarm.</param>
        /// <param name="maxCapacity">Maximum inactive pooled instances.</param>
        /// <param name="inactiveParent">Optional parent transform used for inactive returned objects.</param>
        /// <param name="activateOnGet">Whether to activate objects when spawned.</param>
        /// <param name="deactivateOnReturn">Whether to deactivate objects when despawned.</param>
        public GameObjectPool(
            GameObject prefab,
            int initialCapacity = 0,
            int maxCapacity = 128,
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
                OnGet,
                OnReturn,
                OnDestroy,
                IsValid,
                initialCapacity,
                maxCapacity);
        }

        /// <summary>
        /// Gets the amount of inactive pooled instances.
        /// </summary>
        public int InactiveCount => _pool.InactiveCount;

        /// <summary>
        /// Spawns a pooled object.
        /// </summary>
        /// <param name="position">Optional spawn position.</param>
        /// <param name="rotation">Optional spawn rotation.</param>
        /// <param name="parent">Optional parent transform for the active object.</param>
        /// <returns>The pooled instance.</returns>
        public GameObject Spawn(Vector3? position = null, Quaternion? rotation = null, Transform parent = null)
        {
            GameObject instance = _pool.Get();

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            if (position.HasValue)
            {
                instance.transform.position = position.Value;
            }

            if (rotation.HasValue)
            {
                instance.transform.rotation = rotation.Value;
            }

            return instance;
        }

        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        /// <param name="instance">Instance to despawn.</param>
        /// <returns>True if returned to pool.</returns>
        public bool Despawn(GameObject instance)
        {
            return _pool.Return(instance);
        }

        /// <summary>
        /// Prewarms the pool.
        /// </summary>
        /// <param name="count">Count to prewarm.</param>
        public void Prewarm(int count)
        {
            _pool.Prewarm(count);
        }

        /// <summary>
        /// Clears pooled instances.
        /// </summary>
        /// <param name="destroyAll">When true, also destroys any tracked active objects.</param>
        public void Clear(bool destroyAll = false)
        {
            _pool.Clear(destroyAll);
        }

        private GameObject CreateInstance()
        {
            return UnityEngine.Object.Instantiate(_prefab);
        }

        private void OnGet(GameObject instance)
        {
            if (_activateOnGet && !instance.activeSelf)
            {
                instance.SetActive(true);
            }
        }

        private void OnReturn(GameObject instance)
        {
            if (_inactiveParent != null)
            {
                instance.transform.SetParent(_inactiveParent, false);
            }

            if (_deactivateOnReturn && instance.activeSelf)
            {
                instance.SetActive(false);
            }
        }

        private void OnDestroy(GameObject instance)
        {
            UnityEngine.Object.Destroy(instance);
        }

        private static bool IsValid(GameObject instance)
        {
            return instance != null;
        }
    }
}
