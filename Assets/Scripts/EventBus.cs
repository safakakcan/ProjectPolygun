using System;
using System.Collections.Generic;
using Models;

public sealed class EventBus
{
    private static readonly Lazy<EventBus> _instance = new(() => new EventBus());
    public static EventBus Instance => _instance.Value;

    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    private EventBus() { }

    public void Subscribe<T>(Action<T> callback) where T : unmanaged
    {
        var type = typeof(T);
        if (!_subscribers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _subscribers[type] = list;
        }

        if (!list.Contains(callback))
            list.Add(callback);
    }

    public void Unsubscribe<T>(Action<T> callback) where T : IPoolObject, new()
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out var list))
        {
            list.Remove(callback);
            if (list.Count == 0)
                _subscribers.Remove(type);
        }
    }

    public void Publish<T>(T eventData) where T : IPoolObject, new()
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out var list))
        {
            var temp = new List<Delegate>(list);
            foreach (var callback in temp)
            {
                if (callback is Action<T> action)
                    action(eventData);
            }
        }
        ObjectPool<T>.Pool(eventData);
    }

    public void ClearAll()
    {
        _subscribers.Clear();
    }
}
