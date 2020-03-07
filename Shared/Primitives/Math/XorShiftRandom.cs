using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Duality
{
    /// <summary>
    /// XorShift+ Random Number Generator
    /// © 2018 R. Wildenhaus - Licensed under MIT
    /// </summary>
    public class XorShiftRandom
    {
        private ulong x_;
        private ulong y_;

        private ulong buffer_;
        private ulong bufferMask_;

        public XorShiftRandom()
        {
            x_ = (ulong)Guid.NewGuid().GetHashCode();
            y_ = (ulong)Guid.NewGuid().GetHashCode();
        }

        public XorShiftRandom(ulong seed)
        {
            x_ = seed << 3;
            y_ = seed >> 3;
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public int Next()
        {
            return (NextInt32() & 0x7FFFFFFF);
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public int Next(int max)
        {
            return (NextInt32() & 0x7FFFFFFF) % max;
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public int Next(int min, int max)
        {
            return min + ((NextInt32() & 0x7FFFFFFF) % (max - min));
        }

        public bool NextBool()
        {
            bool _;
            if (bufferMask_ > 0) {
                _ = (buffer_ & bufferMask_) == 0;
                bufferMask_ >>= 1;
                return _;
            }

            ulong temp_x, temp_y;
            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            buffer_ = temp_y + y_;
            x_ = temp_x;
            y_ = temp_y;

            bufferMask_ = 0x8000000000000000;
            return (buffer_ & 0xF000000000000000) == 0;
        }

        public byte NextByte()
        {
            if (bufferMask_ >= 8) {
                byte _ = (byte)buffer_;
                buffer_ >>= 8;
                bufferMask_ >>= 8;
                return _;
            }

            ulong temp_x, temp_y;
            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            buffer_ = temp_y + y_;
            x_ = temp_x;
            y_ = temp_y;

            bufferMask_ = 0x8000000000000;
            return (byte)(buffer_ >>= 8);
        }

        public short NextInt16()
        {
            short _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (short)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        public ushort NextUInt16()
        {
            ushort _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (ushort)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        public int NextInt32()
        {
            int _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (int)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        public uint NextUInt32()
        {
            uint _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (uint)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        public long NextInt64()
        {
            long _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (long)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        public ulong NextUInt64()
        {
            ulong _;
            ulong temp_x, temp_y;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            _ = (ulong)(temp_y + y_);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        public float NextFloat()
        {
            const float FLOAT_UNIT = 1f / (int.MaxValue + 1f);

            float _;
            ulong temp_x, temp_y, temp_z;

            temp_x = y_;
            x_ ^= x_ << 23; temp_y = x_ ^ y_ ^ (x_ >> 17) ^ (y_ >> 26);

            temp_z = temp_y + y_;
            _ = FLOAT_UNIT * (0x7FFFFFFF & temp_z);

            x_ = temp_x;
            y_ = temp_y;

            return _;
        }

        public float NextFloat(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        public Vector3 NextVector3()
        {
            Quaternion rot = Quaternion.Identity;
            rot *= Quaternion.FromAxisAngle(Vector3.UnitZ, NextFloat() * MathF.RadAngle360);
            rot *= Quaternion.FromAxisAngle(Vector3.UnitX, NextFloat() * MathF.RadAngle360);
            rot *= Quaternion.FromAxisAngle(Vector3.UnitY, NextFloat() * MathF.RadAngle360);
            return Vector3.Transform(Vector3.UnitX, rot);
        }

        public Vector3 NextVector3(float x, float y, float z, float w, float h, float d)
        {
            return new Vector3(NextFloat(x, x + w), NextFloat(y, y + h), NextFloat(z, z + d));
        }

        public T OneOf<T>(IEnumerable<T> values)
        {
            return values.ElementAt(Next(values.Count()));
        }

        public T OneOfWeighted<T>(IEnumerable<KeyValuePair<T, float>> weightedValues)
        {
            float totalWeight = weightedValues.Sum(v => v.Value);
            float pickedWeight = NextFloat() * totalWeight;

            foreach (KeyValuePair<T, float> pair in weightedValues) {
                pickedWeight -= pair.Value;
                if (pickedWeight < 0.0f) return pair.Key;
            }

            return default(T);
        }

        public T OneOfWeighted<T>(params KeyValuePair<T, float>[] weightedValues)
        {
            return OneOfWeighted<T>(weightedValues as IEnumerable<KeyValuePair<T, float>>);
        }
    }
}
