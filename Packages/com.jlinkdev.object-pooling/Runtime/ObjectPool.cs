using System;
using System.Collections.Generic;

namespace jlinkdev.UnityUtilities.ObjectPooling
{
    /// <summary>
    /// Generic reusable object pool for reference types.
    /// </summary>
    /// <typeparam name="T">Pooled object type.</typeparam>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;
        private readonly Action<T> _onDestroy;
        private readonly Predicate<T> _isAlive;

        private readonly Stack<T> _inactive;
        private readonly HashSet<T> _allInstances;
        private readonly HashSet<T> _inactiveLookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
        /// </summary>
        /// <param name="factory">Factory used when a new instance is required.</param>
        /// <param name="initialCapacity">Number of instances to prewarm.</param>
        /// <param name="maxCapacity">Maximum number of pooled instances tracked by this pool.</param>
        /// <param name="onGet">Optional callback invoked when an instance is retrieved.</param>
        /// <param name="onReturn">Optional callback invoked when an instance is returned.</param>
        /// <param name="onDestroy">Optional callback invoked when an instance is destroyed by this pool.</param>
        /// <param name="isAlive">Optional predicate to validate whether an instance is still valid/alive.</param>
        public ObjectPool(
            Func<T> factory,
            int initialCapacity,
            int maxCapacity,
            Action<T> onGet = null,
            Action<T> onReturn = null,
            Action<T> onDestroy = null,
            Predicate<T> isAlive = null)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            if (maxCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCapacity));
            }

            if (initialCapacity > maxCapacity)
            {
                throw new ArgumentException("Initial capacity cannot be greater than max capacity.");
            }

            _factory = factory;
            _onGet = onGet;
            _onReturn = onReturn;
            _onDestroy = onDestroy;
            _isAlive = isAlive ?? DefaultIsAlive;

            MaxCapacity = maxCapacity;
            _inactive = new Stack<T>(maxCapacity);
            _allInstances = new HashSet<T>();
            _inactiveLookup = new HashSet<T>();

            Prewarm(initialCapacity);
        }

        /// <summary>
        /// Gets the total number of currently alive tracked instances.
        /// </summary>
        public int CountAll => _allInstances.Count;

        /// <summary>
        /// Gets the number of inactive instances available for retrieval.
        /// </summary>
        public int CountInactive => _inactive.Count;

        /// <summary>
        /// Gets the number of active instances currently checked out from the pool.
        /// </summary>
        public int CountActive => _allInstances.Count - _inactive.Count;

        /// <summary>
        /// Gets the maximum number of instances this pool can track.
        /// </summary>
        public int MaxCapacity { get; }

        /// <summary>
        /// Attempts to prewarm the pool with up to <paramref name="count"/> instances.
        /// </summary>
        /// <param name="count">The number of instances to prewarm.</param>
        /// <returns>The number of instances actually prewarmed.</returns>
        public int Prewarm(int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            int created = 0;
            while (created < count && _allInstances.Count < MaxCapacity)
            {
                T instance = _factory();
                if (!_isAlive(instance))
                {
                    continue;
                }

                _allInstances.Add(instance);
                _inactive.Push(instance);
                _inactiveLookup.Add(instance);
                _onReturn?.Invoke(instance);
                created++;
            }

            return created;
        }

        /// <summary>
        /// Gets an instance from the pool, creating one if needed and allowed by capacity.
        /// </summary>
        /// <returns>An instance, or <c>null</c> if no instance is available and capacity was reached.</returns>
        public T Get()
        {
            while (_inactive.Count > 0)
            {
                T instance = _inactive.Pop();
                _inactiveLookup.Remove(instance);

                if (!_isAlive(instance))
                {
                    _allInstances.Remove(instance);
                    continue;
                }

                _onGet?.Invoke(instance);
                return instance;
            }

            if (_allInstances.Count >= MaxCapacity)
            {
                return null;
            }

            T created = _factory();
            if (!_isAlive(created))
            {
                return null;
            }

            _allInstances.Add(created);
            _onGet?.Invoke(created);
            return created;
        }

        /// <summary>
        /// Attempts to return an instance to the pool.
        /// </summary>
        /// <param name="instance">The instance to return.</param>
        /// <returns><c>true</c> when successfully returned; otherwise <c>false</c>.</returns>
        public bool Return(T instance)
        {
            if (!_isAlive(instance))
            {
                return false;
            }

            if (!_allInstances.Contains(instance))
            {
                return false;
            }

            if (_inactiveLookup.Contains(instance))
            {
                return false;
            }

            _onReturn?.Invoke(instance);
            _inactive.Push(instance);
            _inactiveLookup.Add(instance);
            return true;
        }

        /// <summary>
        /// Destroys and removes all inactive instances from the pool.
        /// </summary>
        public void ClearInactive()
        {
            while (_inactive.Count > 0)
            {
                T instance = _inactive.Pop();
                _inactiveLookup.Remove(instance);

                if (_allInstances.Remove(instance) && _isAlive(instance))
                {
                    _onDestroy?.Invoke(instance);
                }
            }
        }

        /// <summary>
        /// Destroys and removes all tracked instances from the pool.
        /// </summary>
        public void ClearAll()
        {
            HashSet<T> snapshot = new HashSet<T>(_allInstances);
            _inactive.Clear();
            _inactiveLookup.Clear();
            _allInstances.Clear();

            foreach (T instance in snapshot)
            {
                if (_isAlive(instance))
                {
                    _onDestroy?.Invoke(instance);
                }
            }
        }

        /// <summary>
        /// Determines whether an instance belongs to this pool.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns><c>true</c> if this pool created and tracks the instance; otherwise <c>false</c>.</returns>
        public bool Contains(T instance)
        {
            return _isAlive(instance) && _allInstances.Contains(instance);
        }

        private static bool DefaultIsAlive(T instance)
        {
            return instance != null;
        }
    }
}
