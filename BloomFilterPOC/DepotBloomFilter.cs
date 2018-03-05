using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloomFilterPOC
{
    public class DepotBloomFilter
    {
        public string DepotName { get; set; }
        public byte[] BloomFilter { get; set; }
        public int Count { get; set; }
        public double FalsePositiveRate { get; set; }
    }
}
