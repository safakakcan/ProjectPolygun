// API consistent with Microsoft's ObjectPool<T>.
// thread safe.

using System.Runtime.CompilerServices;

namespace Mirror
{
    public static class ConcurrentNetworkWriterPool
    {
        // initial capacity to avoid allocations in the first few frames
        // 1000 * 1200 bytes = around 1 MB.
        public const int InitialCapacity = 1000;


        // reuse ConcurrentPool<T>
        // we still wrap it in NetworkWriterPool.Get/Recycle so we can reset the
        // position before reusing.
        // this is also more consistent with NetworkReaderPool where we need to
        // assign the internal buffer before reusing.
        private static readonly ConcurrentPool<ConcurrentNetworkWriterPooled> pool =
            new(
                // new object function
                () => new ConcurrentNetworkWriterPooled(),
                // initial capacity to avoid allocations in the first few frames
                // 1000 * 1200 bytes = around 1 MB.
                InitialCapacity
            );

        // pool size access for debugging & tests
        public static int Count => pool.Count;

        public static ConcurrentNetworkWriterPooled Get()
        {
            // grab from pool & reset position
            var writer = pool.Get();
            writer.Position = 0;
            return writer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(ConcurrentNetworkWriterPooled writer)
        {
            pool.Return(writer);
        }
    }
}