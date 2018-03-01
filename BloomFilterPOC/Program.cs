using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.Document.Hosting;
using Microsoft.Document.Hosting.RestClient;
using Microsoft.Document.Hosting.RestService.Contract;
using System.Threading;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Bson;
using System.Diagnostics;

namespace BloomFilterPOC
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 1; i < 7; i++)
            {
                Exp1((int)Math.Pow(13, i));
                Console.WriteLine("-----------------------" + i);
            }
            
            // TestDotnerApiBloomFilters(clientName, accessKey);
        }

        public static void TestDotnerApiBloomFilters(string clientName, string accessKey)
        {
            double falsePositiveRate = 0.00001;
            string baseUri = "https://op-dhs-prod-read-nus.azurewebsites.net/";// "https://op-dhs-sandbox-read.azurewebsites.net/"; //
 
            IDocumentHostingService dhsClient = new DocumentHostingServiceClient(new Uri(baseUri), clientName, accessKey);
            IList<BloomFilter> result = new List<BloomFilter>();
            IList<string> assetIds = new List<string>();
            Dictionary<string, List<string>> conflicts = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            IList<GetDepotResponse> depots = dhsClient.GetAllDepotsBySiteBasePath("docs", "docs.microsoft.com/dotnet/", null, CancellationToken.None).Result;
            foreach (GetDepotResponse depot in depots)
            {
                if (depot.SystemMetadata.GetValueOrDefault<bool>(MetadataConstants.ActiveKey))
                {
                    string depotName = depot.DepotName;
                    string continueAt = null;
                    Console.WriteLine($"{depotName} Start.");
                    IEnumerable<GetDocumentResponse> allDocuments = new List<GetDocumentResponse>();
                    int i = 0;
                    do
                    {
                        for (int retry = 0; retry < 3; i++)
                        {
                            try
                            {
                                GetDocumentsResponse documents = dhsClient.GetDocumentsPaginated(depot.DepotName, "en-us", "live", true, continueAt, null, null, CancellationToken.None).Result;
                                continueAt = documents.ContinueAt;
                                allDocuments = allDocuments.Concat(documents.Documents);
                                break;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                Console.WriteLine($"Retry for {retry + 1} times");
                            }
                        }

                        i++;
                        Console.WriteLine($"{i:000} ..................");
                    }
                    while (!string.IsNullOrEmpty(continueAt));
                    Console.WriteLine($"{depotName} Size: {allDocuments.Count()}.");
                    var bloomFilter = new BloomFilter(allDocuments.Count(), falsePositiveRate);
                    foreach (var document in allDocuments)
                    {
                        bloomFilter.Add(document.AssetId);
                        assetIds.Add(document.AssetId);
                    }
                    Console.WriteLine($"{depotName} Bloom Filter Size: {bloomFilter.BitLength / 1024 / 8} KB.");
                    result.Add(bloomFilter);
                    Console.WriteLine($"{depotName} Done.");
                }
            }
            using (StreamWriter file = new StreamWriter(@"output.json", true))
            {
                file.WriteLine(JsonConvert.SerializeObject(result));
            }

            int onlyOneCount = 0;
            var conflictHashDict = new Dictionary<int, int>();
            foreach (var assetId in assetIds)
            {
                var conflictCount = result.Where(r => r.Contains(assetId)).Count();
                if (conflictCount == 1)
                {
                    onlyOneCount++;
                } else
                {
                    if (conflictHashDict.ContainsKey(conflictCount))
                    {
                        conflictHashDict[conflictCount] += 1;
                    }
                    else
                    {
                        conflictHashDict[conflictCount] = 1;
                    }
                }
            }
            Console.WriteLine($"Only one count: {onlyOneCount}, total count: {assetIds.Count()}");
            Console.WriteLine($"Duplicate count: {conflictHashDict.Keys.Count}");
            var output = conflictHashDict.OrderBy(p => p.Value).Reverse();
            using (StreamWriter file = new StreamWriter(@"conflictCount.txt", true))
            {
                foreach (var item in output)
                {
                    file.WriteLine($"{item.Key}: {item.Value}");
                }
            }

            Console.ReadLine();
        }
        
        public static void GenerateDotnetApiBloomFilter(string clientName, string accessKey)
        {
            double falsePositiveRate = 0.00001;
            string baseUri = "https://op-dhs-prod-read-nus.azurewebsites.net/";// "https://op-dhs-sandbox-read.azurewebsites.net/"; //
            
            IDocumentHostingService dhsClient = new DocumentHostingServiceClient(new Uri(baseUri), clientName, accessKey);
            IList<DepotBloomFilter> result = new List<DepotBloomFilter>();
            Dictionary<string, List<string>> conflicts = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            IList<GetDepotResponse> depots = dhsClient.GetAllDepotsBySiteBasePath("docs", "docs.microsoft.com/dotnet/", null, CancellationToken.None).Result;
            foreach (GetDepotResponse depot in depots.Skip(20).Take(1))
            {
                if (depot.SystemMetadata.GetValueOrDefault<bool>(MetadataConstants.ActiveKey))
                {
                    string depotName = depot.DepotName;
                    string continueAt = null;
                    Console.WriteLine($"{depotName} Start.");
                    IEnumerable<GetDocumentResponse> allDocuments = new List<GetDocumentResponse>();
                    int i = 0;
                    do
                    {
                        for (int retry = 0; retry < 3; i++)
                        {
                            try
                            {
                                GetDocumentsResponse documents = dhsClient.GetDocumentsPaginated(depot.DepotName, "en-us", "live", true, continueAt, null, null, CancellationToken.None).Result;
                                continueAt = documents.ContinueAt;
                                allDocuments = allDocuments.Concat(documents.Documents);
                                break;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                Console.WriteLine($"Retry for {retry + 1} times");
                            }
                        }

                        i++;
                        Console.WriteLine($"{i:000} ..................");
                    }
                    while (!string.IsNullOrEmpty(continueAt));
                    Console.WriteLine($"{depotName} Size: {allDocuments.Count()}.");
                    var bloomFilter = new BloomFilter(allDocuments.Count(), falsePositiveRate);
                    foreach (var document in allDocuments)
                    {
                        bloomFilter.Add(document.AssetId);
                    }
                    Console.WriteLine($"{depotName} Bloom Filter Size: {bloomFilter.BitLength / 1024 / 8} KB.");
                    result.Add(new DepotBloomFilter
                    {
                        DepotName = depotName,
                        BloomFilter = bloomFilter.BitArray,
                        Count = allDocuments.Count(),
                        FalsePositiveRate = falsePositiveRate
                    });
                    Console.WriteLine($"{depotName} Done.");
                }
            }
            /*
            using (StreamWriter file = new StreamWriter(@"output.json", true))
            {
                file.WriteLine(JsonConvert.SerializeObject(result));
            }
            */

            MemoryStream ms = new MemoryStream();
            using (BsonWriter writer = new BsonWriter(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, result);
            }

            Console.ReadLine();
        }

        public static void Exp1(int count = 1000000)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            double falsePositveRate = 0.00001;
            int valueCount = 3;

            BloomFilter bf = new BloomFilter(count, falsePositveRate);
            for (int i = 0; i < count; i++)
            {
                bf.Add((i * 3).ToString());
            }
            int correntNumber = 0;
            var output = bf.BitArray;
            // Console.WriteLine(output);
            Console.WriteLine($"Output Length: {output.Length / 1024} KB");
            for (int i = 0; i < count * valueCount; i++)
            {
                if (bf.Contains(i.ToString()))
                {
                    correntNumber++;
                }
            }
            Console.WriteLine($"{correntNumber} : {count}");
            Console.WriteLine($"Memory : {bf.BitLength / 1024 / 8} KB");

            correntNumber = 0;
            var newBf = new BloomFilter(count, falsePositveRate);
            newBf.BitArray = (output);
            for (int i = 0; i < count * 3; i++)
            {
                if (newBf.Contains(i.ToString()))
                {
                    correntNumber++;
                }
            }
            Console.WriteLine($"new bf {correntNumber} : {count}");
            Console.WriteLine($"Hash function number : {newBf.HashFunctionCount}");
            Console.WriteLine($"False positive rate : {newBf.FalsePositiveRate}");
            Console.WriteLine($"Memory : {newBf.BitLength / 1024 / 8} KB");
            timer.Stop();
            Console.WriteLine($"Experiment use {(double)timer.ElapsedMilliseconds / 1000} seconds.");
            Console.WriteLine($"Each hash set computer use {(double)timer.ElapsedMilliseconds / count / (valueCount + 1)} milli seconds.");

            Console.ReadLine();
            /*
            while (true)
            {
                var input = Console.ReadLine();
                Console.WriteLine(bf.Contains(input));
            };
            */
        }
    }
}
