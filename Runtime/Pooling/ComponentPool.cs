using System;
using UnityEngine;

namespace UnityUtilities.Pooling
{
    /// <summary>
    /// Unity-specific pool for component instances.
    /// </summary>
    /// <typeparam name="TComponent">Component type to pool.</typeparam>
    public sealed class ComponentPool<TComponent> where TComponent : Component
    {
        private readonly GameObjectPool _gameObjectPool;

        /// <summary>
        /// Creates a component pool using a component prefab.
        /// </summary>
        /// <param name="prefab">Component prefab to instantiate.</param>
        /// <param name="initialCapacity">Number of objects to prewarm.</param>
        /// <param name="maxCapacity">Maximum inactive pooled instances.</param>
        /// <param name="inactiveParent">Optional parent transform for inactive pooled objects.</param>
        /// <param name="activateOnGet">Whether instances activate on get.</param>
        /// <param name="deactivateOnReturn">Whether instances deactivate on return.</param>
        public ComponentPool(
            TComponent prefab,
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

            _gameObjectPool = new GameObjectPool(
                prefab.gameObject,
                initialCapacity,
                maxCapacity,
                inactiveParent,
                activateOnGet,
                deactivateOnReturn);
        }

        /// <summary>
        /// Gets the count of inactive pooled instances.
        /// </summary>
        public int InactiveCount => _gameObjectPool.InactiveCount;

        /// <summary>
        /// Spawns a pooled component instance.
        /// </summary>
        /// <param name="position">Optional world position.</param>
        /// <param name="rotation">Optional world rotation.</param>
        /// <param name="parent">Optional active parent transform.</param>
        /// <returns>Pooled component instance.</returns>
        public TComponent Spawn(Vector3? position = null, Quaternion? rotation = null, Transform parent = null)
        {
            GameObject go = _gameObjectPool.Spawn(position, rotation, parent);
            return go.GetComponent<TComponent>();
        }

        /// <summary>
        /// Despawns a pooled component instance.
        /// </summary>
        /// <param name="instance">Component instance to return.</param>
        /// <returns>True when successfully returned.</returns>
        public bool Despawn(TComponent instance)
        {
            return instance != null && _gameObjectPool.Despawn(instance.gameObject);
        }

        /// <summary>
        /// Prewarms the pool.
        /// </summary>
        /// <param name="count">Count to prewarm.</param>
        public void Prewarm(int count)
        {
            _gameObjectPool.Prewarm(count);
        }

        /// <summary>
        /// Clears pooled instances.
        /// </summary>
        /// <param name="destroyAll">When true, also destroys tracked active instances.</param>
        public void Clear(bool destroyAll = false)
        {
            _gameObjectPool.Clear(destroyAll);
        }
    }
}
