using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityUtilities.Pooling
{
    /// <summary>
    /// Generic reusable object pool with configurable prewarm and maximum capacity.
    /// </summary>
    /// <typeparam name="T">Type of pooled object.</typeparam>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _inactive;
        private readonly HashSet<T> _inactiveLookup;
        private readonly HashSet<T> _allInstances;

        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;
        private readonly Action<T> _onDestroy;
        private readonly Func<T, bool> _isValid;

        /// <summary>
        /// Creates a new object pool.
        /// </summary>
        /// <param name="createFunc">Factory used to create new instances.</param>
        /// <param name="onGet">Callback invoked when an object is retrieved from the pool.</param>
        /// <param name="onReturn">Callback invoked when an object is returned to the pool.</param>
        /// <param name="onDestroy">Callback invoked when an object is discarded/destroyed.</param>
        /// <param name="isValid">Optional validation callback for inactive entries. Invalid entries are culled.</param>
        /// <param name="initialCapacity">Number of objects to prewarm into the pool.</param>
        /// <param name="maxCapacity">Maximum number of pooled inactive objects kept for reuse.</param>
        public ObjectPool(
            Func<T> createFunc,
            Action<T> onGet = null,
            Action<T> onReturn = null,
            Action<T> onDestroy = null,
            Func<T, bool> isValid = null,
            int initialCapacity = 0,
            int maxCapacity = int.MaxValue)
        {
            if (createFunc == null)
            {
                throw new ArgumentNullException(nameof(createFunc));
            }

            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be >= 0.");
            }

            if (maxCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Max capacity must be >= 1.");
            }

            _createFunc = createFunc;
            _onGet = onGet;
            _onReturn = onReturn;
            _onDestroy = onDestroy;
            _isValid = isValid;

            _inactive = new Stack<T>(Mathf.Max(initialCapacity, 1));
            _inactiveLookup = new HashSet<T>();
            _allInstances = new HashSet<T>();

            MaxCapacity = maxCapacity;

            Prewarm(initialCapacity);
        }

        /// <summary>
        /// Gets the maximum number of inactive instances stored for reuse.
        /// </summary>
        public int MaxCapacity { get; }

        /// <summary>
        /// Gets the number of currently inactive pooled objects.
        /// </summary>
        public int InactiveCount => _inactive.Count;

        /// <summary>
        /// Gets the total number of known objects created by this pool.
        /// </summary>
        public int TotalCount => _allInstances.Count;

        /// <summary>
        /// Retrieves an instance from the pool, creating a new one when needed.
        /// </summary>
        /// <returns>A pooled instance.</returns>
        public T Get()
        {
            T item;

            while (_inactive.Count > 0)
            {
                item = _inactive.Pop();
                _inactiveLookup.Remove(item);

                if (IsValid(item))
                {
                    _onGet?.Invoke(item);
                    return item;
                }

                _allInstances.Remove(item);
            }

            item = _createFunc();

            if (!IsValid(item))
            {
                throw new InvalidOperationException("Pool create function returned an invalid instance.");
            }

            _allInstances.Add(item);
            _onGet?.Invoke(item);
            return item;
        }

        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        /// <param name="item">The instance to return.</param>
        /// <returns>True if returned to pool; false when ignored/discarded.</returns>
        public bool Return(T item)
        {
            if (!IsValid(item))
            {
                return false;
            }

            if (!_allInstances.Contains(item))
            {
                Debug.LogWarning($"Tried to return an item to pool {nameof(ObjectPool<T>)} that was not created by this pool: {item}.");
                return false;
            }

            if (_inactiveLookup.Contains(item))
            {
                Debug.LogWarning($"Tried to return item to pool {nameof(ObjectPool<T>)} more than once: {item}.");
                return false;
            }

            _onReturn?.Invoke(item);

            if (_inactive.Count >= MaxCapacity)
            {
                _allInstances.Remove(item);
                _onDestroy?.Invoke(item);
                return false;
            }

            _inactive.Push(item);
            _inactiveLookup.Add(item);
            return true;
        }

        /// <summary>
        /// Prewarms the pool by creating and returning new instances.
        /// </summary>
        /// <param name="count">Number of instances to create.</param>
        public void Prewarm(int count)
        {
            if (count <= 0)
            {
                return;
            }

            int toCreate = Mathf.Min(count, MaxCapacity - _inactive.Count);
            for (int i = 0; i < toCreate; i++)
            {
                T item = _createFunc();
                if (!IsValid(item))
                {
                    continue;
                }

                _allInstances.Add(item);
                _onReturn?.Invoke(item);
                _inactive.Push(item);
                _inactiveLookup.Add(item);
            }
        }

        /// <summary>
        /// Clears pooled inactive instances.
        /// </summary>
        /// <param name="destroyAll">When true, also destroys active tracked instances and fully resets pool state.</param>
        public void Clear(bool destroyAll = false)
        {
            while (_inactive.Count > 0)
            {
                T item = _inactive.Pop();
                _inactiveLookup.Remove(item);
                _allInstances.Remove(item);

                if (IsValid(item))
                {
                    _onDestroy?.Invoke(item);
                }
            }

            if (!destroyAll)
            {
                return;
            }

            if (_allInstances.Count == 0)
            {
                return;
            }

            T[] remaining = new T[_allInstances.Count];
            _allInstances.CopyTo(remaining);
            for (int i = 0; i < remaining.Length; i++)
            {
                T item = remaining[i];
                _allInstances.Remove(item);
                _inactiveLookup.Remove(item);

                if (IsValid(item))
                {
                    _onDestroy?.Invoke(item);
                }
            }
        }

        private bool IsValid(T item)
        {
            if (_isValid != null)
            {
                return _isValid(item);
            }

            return item != null;
        }
    }
}
