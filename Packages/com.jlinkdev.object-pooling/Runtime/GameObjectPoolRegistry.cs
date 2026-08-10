using System.Collections.Generic;
using UnityEngine;

namespace jlinkdev.UnityUtilities.ObjectPooling
{
    /// <summary>
    /// Inspector-friendly registry for managing multiple prefab-backed pools by prefab reference.
    /// </summary>
    public sealed class GameObjectPoolRegistry : MonoBehaviour
    {
        [SerializeField] [Tooltip("Reusable prefab pool definition assets initialized by this registry. Processed after scene definitions.")]
        private GameObjectPoolDefinitionSet[] _definitionSets;
        [SerializeField] [Tooltip("Scene-local prefab pool definitions initialized by this registry. Processed before definition sets.")]
        private GameObjectPoolDefinition[] _definitions;
        [SerializeField] [Tooltip("Parent used for inactive instances created by this registry.")]
        private Transform _defaultInactiveParent;
        [SerializeField] [Tooltip("Whether instances from this registry are activated when retrieved from a pool.")]
        private bool _activateOnGet = true;
        [SerializeField] [Tooltip("Whether instances from this registry are deactivated when returned to a pool.")]
        private bool _deactivateOnReturn = true;
        [SerializeField] [Tooltip("Number of instances prewarmed when an undefined prefab is registered at runtime.")] [Min(0)]
        private int _runtimeInitialCapacity;
        [SerializeField] [Tooltip("Maximum number of instances tracked when an undefined prefab is registered at runtime.")] [Min(1)]
        private int _runtimeMaxCapacity = 64;
        [SerializeField] [Tooltip("Whether the registry initializes all valid definitions during Awake.")]
        private bool _initializeOnAwake = true;
        [SerializeField] [Tooltip("Whether all pooled instances are destroyed when this registry is destroyed.")]
        private bool _clearAllOnDestroy = true;

        private readonly Dictionary<GameObject, GameObjectPool> _poolsByPrefab = new Dictionary<GameObject, GameObjectPool>();
        private readonly Dictionary<GameObject, GameObjectPool> _ownersByInstance = new Dictionary<GameObject, GameObjectPool>();
        private bool _initialized;

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
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            InitializeDefinitions(_definitions, "scene registry");

            if (_definitionSets == null)
            {
                return;
            }

            for (int i = 0; i < _definitionSets.Length; i++)
            {
                GameObjectPoolDefinitionSet definitionSet = _definitionSets[i];
                if (definitionSet == null)
                {
                    Debug.LogWarning($"Pool definition set at index {i} is null.", this);
                    continue;
                }

                InitializeDefinitions(definitionSet.Definitions, $"definition set '{definitionSet.name}'");
            }
        }

        /// <summary>
        /// Spawns an instance from the pool for the provided prefab.
        /// </summary>
        public GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        }

        /// <summary>
        /// Spawns an instance from the pool for the provided prefab and applies transform data.
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return TrySpawn(prefab, out GameObject instance, position, rotation, parent) ? instance : null;
        }

        /// <summary>
        /// Attempts to spawn an instance from the pool for the provided prefab.
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
        /// Attempts to prewarm the pool for the provided prefab.
        /// </summary>
        public int Prewarm(GameObject prefab, int count)
        {
            return TryGetPool(prefab, out GameObjectPool pool) ? pool.Prewarm(count) : 0;
        }

        /// <summary>
        /// Destroys inactive instances in every initialized pool.
        /// </summary>
        public void ClearInactive()
        {
            foreach (GameObjectPool pool in _poolsByPrefab.Values)
            {
                pool.ClearInactive();
            }
        }

        /// <summary>
        /// Destroys all tracked instances in every initialized pool.
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

            pool = CreateRuntimePool(prefab, out int initialCapacity, out int maxCapacity);
            _poolsByPrefab.Add(prefab, pool);
            Debug.LogWarning(
                $"No pool definition was initialized for prefab '{prefab.name}'. " +
                $"A runtime pool was created with initial capacity {initialCapacity} and max capacity {maxCapacity}.",
                this);
            return true;
        }

        private void InitializeDefinitions(GameObjectPoolDefinition[] definitions, string source)
        {
            if (definitions == null)
            {
                return;
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                GameObjectPoolDefinition definition = definitions[i];
                if (definition == null)
                {
                    Debug.LogWarning($"Pool definition at {source} index {i} is null.", this);
                    continue;
                }

                if (!definition.IsValid(out string message))
                {
                    Debug.LogWarning(message, this);
                    continue;
                }

                if (_poolsByPrefab.ContainsKey(definition.Prefab))
                {
                    Debug.LogWarning($"Duplicate pool prefab '{definition.Prefab.name}' in {source} ignored.", this);
                    continue;
                }

                _poolsByPrefab.Add(
                    definition.Prefab,
                    definition.CreatePool(_defaultInactiveParent, _activateOnGet, _deactivateOnReturn));
            }
        }

        private GameObjectPool CreateRuntimePool(GameObject prefab, out int initialCapacity, out int maxCapacity)
        {
            initialCapacity = Mathf.Max(0, _runtimeInitialCapacity);
            maxCapacity = Mathf.Max(1, _runtimeMaxCapacity);
            if (initialCapacity > maxCapacity)
            {
                maxCapacity = initialCapacity;
            }

            return new GameObjectPool(
                prefab,
                initialCapacity,
                maxCapacity,
                _defaultInactiveParent,
                _activateOnGet,
                _deactivateOnReturn);
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
