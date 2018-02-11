using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace BloomFilterPOC
{
    class Program
    {
        static void Main(string[] args)
        {
            int count = 1000000;

            BloomFilter<string> bf = new BloomFilter<string>(count, 0.001, SecondaryHash);
            for (int i = 0; i < count; i++)
            {
                bf.Add((i * 3).ToString());
            }
            int correntNumber = 0;
            var output = bf.BitArray;
            var outputStr = string.Join(",", output);
            Console.WriteLine(outputStr);
            Console.WriteLine($"Output : {outputStr.Length * 8 / 1024} KB");
            for (int i = 0; i < count * 3; i++)
            {
                if (bf.Contains(i.ToString()))
                {
                    correntNumber++;
                }
            }
            Console.WriteLine($"{correntNumber} : {count}");
            Console.WriteLine($"Memory : {bf.BitLength / 1024} KB");

            correntNumber = 0;
            var newBf = new BloomFilter<string>(count, 0.001, SecondaryHash);
            newBf.BitArray = output;
            for (int i = 0; i < count * 3; i++)
            {
                if (bf.Contains(i.ToString()))
                {
                    correntNumber++;
                }
            }
            Console.WriteLine($"new bf {correntNumber} : {count}");
            Console.WriteLine($"Memory : {newBf.BitLength / 1024} KB");

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
