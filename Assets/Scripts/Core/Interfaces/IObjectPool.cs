namespace ProjectPolygun.Core.Interfaces
{
    /// <summary>
    ///     Generic object pool interface for performance optimization
    /// </summary>
    /// <typeparam name="T">Type of objects to pool</typeparam>
    public interface IObjectPool<T> where T : class
    {
        /// <summary>
        ///     Get current pool size
        /// </summary>
        int Count { get; }

        /// <summary>
        ///     Get an object from the pool
        /// </summary>
        /// <returns>Pooled object instance</returns>
        T Get();

        /// <summary>
        ///     Return an object to the pool
        /// </summary>
        /// <param name="obj">Object to return</param>
        void Return(T obj);

        /// <summary>
        ///     Clear all objects from the pool
        /// </summary>
        void Clear();

        /// <summary>
        ///     Pre-warm the pool with objects
        /// </summary>
        /// <param name="count">Number of objects to pre-create</param>
        void Prewarm(int count);
    }
}