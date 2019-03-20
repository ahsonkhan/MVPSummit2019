using BenchmarkDotNet.Attributes;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Demo
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, targetCount: 5, invocationCount: 6)]
    public class JsonDocumentPerf
    {
        private readonly string _fileName = "world_universities_and_domains.json";

        private byte[] _dataUtf8;
        private string _jsonString;

        [GlobalSetup]
        public void GlobalSetup()
        {
            string dir = Directory.GetCurrentDirectory();

            string fileName = dir + "\\" + _fileName;

            while (true)
            {
                if (File.Exists(fileName))
                {
                    break;
                }
                dir = Path.GetFullPath(Path.Combine(dir, @"..\"));
                if (dir.EndsWith("MVPSummit2019"))
                {
                    break;
                }
                fileName = dir + "\\" + _fileName;
            }

            _jsonString = File.ReadAllText(fileName);
            _dataUtf8 = Encoding.UTF8.GetBytes(_jsonString);
        }

        [Benchmark(Baseline = true)]
        public (int count, int total) Newtonsoft()
        {
            JArray array = JArray.Parse(_jsonString);

            int count = 0;
            int total = 0;

            foreach (JToken obj in array)
            {
                if (obj.HasValues && obj.Type == JTokenType.Object)
                {
                    if (((string)obj["name"]).StartsWith("University of"))
                    {
                        count++;
                    }
                }
                total++;
            }
            return (count, total);
        }

        [Benchmark]
        public (int count, int total) TextJson()
        {
            using JsonDocument document = JsonDocument.Parse(_dataUtf8);

            int count = 0;
            int total = 0;

            JsonElement root = document.RootElement;
            foreach (JsonElement obj in root.EnumerateArray())
            {
                if (obj.Type == JsonValueType.Object)
                {
                    if (obj.GetProperty("name").GetString().StartsWith("University of"))
                    {
                        count++;
                    }
                }
                total++;
            }
            return (count, total);
        }
    }
}
