using UnityEngine;

namespace UnityUtilities.Pooling
{
    /// <summary>
    /// Inspector-driven <see cref="GameObject"/> pool component.
    /// </summary>
    public sealed class GameObjectPoolBehaviour : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private GameObject prefab;
        [SerializeField] [Min(0)] private int initialCapacity = 8;
        [SerializeField] [Min(1)] private int maxCapacity = 64;
        [SerializeField] private Transform inactiveParent;
        [SerializeField] private bool prewarmOnAwake = true;

        [Header("Activation")]
        [SerializeField] private bool activateOnSpawn = true;
        [SerializeField] private bool deactivateOnDespawn = true;

        private GameObjectPool _pool;

        /// <summary>
        /// Gets whether this pool has been initialized.
        /// </summary>
        public bool IsInitialized => _pool != null;

        /// <summary>
        /// Gets the count of currently inactive pooled objects.
        /// </summary>
        public int InactiveCount => _pool == null ? 0 : _pool.InactiveCount;

        private void Awake()
        {
            EnsureInitialized();

            if (prewarmOnAwake)
            {
                _pool.Prewarm(initialCapacity);
            }
        }

        private void OnDestroy()
        {
            if (_pool != null)
            {
                _pool.Clear(destroyAll: true);
            }
        }

        /// <summary>
        /// Initializes the runtime pool if needed.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_pool != null)
            {
                return;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"{nameof(GameObjectPoolBehaviour)} on '{name}' cannot initialize without a prefab.", this);
                return;
            }

            Transform poolParent = inactiveParent != null ? inactiveParent : transform;

            _pool = new GameObjectPool(
                prefab,
                initialCapacity: 0,
                maxCapacity,
                poolParent,
                activateOnSpawn,
                deactivateOnDespawn);
        }

        /// <summary>
        /// Spawns an instance from this pool.
        /// </summary>
        /// <param name="position">Optional world position.</param>
        /// <param name="rotation">Optional world rotation.</param>
        /// <param name="parent">Optional active parent transform.</param>
        /// <returns>Spawned instance or null when pool is unavailable.</returns>
        public GameObject Spawn(Vector3? position = null, Quaternion? rotation = null, Transform parent = null)
        {
            EnsureInitialized();
            return _pool == null ? null : _pool.Spawn(position, rotation, parent);
        }

        /// <summary>
        /// Returns an instance to this pool.
        /// </summary>
        /// <param name="instance">Instance to despawn.</param>
        /// <returns>True when the instance was successfully returned.</returns>
        public bool Despawn(GameObject instance)
        {
            if (_pool == null)
            {
                return false;
            }

            return _pool.Despawn(instance);
        }

        /// <summary>
        /// Prewarms the pool with additional objects.
        /// </summary>
        /// <param name="count">Count to prewarm.</param>
        public void Prewarm(int count)
        {
            EnsureInitialized();
            _pool?.Prewarm(count);
        }

        /// <summary>
        /// Clears pooled instances.
        /// </summary>
        /// <param name="destroyAll">When true, also destroys tracked active instances.</param>
        public void Clear(bool destroyAll = false)
        {
            _pool?.Clear(destroyAll);
        }
    }
}
