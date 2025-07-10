using System.Collections.Generic;
using Models;

public static class ObjectPool<T> where T : IPoolObject, new()
{
    private static readonly Stack<T> _stack = new();

    public static T Get()
    {
        return _stack.Count > 0 ? _stack.Pop() : new T();
    }

    public static void Pool(T obj)
    {
        _stack.Push(obj);
    }
}
