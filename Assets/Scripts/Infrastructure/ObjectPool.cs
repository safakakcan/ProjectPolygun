using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectPolygun.Core.Interfaces;

namespace ProjectPolygun.Infrastructure
{
    /// <summary>
    /// Generic object pool implementation for performance optimization
    /// </summary>
    /// <typeparam name="T">Type of objects to pool</typeparam>
    public class ObjectPool<T> : IObjectPool<T> where T : class
    {
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;

        public int Count => _pool.Count;

        /// <summary>
        /// Create a new object pool
        /// </summary>
        /// <param name="createFunc">Function to create new objects</param>
        /// <param name="onGet">Action to execute when getting object from pool</param>
        /// <param name="onReturn">Action to execute when returning object to pool</param>
        public ObjectPool(Func<T> createFunc, Action<T> onGet = null, Action<T> onReturn = null)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _onGet = onGet;
            _onReturn = onReturn;
        }

        public T Get()
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = _createFunc();
            }

            _onGet?.Invoke(obj);
            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("Attempted to return null object to pool");
                return;
            }

            _onReturn?.Invoke(obj);
            _pool.Enqueue(obj);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = _createFunc();
                _onReturn?.Invoke(obj);
                _pool.Enqueue(obj);
            }
        }
    }
} 