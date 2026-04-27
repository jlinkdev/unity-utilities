using System;
using UnityEngine;

namespace jlinkdev.UnityUtilities.ObjectPooling
{
    /// <summary>
    /// Serializable configuration for one prefab-backed game object pool.
    /// </summary>
    [Serializable]
    public sealed class GameObjectPoolDefinition
    {
        [SerializeField] [Tooltip("Prefab used as both the pooled object template and the registry lookup reference.")]
        private GameObject _prefab;
        [SerializeField] [Tooltip("Number of prefab instances created when this pool is initialized.")] [Min(0)]
        private int _initialCapacity = 8;
        [SerializeField] [Tooltip("Maximum number of prefab instances this pool can track at once.")] [Min(1)]
        private int _maxCapacity = 64;
        [SerializeField] [Tooltip("Optional parent for inactive instances from this pool. Uses the registry default inactive parent when left empty.")]
        private Transform _inactiveParent;
        [SerializeField] [Tooltip("Whether instances are activated when retrieved from the pool.")]
        private bool _activateOnGet = true;
        [SerializeField] [Tooltip("Whether instances are deactivated when returned to the pool.")]
        private bool _deactivateOnReturn = true;

        /// <summary>
        /// Gets the prefab used to create pooled instances and identify this pool in a registry.
        /// </summary>
        public GameObject Prefab => _prefab;

        /// <summary>
        /// Gets the number of instances prewarmed during registry initialization.
        /// </summary>
        public int InitialCapacity => _initialCapacity;

        /// <summary>
        /// Gets the maximum number of instances this pool can track.
        /// </summary>
        public int MaxCapacity => _maxCapacity;

        internal bool IsValid(out string message)
        {
            if (_prefab == null)
            {
                message = "Pool definition has no prefab.";
                return false;
            }

            if (_initialCapacity < 0)
            {
                message = $"Pool definition for '{_prefab.name}' has a negative initial capacity.";
                return false;
            }

            if (_maxCapacity <= 0)
            {
                message = $"Pool definition for '{_prefab.name}' must have a max capacity greater than zero.";
                return false;
            }

            if (_initialCapacity > _maxCapacity)
            {
                message = $"Pool definition for '{_prefab.name}' has an initial capacity greater than max capacity.";
                return false;
            }

            message = null;
            return true;
        }

        internal GameObjectPool CreatePool(Transform defaultInactiveParent)
        {
            Transform inactiveParent = _inactiveParent != null ? _inactiveParent : defaultInactiveParent;
            return new GameObjectPool(
                _prefab,
                _initialCapacity,
                _maxCapacity,
                inactiveParent,
                _activateOnGet,
                _deactivateOnReturn);
        }
    }
}
