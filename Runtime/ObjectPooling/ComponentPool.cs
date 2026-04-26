using System;
using UnityEngine;

namespace UnityUtilities.Pooling
{
    /// <summary>
    /// Unity-specific reusable pool for <see cref="Component"/> instances.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly GameObjectPool _gameObjectPool;

        /// <summary>
        /// Initializes a new <see cref="ComponentPool{T}"/>.
        /// </summary>
        /// <param name="componentPrefab">Component prefab used to create pooled objects.</param>
        /// <param name="initialCapacity">Number of instances to prewarm.</param>
        /// <param name="maxCapacity">Maximum number of tracked instances.</param>
        /// <param name="inactiveParent">Optional parent for inactive pooled objects.</param>
        /// <param name="activateOnGet">Set active on retrieval.</param>
        /// <param name="deactivateOnReturn">Set inactive on return.</param>
        public ComponentPool(
            T componentPrefab,
            int initialCapacity,
            int maxCapacity,
            Transform inactiveParent = null,
            bool activateOnGet = true,
            bool deactivateOnReturn = true)
        {
            if (componentPrefab == null)
            {
                throw new ArgumentNullException(nameof(componentPrefab));
            }

            _gameObjectPool = new GameObjectPool(
                componentPrefab.gameObject,
                initialCapacity,
                maxCapacity,
                inactiveParent,
                activateOnGet,
                deactivateOnReturn);
        }

        /// <summary>
        /// Gets total tracked instances.
        /// </summary>
        public int CountAll => _gameObjectPool.CountAll;

        /// <summary>
        /// Gets currently inactive instances.
        /// </summary>
        public int CountInactive => _gameObjectPool.CountInactive;

        /// <summary>
        /// Gets checked-out active instances.
        /// </summary>
        public int CountActive => _gameObjectPool.CountActive;

        /// <summary>
        /// Gets an instance from the pool.
        /// </summary>
        public T Get()
        {
            GameObject instance = _gameObjectPool.Get();
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <summary>
        /// Gets an instance from the pool and sets transform data.
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject instance = _gameObjectPool.Get(position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        public bool Return(T instance)
        {
            return instance != null && _gameObjectPool.Return(instance.gameObject);
        }

        /// <summary>
        /// Prewarms the pool with additional instances.
        /// </summary>
        public int Prewarm(int count)
        {
            return _gameObjectPool.Prewarm(count);
        }

        /// <summary>
        /// Destroys all inactive pooled instances.
        /// </summary>
        public void ClearInactive()
        {
            _gameObjectPool.ClearInactive();
        }

        /// <summary>
        /// Destroys all tracked pooled instances, including checked-out instances.
        /// </summary>
        public void ClearAll()
        {
            _gameObjectPool.ClearAll();
        }
    }
}
