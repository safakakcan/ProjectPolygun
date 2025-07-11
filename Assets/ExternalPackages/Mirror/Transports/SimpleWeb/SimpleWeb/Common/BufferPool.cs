using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mirror.SimpleWeb
{
    public interface IBufferOwner
    {
        void Return(ArrayBuffer buffer);
    }

    public sealed class ArrayBuffer : IDisposable
    {
        public readonly byte[] array;
        private readonly IBufferOwner owner;

        /// <summary>
        ///     How many times release needs to be called before buffer is returned to pool
        ///     <para>This allows the buffer to be used in multiple places at the same time</para>
        /// </summary>
        /// <remarks>
        ///     This value is normally 0, but can be changed to require release to be called multiple times
        /// </remarks>
        private int releasesRequired;

        public ArrayBuffer(IBufferOwner owner, int size)
        {
            this.owner = owner;
            array = new byte[size];
        }

        /// <summary>
        ///     number of bytes written to buffer
        /// </summary>
        public int count { get; internal set; }

        public void Dispose()
        {
            Release();
        }

        /// <summary>
        ///     How many times release needs to be called before buffer is returned to pool
        ///     <para>This allows the buffer to be used in multiple places at the same time</para>
        /// </summary>
        public void SetReleasesRequired(int required)
        {
            releasesRequired = required;
        }

        public void Release()
        {
            var newValue = Interlocked.Decrement(ref releasesRequired);
            if (newValue <= 0)
            {
                count = 0;
                owner?.Return(this);
            }
        }

        public void CopyTo(byte[] target, int offset)
        {
            if (count > target.Length + offset)
                throw new ArgumentException($"{nameof(count)} was greater than {nameof(target)}.length", nameof(target));

            Buffer.BlockCopy(array, 0, target, offset, count);
        }

        public void CopyFrom(ArraySegment<byte> segment)
        {
            CopyFrom(segment.Array, segment.Offset, segment.Count);
        }

        public void CopyFrom(byte[] source, int offset, int length)
        {
            if (length > array.Length)
                throw new ArgumentException($"{nameof(length)} was greater than {nameof(array)}.length", nameof(length));

            count = length;
            Buffer.BlockCopy(source, offset, array, 0, length);
        }

        public void CopyFrom(IntPtr bufferPtr, int length)
        {
            if (length > array.Length)
                throw new ArgumentException($"{nameof(length)} was greater than {nameof(array)}.length", nameof(length));

            count = length;
            Marshal.Copy(bufferPtr, array, 0, length);
        }

        public ArraySegment<byte> ToSegment()
        {
            return new ArraySegment<byte>(array, 0, count);
        }

        [Conditional("UNITY_ASSERTIONS")]
        internal void Validate(int arraySize)
        {
            if (array.Length != arraySize)
                Log.Error("[SWT-ArrayBuffer]: Buffer that was returned had an array of the wrong size");
        }
    }

    internal class BufferBucket : IBufferOwner
    {
        public readonly int arraySize;
        private readonly ConcurrentQueue<ArrayBuffer> buffers;

        /// <summary>
        ///     keeps track of how many arrays are taken vs returned
        /// </summary>
        internal int _current;

        public BufferBucket(int arraySize)
        {
            this.arraySize = arraySize;
            buffers = new ConcurrentQueue<ArrayBuffer>();
        }

        public void Return(ArrayBuffer buffer)
        {
            DecrementCreated();
            buffer.Validate(arraySize);
            buffers.Enqueue(buffer);
        }

        public ArrayBuffer Take()
        {
            IncrementCreated();
            if (buffers.TryDequeue(out var buffer))
                return buffer;
            Log.Flood($"[SWT-BufferBucket]: BufferBucket({arraySize}) create new");
            return new ArrayBuffer(this, arraySize);
        }

        [Conditional("DEBUG")]
        private void IncrementCreated()
        {
            var next = Interlocked.Increment(ref _current);
            Log.Flood($"[SWT-BufferBucket]: BufferBucket({arraySize}) count:{next}");
        }

        [Conditional("DEBUG")]
        private void DecrementCreated()
        {
            var next = Interlocked.Decrement(ref _current);
            Log.Flood($"[SWT-BufferBucket]: BufferBucket({arraySize}) count:{next}");
        }
    }

    /// <summary>
    ///     Collection of different sized buffers
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Problem: <br />
    ///         * Need to cached byte[] so that new ones aren't created each time <br />
    ///         * Arrays sent are multiple different sizes <br />
    ///         * Some message might be big so need buffers to cover that size <br />
    ///         * Most messages will be small compared to max message size <br />
    ///     </para>
    ///     <br />
    ///     <para>
    ///         Solution: <br />
    ///         * Create multiple groups of buffers covering the range of allowed sizes <br />
    ///         * Split range exponentially (using math.log) so that there are more groups for small buffers <br />
    ///     </para>
    /// </remarks>
    public class BufferPool
    {
        private readonly int bucketCount;
        internal readonly BufferBucket[] buckets;
        private readonly int largest;
        private readonly int smallest;

        public BufferPool(int bucketCount, int smallest, int largest)
        {
            if (bucketCount < 2) throw new ArgumentException("Count must be at least 2");
            if (smallest < 1) throw new ArgumentException("Smallest must be at least 1");
            if (largest < smallest) throw new ArgumentException("Largest must be greater than smallest");

            this.bucketCount = bucketCount;
            this.smallest = smallest;
            this.largest = largest;

            // split range over log scale (more buckets for smaller sizes)
            var minLog = Math.Log(this.smallest);
            var maxLog = Math.Log(this.largest);
            var range = maxLog - minLog;
            var each = range / (bucketCount - 1);

            buckets = new BufferBucket[bucketCount];

            for (var i = 0; i < bucketCount; i++)
            {
                var size = smallest * Math.Pow(Math.E, each * i);
                buckets[i] = new BufferBucket((int)Math.Ceiling(size));
            }

            Validate();

            // Example
            // 5         count
            // 20        smallest
            // 16400     largest

            // 3.0       log 20
            // 9.7       log 16400

            // 6.7       range 9.7 - 3
            // 1.675     each  6.7 / (5-1)

            // 20        e^ (3 + 1.675 * 0)
            // 107       e^ (3 + 1.675 * 1)
            // 572       e^ (3 + 1.675 * 2)
            // 3056      e^ (3 + 1.675 * 3)
            // 16,317    e^ (3 + 1.675 * 4)

            // precision wont be lose when using doubles
        }

        [Conditional("UNITY_ASSERTIONS")]
        private void Validate()
        {
            if (buckets[0].arraySize != smallest)
                Log.Error("[SWT-BufferPool]: BufferPool Failed to create bucket for smallest. bucket:{0} smallest:{1}", buckets[0].arraySize, smallest);

            var largestBucket = buckets[bucketCount - 1].arraySize;
            // rounded using Ceiling, so allowed to be 1 more that largest
            if (largestBucket != largest && largestBucket != largest + 1)
                Log.Error("[SWT-BufferPool]: BufferPool Failed to create bucket for largest. bucket:{0} smallest:{1}", largestBucket, largest);
        }

        public ArrayBuffer Take(int size)
        {
            if (size > largest)
                throw new ArgumentException($"Size ({size}) is greater than largest ({largest})");

            for (var i = 0; i < bucketCount; i++)
                if (size <= buckets[i].arraySize)
                    return buckets[i].Take();

            throw new ArgumentException($"Size ({size}) is greater than largest ({largest})");
        }
    }
}