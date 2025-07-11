#pragma warning disable CS0659 // 'Vector3Long' overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning disable CS0661 // 'Vector3Long' defines operator == or operator != but does not override Object.GetHashCode()

// Vector3Long by mischa (based on game engine project)
using System;
using System.Runtime.CompilerServices;

namespace Mirror
{
    public struct Vector3Long
    {
        public long x;
        public long y;
        public long z;

        public static readonly Vector3Long zero = new(0, 0, 0);
        public static readonly Vector3Long one = new(1, 1, 1);
        public static readonly Vector3Long forward = new(0, 0, 1);
        public static readonly Vector3Long back = new(0, 0, -1);
        public static readonly Vector3Long left = new(-1, 0, 0);
        public static readonly Vector3Long right = new(1, 0, 0);
        public static readonly Vector3Long up = new(0, 1, 0);
        public static readonly Vector3Long down = new(0, -1, 0);

        // constructor /////////////////////////////////////////////////////////
        public Vector3Long(long x, long y, long z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // operators ///////////////////////////////////////////////////////////
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Long operator +(Vector3Long a, Vector3Long b)
        {
            return new Vector3Long(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Long operator -(Vector3Long a, Vector3Long b)
        {
            return new Vector3Long(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Long operator -(Vector3Long v)
        {
            return new Vector3Long(-v.x, -v.y, -v.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Long operator *(Vector3Long a, long n)
        {
            return new Vector3Long(a.x * n, a.y * n, a.z * n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Long operator *(long n, Vector3Long a)
        {
            return new Vector3Long(a.x * n, a.y * n, a.z * n);
        }

        // == returns true if approximately equal (with epsilon).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector3Long a, Vector3Long b)
        {
            return a.x == b.x &&
                   a.y == b.y &&
                   a.z == b.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector3Long a, Vector3Long b)
        {
            return !(a == b);
        }

        // NO IMPLICIT System.Numerics.Vector3Long conversion because double<->float
        // would silently lose precision in large worlds.

        // [i] component index. useful for iterating all components etc.
        public long this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    default: throw new IndexOutOfRangeException($"Vector3Long[{index}] out of range.");
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    default: throw new IndexOutOfRangeException($"Vector3Long[{index}] out of range.");
                }
            }
        }

        // instance functions //////////////////////////////////////////////////
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"({x} {y} {z})";
        }

        // equality ////////////////////////////////////////////////////////////
        // implement Equals & HashCode explicitly for performance.
        // calling .Equals (instead of "==") checks for exact equality.
        // (API compatibility)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector3Long other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        // Equals(object) can reuse Equals(Vector4)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            return other is Vector3Long vector4 && Equals(vector4);
        }

#if UNITY_2021_3_OR_NEWER
        // Unity 2019/2020 don't have HashCode.Combine yet.
        // this is only to avoid reflection. without defining, it works too.
        // default generated by rider
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }
#endif
    }
}