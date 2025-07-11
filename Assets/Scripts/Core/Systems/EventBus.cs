using System;
using System.Collections.Generic;
using ProjectPolygun.Core.Interfaces;
using UnityEngine;

namespace ProjectPolygun.Core.Systems
{
    /// <summary>
    ///     Thread-safe event bus implementation for decoupled system communication
    /// </summary>
    public class EventBus : MonoBehaviour, IEventBus
    {
        private readonly object _lock = new();
        private readonly Dictionary<Type, List<Delegate>> _subscriptions = new();

        private void OnDestroy()
        {
            Clear();
        }

        public void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;

            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_subscriptions.ContainsKey(eventType)) _subscriptions[eventType] = new List<Delegate>();

                _subscriptions[eventType].Add(handler);
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;

            lock (_lock)
            {
                var eventType = typeof(T);
                if (_subscriptions.ContainsKey(eventType))
                {
                    _subscriptions[eventType].Remove(handler);

                    // Clean up empty lists
                    if (_subscriptions[eventType].Count == 0) _subscriptions.Remove(eventType);
                }
            }
        }

        public void Publish<T>(T eventData) where T : IGameEvent
        {
            if (eventData == null) return;

            List<Delegate> handlers;
            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_subscriptions.ContainsKey(eventType))
                    return;

                // Create a copy to avoid modification during iteration
                handlers = new List<Delegate>(_subscriptions[eventType]);
            }

            // Execute handlers outside of lock to prevent deadlocks
            foreach (var handler in handlers)
                try
                {
                    ((Action<T>)handler)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in event handler for {typeof(T).Name}: {ex.Message}");
                }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _subscriptions.Clear();
            }
        }
    }
}