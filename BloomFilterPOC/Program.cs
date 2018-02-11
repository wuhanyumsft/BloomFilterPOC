using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BloomFilterDotNet;
using System.Security.Cryptography;

namespace BloomFilterPOC
{
    class Program
    {
        static void Main(string[] args)
        {
            int count = 1000000;
            long previousMemory = GC.GetTotalMemory(true);
            BloomFilter<string> bf = new BloomFilter<string>(count, 0.01, SecondaryHash);
            for (int i = 0; i < count; i++)
            {
                bf.Add(i.ToString());
            }
            int correntNumber = 0;
            for (int i = 0; i < count; i++)
            {
                if (bf.Contains(i.ToString()))
                {
                    correntNumber++;
                }
            }
            Console.WriteLine($"{correntNumber} : {count}");
            Console.WriteLine($"Memory : {(GC.GetTotalMemory(true) - previousMemory) / 1024} KB");
            while (true)
            {
                var input = Console.ReadLine();
                Console.WriteLine(bf.Contains(input));
            };
        }

        private static int SecondaryHash(string input)
        {
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToInt32(hashed, 0);
        }
    }
}
