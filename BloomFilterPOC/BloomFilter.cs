using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BloomFilterPOC
{
    public class BloomFilter
    {
        private const double DefaultFalsePositiveRate = 0.001;
        private int _count;
        private int _targetCapacity;
        private int _hashFunctionCount;
        private double _falsePositiveRate;
        private Func<string, int> _firstHash;
        private Func<string, int> _secondaryHash;
        private BitArray _bitArray;
        private MD5 md5Hasher;
        SHA256Managed sha256hash;

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

        public string BitArray
        {
            get
            {
                byte[] ret = new byte[(this._bitArray.Length - 1) / 8 + 1];
                this._bitArray.CopyTo(ret, 0);
                return Convert.ToBase64String(ret);
            }

            set
            {
                byte[] ret = Convert.FromBase64String(value);
                this._bitArray =new BitArray(ret);
                /*
                var tmp = new BitArray(ret);
                for (int i = 0; i < this._bitArray.Length && i < tmp.Length; i++)
                {
                    this._bitArray.Set(i, tmp.Get(i));
                }
                */
            }
        }

        public BloomFilter(int targetCapacity, double falsePositiveRate)
        {
            this._targetCapacity = targetCapacity;
            this._hashFunctionCount = CalculateHashCount(targetCapacity, falsePositiveRate);
            this._falsePositiveRate = falsePositiveRate;
            this._firstHash = DefaultHash;
            this._secondaryHash = MurMur3Hash; // DefaultHash, MD5Hash, SHA256Hash, MurMur3Hash
            this._bitArray = new BitArray(CalculateBitArrayLength(targetCapacity, falsePositiveRate));
        }

        public bool Contains(string item)
        {
            int hashCode = this._firstHash(item);
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

        public void Add(string item)
        {
            int hashCode = this._firstHash(item);
            this._bitArray.SetValueFromHash(hashCode);
            int hash = this._secondaryHash(item);
            this._bitArray.SetValueFromHash(hash);
            for (int index = 2; index < this._hashFunctionCount; ++index)
                this._bitArray.SetValueFromHash(hashCode + index * hash);
            this._count = this._count + 1;
        }

        private int DefaultHash(string input)
        {
            return input.GetHashCode();
        }

        private int MD5Hash(string input)
        {
            if (md5Hasher == null)
            {
                md5Hasher = MD5.Create();
            }
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToInt32(hashed, 0);
        }

        private int SHA256Hash(string input)
        {
            var message = Encoding.ASCII.GetBytes(input);       
            if (sha256hash == null)
            {
                sha256hash = new SHA256Managed();
            }
            var hashValue = sha256hash.ComputeHash(message);
            return BitConverter.ToInt32(hashValue, 0);
        }

        private int MurMur3Hash(string input)
        {
            var message = Encoding.ASCII.GetBytes(input);
            return MurMurHash3.Hash(message);
        }


        private static int CalculateBitArrayLength(int capacity, double falsePositiveRate)
        {
            return (int)Math.Ceiling((double)capacity * Math.Log(falsePositiveRate, 1.0 / Math.Pow(2.0, Math.Log(2.0))) / 8) * 8;
        }

        private static int CalculateHashCount(int capacity, double falsePositiveRate)
        {
            return (int)Math.Round(Math.Log(2.0) * (double)CalculateBitArrayLength(capacity, falsePositiveRate) / (double)capacity);
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
