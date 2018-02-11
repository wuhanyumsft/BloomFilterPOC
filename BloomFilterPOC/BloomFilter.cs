using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloomFilterPOC
{
    public class BloomFilter<T>
    {
        private const double DefaultFalsePositiveRate = 0.001;
        private int _count;
        private int _targetCapacity;
        private int _hashFunctionCount;
        private double _falsePositiveRate;
        private Func<T, int> _secondaryHash;
        private BitArray _bitArray;

        public int TargetCapacity
        {
            get
            {
                return this._targetCapacity;
            }
        }

        public int HashFunctionCount
        {
            get
            {
                return this._hashFunctionCount;
            }
        }

        public double FalsePositiveRate
        {
            get
            {
                return this._falsePositiveRate;
            }
        }

        public int BitLength
        {
            get
            {
                return this._bitArray.Count;
            }
        }

        public int Count
        {
            get
            {
                return this._count;
            }
        }

        public int[] BitArray
        {
            get
            {
                return this._bitArray.ToIntArray();
            }
            set
            {
                this._bitArray = value.FromIntArray();
            }
        }

        public BloomFilter(int targetCapacity, Func<T, int> secondaryHash_NotGetHashCode)
          : this(targetCapacity, BloomFilter<T>.CalculateHashCount(targetCapacity, 0.001), 0.001, secondaryHash_NotGetHashCode)
        {
        }

        public BloomFilter(int targetCapacity, double falsePositiveRate, Func<T, int> secondaryHash_NotGetHashCode)
          : this(targetCapacity, BloomFilter<T>.CalculateHashCount(targetCapacity, falsePositiveRate), falsePositiveRate, secondaryHash_NotGetHashCode)
        {
        }

        public BloomFilter(int targetCapacity, int hashFunctionCount, double falsePositiveRate, Func<T, int> secondaryHash_NotGetHashCode)
        {
            this._targetCapacity = targetCapacity;
            this._hashFunctionCount = hashFunctionCount;
            this._falsePositiveRate = falsePositiveRate;
            this._secondaryHash = secondaryHash_NotGetHashCode;
            this._bitArray = new BitArray(BloomFilter<T>.CalculateBitArrayLength(targetCapacity, falsePositiveRate));
        }

        public bool Contains(T item)
        {
            int hashCode = item.GetHashCode();
            if (!this._bitArray.GetValueFromHash(hashCode))
                return false;
            int hash = this._secondaryHash(item);
            if (!this._bitArray.GetValueFromHash(hash))
                return false;
            for (int index = 2; index < this._hashFunctionCount; ++index)
            {
                if (!this._bitArray.GetValueFromHash(hashCode + index * hash))
                    return false;
            }
            return true;
        }

        public void Add(T item)
        {
            int hashCode = item.GetHashCode();
            this._bitArray.SetValueFromHash(hashCode);
            int hash = this._secondaryHash(item);
            this._bitArray.SetValueFromHash(hash);
            for (int index = 2; index < this._hashFunctionCount; ++index)
                this._bitArray.SetValueFromHash(hashCode + index * hash);
            this._count = this._count + 1;
        }

        private static int CalculateBitArrayLength(int capacity, double falsePositiveRate)
        {
            return (int)Math.Ceiling((double)capacity * Math.Log(falsePositiveRate, 1.0 / Math.Pow(2.0, Math.Log(2.0))));
        }

        private static int CalculateHashCount(int capacity, double falsePositiveRate)
        {
            return (int)Math.Round(Math.Log(2.0) * (double)BloomFilter<T>.CalculateBitArrayLength(capacity, falsePositiveRate) / (double)capacity);
        }
    }

    internal static class Ext
    {
        internal static bool GetValueFromHash(this BitArray THIS, int hash)
        {
            return THIS.Get(Math.Abs(hash) % THIS.Length);
        }

        internal static void SetValueFromHash(this BitArray THIS, int hash)
        {
            THIS.Set(Math.Abs(hash) % THIS.Length, true);
        }

        public static int[] ToIntArray(this BitArray bits)
        {
            int[] baBits = new int[bits.Length / 32 + 1];  // 4 ints makes up 128 bits

            bits.CopyTo(baBits, 0);
            return baBits;
        }

        public static BitArray FromIntArray(this int[] bits)
        {
            return new BitArray(bits);
        }
    }
}
