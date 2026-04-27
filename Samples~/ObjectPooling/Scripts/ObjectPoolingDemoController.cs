using System.Collections.Generic;
using jlinkdev.UnityUtilities.ObjectPooling;
using UnityEngine;

namespace jlinkdev.UnityUtilities.Samples.ObjectPooling
{
    /// <summary>
    /// Drives the object pooling sample scene.
    /// </summary>
    public sealed class ObjectPoolingDemoController : MonoBehaviour
    {
        private enum DemoMode
        {
            SinglePoolHandle,
            Registry
        }

        [Header("Pool")]
        [SerializeField] [Tooltip("GameObjectPoolHandle used to demonstrate a single scene-authored prefab pool.")]
        private GameObjectPoolHandle _poolHandle;
        [SerializeField] [Tooltip("GameObjectPoolRegistry used to demonstrate multiple prefab pools selected by prefab reference.")]
        private GameObjectPoolRegistry _poolRegistry;
        [SerializeField] [Tooltip("Optional parent assigned to objects while they are checked out from either pool type.")]
        private Transform _activeParent;

        [Header("Registry")]
        [SerializeField] [Tooltip("Current demo mode. Toggle at runtime with the overlay button.")]
        private DemoMode _demoMode;
        [SerializeField] [Tooltip("Prefabs spawned by the registry demo. Each prefab must have a matching definition on the registry.")]
        private GameObject[] _registryPrefabs;
        [SerializeField] [Tooltip("Colors assigned to registry prefabs by array index so each prefab type is visually distinct.")]
        private Color[] _registryPrefabColors =
        {
            new Color(0.95f, 0.25f, 0.2f),
            new Color(0.2f, 0.65f, 1f),
            new Color(0.25f, 0.9f, 0.35f)
        };

        [Header("Spawning")]
        [SerializeField] [Tooltip("Center point used when choosing random spawn positions.")]
        private Transform _spawnCenter;
        [SerializeField] [Tooltip("Radius around the spawn center where objects are spawned.")] [Min(0f)]
        private float _spawnRadius = 4f;
        [SerializeField] [Tooltip("Seconds between automatic spawns while auto spawn is enabled.")] [Min(0.01f)]
        private float _spawnInterval = 0.15f;
        [SerializeField] [Tooltip("Seconds before each demo object automatically returns to its pool.")] [Min(0.1f)]
        private float _objectLifetime = 2.5f;
        [SerializeField] [Tooltip("Number of objects spawned when using the burst control.")] [Min(1)]
        private int _burstCount = 12;
        [SerializeField] [Tooltip("Number of additional instances requested when using the prewarm control.")] [Min(1)]
        private int _prewarmStep = 5;
        [SerializeField] [Tooltip("Whether the demo continuously spawns objects while playing.")]
        private bool _autoSpawn = true;

        [Header("Motion")]
        [SerializeField] [Tooltip("Random speed range applied to spawned demo objects.")]
        private Vector2 _launchSpeedRange = new Vector2(0.75f, 2.5f);
        [SerializeField] [Tooltip("Random yaw spin speed range applied to spawned demo objects.")]
        private Vector2 _spinSpeedRange = new Vector2(45f, 180f);

        private readonly List<PooledDemoObject> _activeObjects = new List<PooledDemoObject>();
        private float _timer;
        private int _nextRegistryPrefabIndex;
        private Color _lastRegistryPrefabColor = Color.white;

        public GameObjectPoolHandle PoolHandle => _poolHandle;
        public GameObjectPoolRegistry PoolRegistry => _poolRegistry;
        public bool IsRegistryMode => _demoMode == DemoMode.Registry;
        public string CurrentModeName => IsRegistryMode ? "GameObjectPoolRegistry" : "GameObjectPoolHandle";
        public string CurrentModeDescription => IsRegistryMode
            ? "Add GameObjectPoolRegistry to one scene object, add one GameObjectPoolDefinition per prefab, then spawn by prefab reference and despawn by instance."
            : "Add GameObjectPoolHandle to a scene object, assign one prefab plus capacity settings, then call Spawn and Despawn on that handle.";

        public bool AutoSpawn
        {
            get => _autoSpawn;
            set => _autoSpawn = value;
        }

        public int ActiveDemoObjects => _activeObjects.Count;
        public int TotalSpawnAttempts { get; private set; }
        public int SuccessfulSpawns { get; private set; }
        public int FailedSpawns { get; private set; }
        public int ReturnedObjects { get; private set; }
        public int BurstCount => _burstCount;
        public int PrewarmStep => _prewarmStep;
        public int RegistryPrefabCount => _registryPrefabs != null ? _registryPrefabs.Length : 0;

        public string GetRegistryPrefabName(int index)
        {
            if (_registryPrefabs == null || index < 0 || index >= _registryPrefabs.Length || _registryPrefabs[index] == null)
            {
                return "Unassigned";
            }

            return _registryPrefabs[index].name;
        }

        public Color GetRegistryPrefabColor(int index)
        {
            return ResolveRegistryColor(index);
        }

        private void Update()
        {
            if (!_autoSpawn)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < _spawnInterval)
            {
                return;
            }

            _timer = 0f;
            SpawnOne();
        }

