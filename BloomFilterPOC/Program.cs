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

namespace BloomFilterPOC
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseUri = "https://op-dhs-prod-read-nus.azurewebsites.net/";// "https://op-dhs-sandbox-read.azurewebsites.net/"; //
            string clientName = "integration_test";

            IDocumentHostingService dhsClient = new DocumentHostingServiceClient(new Uri(baseUri), clientName, accessKey);

            Dictionary<string, List<string>> conflicts = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            IList<GetDepotResponse> depots = dhsClient.GetAllDepotsBySiteBasePath("docs", "docs.microsoft.com/dotnet/", null, CancellationToken.None).Result;
            foreach (GetDepotResponse depot in depots)
            {
                if (depot.SystemMetadata.GetValueOrDefault<bool>(MetadataConstants.ActiveKey))
                {
                    string depotName = depot.DepotName;
                    string continueAt = null;
                    Console.WriteLine($"{depotName} Start.");
                    do
                    {
                        try
                        {
                            GetDocumentsResponse documents = dhsClient.GetDocumentsPaginated(depot.DepotName, "en-us", "live", false, continueAt, null, null, CancellationToken.None).Result;
                            continueAt = documents.ContinueAt;
                            foreach (GetDocumentResponse document in documents.Documents)
                            {
                                string canonicalUrl = document.Metadata.GetValueOrDefault<string>("canonical_url");
                                if (!string.IsNullOrEmpty(canonicalUrl))
                                {
                                    if (conflicts.ContainsKey(canonicalUrl))
                                    {
                                        conflicts[canonicalUrl].Add(depotName);
                                    }
                                    else
                                    {
                                        conflicts[canonicalUrl] = new List<string> { depotName };
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        Console.WriteLine("\t\t..................");
                    }
                    while (!string.IsNullOrEmpty(continueAt));

                    Console.WriteLine($"{depotName} Done.");
                }
            }
        }

        public static void Exp1()
        {
            int count = 1000000;

            BloomFilter bf = new BloomFilter(count, 0.001);
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
            var newBf = new BloomFilter(count, 0.001);
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
    }
}
