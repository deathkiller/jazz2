using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Import
{
    public static class Pair
    {
#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static Pair<T1, T2> Create<T1, T2>(T1 first, T2 second)
        {
            return new Pair<T1, T2>(first, second);
        }
    }

    [Serializable]
#if DEBUG
    [DebuggerDisplay("First = {First}, Second = {Second}")]
#endif
    public struct Pair<T1, T2> : IEquatable<Pair<T1, T2>>
    {
        private static readonly bool t1IsValueType = typeof(T1).IsValueType;
        private static readonly bool t2IsValueType = typeof(T2).IsValueType;

        private readonly T1 first;
        private readonly T2 second;

        public T1 First
        {
            get { return first; }
        }

        public T2 Second
        {
            get { return second; }
        }

        public Pair(T1 first, T2 second)
        {
            this.first = first;
            this.second = second;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Pair<T1, T2>)) {
                return false;
            }

            return Equals((Pair<T1, T2>)obj);
        }

        public bool Equals(Pair<T1, T2> other)
        {
            bool firstEqual = (!t1IsValueType && first == null && other.first == null) ||
                              ((t1IsValueType || (first != null && other.first != null)) &&
                               first.Equals(other.first));
            if (!firstEqual) {
                return false;
            }

            bool secondEqual = (!t2IsValueType && second == null && other.second == null) ||
                               ((t2IsValueType || (second != null && other.second != null)) &&
                                second.Equals(other.second));
            return secondEqual;
        }

        public override int GetHashCode()
        {
            int firstHash;
            if (!t1IsValueType && first == null) {
                firstHash = 0;
            } else {
                firstHash = first.GetHashCode();
            }

            int secondHash;
            if (!t2IsValueType && second == null) {
                secondHash = 0;
            } else {
                secondHash = second.GetHashCode();
            }

            return (firstHash << 5) + firstHash ^ secondHash;
        }

        public static bool operator ==(Pair<T1, T2> lhs, Pair<T1, T2> rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Pair<T1, T2> lhs, Pair<T1, T2> rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static implicit operator Tuple<T1, T2>(Pair<T1, T2> from)
        {
            return new Tuple<T1, T2>(from.First, from.Second);
        }

        public static implicit operator Pair<T1, T2>(Tuple<T1, T2> from)
        {
            return new Pair<T1, T2>(from.Item1, from.Item2);
        }
    }
}