        public void SpawnOne()
        {
            TotalSpawnAttempts++;
            Vector3 position = GetSpawnPosition();
            GameObject instance = IsRegistryMode ? SpawnFromRegistry(position) : SpawnFromHandle(position);

            if (instance == null)
            {
                FailedSpawns++;
                return;
            }

            PooledDemoObject pooledObject = instance.GetComponent<PooledDemoObject>();
            if (pooledObject == null)
            {
                pooledObject = instance.AddComponent<PooledDemoObject>();
            }

            if (IsRegistryMode)
            {
                pooledObject.Initialize(
                    this,
                    _poolRegistry,
                    _objectLifetime,
                    GetLaunchVelocity(position),
                    GetSpinSpeed(),
                    _lastRegistryPrefabColor);
            }
            else
            {
                pooledObject.Initialize(this, _poolHandle, _objectLifetime, GetLaunchVelocity(position), GetSpinSpeed());
            }

            _activeObjects.Add(pooledObject);
            SuccessfulSpawns++;
        }

        public void SpawnBurst()
        {
            for (int i = 0; i < _burstCount; i++)
            {
                SpawnOne();
            }
        }

        public void ReturnAll()
        {
            for (int i = _activeObjects.Count - 1; i >= 0; i--)
            {
                if (_activeObjects[i] != null)
                {
                    _activeObjects[i].ForceReturn();
                }
            }

            _activeObjects.Clear();
        }

        public void PrewarmAdditional()
        {
            if (IsRegistryMode)
            {
                PrewarmRegistryPrefabs();
                return;
            }

            if (_poolHandle == null)
            {
                return;
            }

            _poolHandle.Prewarm(_prewarmStep);
        }

        public void ClearInactive()
        {
            if (IsRegistryMode)
            {
                if (_poolRegistry != null)
                {
                    _poolRegistry.ClearInactive();
                }

                return;
            }

            if (_poolHandle == null)
            {
                return;
            }

            _poolHandle.ClearInactive();
        }

        public void ClearAll()
        {
            if (IsRegistryMode)
            {
                if (_poolRegistry != null)
                {
                    _poolRegistry.ClearAll();
                }

                _activeObjects.Clear();
                return;
            }

            if (_poolHandle == null)
            {
                return;
            }

            _poolHandle.ClearAll();
            _activeObjects.Clear();
        }

        public void NotifyReturned(PooledDemoObject pooledObject)
        {
            if (pooledObject == null)
            {
                return;
            }

            _activeObjects.Remove(pooledObject);
            ReturnedObjects++;
        }

        public void ToggleMode()
        {
            ReturnAll();
            _demoMode = IsRegistryMode ? DemoMode.SinglePoolHandle : DemoMode.Registry;
        }

        private GameObject SpawnFromHandle(Vector3 position)
        {
            if (_poolHandle == null)
            {
                Debug.LogWarning("ObjectPoolingDemoController needs a GameObjectPoolHandle reference.", this);
                return null;
            }

            return _poolHandle.Spawn(position, Random.rotation, _activeParent);
        }

        private GameObject SpawnFromRegistry(Vector3 position)
        {
            if (_poolRegistry == null)
            {
                Debug.LogWarning("ObjectPoolingDemoController needs a GameObjectPoolRegistry reference.", this);
                return null;
            }

            GameObject prefab = GetNextRegistryPrefab(out int prefabIndex);
            if (prefab == null)
            {
                Debug.LogWarning("ObjectPoolingDemoController needs at least one registry prefab.", this);
                return null;
            }

            _lastRegistryPrefabColor = ResolveRegistryColor(prefabIndex);
            return _poolRegistry.Spawn(prefab, position, Random.rotation, _activeParent);
        }

        private void PrewarmRegistryPrefabs()
        {
            if (_poolRegistry == null || _registryPrefabs == null)
            {
                return;
            }

            for (int i = 0; i < _registryPrefabs.Length; i++)
            {
                GameObject prefab = _registryPrefabs[i];
                if (prefab != null)
                {
                    _poolRegistry.Prewarm(prefab, _prewarmStep);
                }
            }
        }

        private GameObject GetNextRegistryPrefab(out int prefabIndex)
        {
            prefabIndex = -1;
            if (_registryPrefabs == null || _registryPrefabs.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < _registryPrefabs.Length; i++)
            {
                int index = _nextRegistryPrefabIndex % _registryPrefabs.Length;
                _nextRegistryPrefabIndex++;

                if (_registryPrefabs[index] != null)
                {
                    prefabIndex = index;
                    return _registryPrefabs[index];
                }
            }

            return null;
        }

        private Color ResolveRegistryColor(int prefabIndex)
        {
            if (prefabIndex < 0)
            {
                return Color.white;
            }

            if (_registryPrefabColors == null || _registryPrefabColors.Length == 0)
            {
                return Color.HSVToRGB(Mathf.Repeat(prefabIndex * 0.381966f, 1f), 0.72f, 1f);
            }

            return _registryPrefabColors[prefabIndex % _registryPrefabColors.Length];
        }

        private Vector3 GetSpawnPosition()
        {
            Vector3 center = _spawnCenter != null ? _spawnCenter.position : transform.position;
            Vector2 offset = Random.insideUnitCircle * _spawnRadius;
            return center + new Vector3(offset.x, 0f, offset.y);
        }

        private Vector3 GetLaunchVelocity(Vector3 spawnPosition)
        {
            Vector3 center = _spawnCenter != null ? _spawnCenter.position : transform.position;
            Vector3 direction = spawnPosition - center;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Random.onUnitSphere;
                direction.y = 0f;
            }

            direction.y = 0.35f;
            return direction.normalized * Random.Range(_launchSpeedRange.x, _launchSpeedRange.y);
        }

        private float GetSpinSpeed()
        {
            float direction = Random.value < 0.5f ? -1f : 1f;
            return Random.Range(_spinSpeedRange.x, _spinSpeedRange.y) * direction;
        }
    }
}
