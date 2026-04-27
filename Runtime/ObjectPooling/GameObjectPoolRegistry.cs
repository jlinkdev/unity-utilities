using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.ObjectPooling
{
    /// <summary>
    /// Inspector-friendly registry for managing multiple prefab-backed pools by prefab reference.
    /// </summary>
    public sealed class GameObjectPoolRegistry : MonoBehaviour
    {
        [SerializeField] [Tooltip("Prefab pool definitions initialized by this registry. Each prefab can appear only once.")]
        private GameObjectPoolDefinition[] _definitions;
        [SerializeField] [Tooltip("Fallback parent for inactive instances when a definition does not specify its own inactive parent.")]
        private Transform _defaultInactiveParent;
        [SerializeField] [Tooltip("Whether the registry initializes all valid definitions during Awake.")]
        private bool _initializeOnAwake = true;
        [SerializeField] [Tooltip("Whether all pooled instances are destroyed when this registry is destroyed.")]
        private bool _clearAllOnDestroy = true;

        private readonly Dictionary<GameObject, GameObjectPool> _poolsByPrefab = new Dictionary<GameObject, GameObjectPool>();
        private readonly Dictionary<GameObject, GameObjectPool> _ownersByInstance = new Dictionary<GameObject, GameObjectPool>();

        /// <summary>
        /// Gets the total number of initialized pools.
        /// </summary>
        public int CountPools => _poolsByPrefab.Count;

        /// <summary>
        /// Gets the total number of tracked instances across all pools.
        /// </summary>
        public int CountAll
        {
            get
            {
                int count = 0;
                foreach (GameObjectPool pool in _poolsByPrefab.Values)
                {
                    count += pool.CountAll;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the total number of checked-out instances across all pools.
        /// </summary>
        public int CountActive
        {
            get
            {
                int count = 0;
                foreach (GameObjectPool pool in _poolsByPrefab.Values)
                {
                    count += pool.CountActive;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the total number of inactive instances across all pools.
        /// </summary>
        public int CountInactive
        {
            get
            {
                int count = 0;
                foreach (GameObjectPool pool in _poolsByPrefab.Values)
                {
                    count += pool.CountInactive;
                }

                return count;
            }
        }

        private void Awake()
        {
            if (_initializeOnAwake)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initializes all valid pool definitions.
        /// </summary>
        public void Initialize()
        {
            if (_poolsByPrefab.Count > 0)
            {
                return;
            }

            if (_definitions == null)
            {
                return;
            }

            for (int i = 0; i < _definitions.Length; i++)
            {
                GameObjectPoolDefinition definition = _definitions[i];
                if (definition == null)
                {
                    Debug.LogWarning($"Pool definition at index {i} is null.", this);
                    continue;
                }

                if (!definition.IsValid(out string message))
                {
                    Debug.LogWarning(message, this);
                    continue;
                }

                if (_poolsByPrefab.ContainsKey(definition.Prefab))
                {
                    Debug.LogWarning($"Duplicate pool prefab '{definition.Prefab.name}' ignored.", this);
                    continue;
                }

                _poolsByPrefab.Add(definition.Prefab, definition.CreatePool(_defaultInactiveParent));
            }
        }

        /// <summary>
        /// Spawns an instance from the pool registered for the provided prefab.
        /// </summary>
        public GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        }

        /// <summary>
        /// Spawns an instance from the pool registered for the provided prefab and applies transform data.
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return TrySpawn(prefab, out GameObject instance, position, rotation, parent) ? instance : null;
        }

        /// <summary>
        /// Attempts to spawn an instance from the pool registered for the provided prefab.
        /// </summary>
        public bool TrySpawn(GameObject prefab, out GameObject instance, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            instance = null;
            if (!TryGetPool(prefab, out GameObjectPool pool))
            {
                return false;
            }

            instance = pool.Get(position, rotation, parent);
            if (instance == null)
            {
                return false;
            }

            _ownersByInstance[instance] = pool;
            return true;
        }

        /// <summary>
        /// Returns a spawned instance to the pool that created it.
        /// </summary>
        public bool Despawn(GameObject instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (!_ownersByInstance.TryGetValue(instance, out GameObjectPool pool))
            {
                Debug.LogWarning($"Trying to despawn GameObject '{instance.name}' through a registry that did not spawn it.", this);
                return false;
            }

            _ownersByInstance.Remove(instance);
            return pool.Return(instance);
        }

        /// <summary>
        /// Attempts to prewarm a specific registered pool.
        /// </summary>
        public int Prewarm(GameObject prefab, int count)
        {
            return TryGetPool(prefab, out GameObjectPool pool) ? pool.Prewarm(count) : 0;
        }

        /// <summary>
        /// Destroys inactive instances in every registered pool.
        /// </summary>
        public void ClearInactive()
        {
            foreach (GameObjectPool pool in _poolsByPrefab.Values)
            {
                pool.ClearInactive();
            }
        }

        /// <summary>
        /// Destroys all tracked instances in every registered pool.
        /// </summary>
        public void ClearAll()
        {
            foreach (GameObjectPool pool in _poolsByPrefab.Values)
            {
                pool.ClearAll();
            }

            _ownersByInstance.Clear();
        }

        /// <summary>
        /// Returns whether a pool for the provided prefab is initialized.
        /// </summary>
        public bool ContainsPrefab(GameObject prefab)
        {
            Initialize();
            return prefab != null && _poolsByPrefab.ContainsKey(prefab);
        }

        private bool TryGetPool(GameObject prefab, out GameObjectPool pool)
        {
            Initialize();
            if (prefab == null)
            {
                pool = null;
                Debug.LogWarning("Cannot use a pool registry with a null prefab.", this);
                return false;
            }

            if (_poolsByPrefab.TryGetValue(prefab, out pool))
            {
                return true;
            }

            Debug.LogWarning($"No pool is registered for prefab '{prefab.name}'.", this);
            return false;
        }

        private void OnDestroy()
        {
            if (!_clearAllOnDestroy)
            {
                return;
            }

            ClearAll();
            _poolsByPrefab.Clear();
        }
    }
}
