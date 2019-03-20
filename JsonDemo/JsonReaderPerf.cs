using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Demo
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, targetCount: 5)]
    public class JsonReaderPerf
    {
        private readonly string _fileName = "world_universities_and_domains.json";

        private byte[] _dataUtf8;

        private MemoryStream _memoryStream;
        private StreamReader _streamReader;

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

            string jsonString = File.ReadAllText(fileName);
            _dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            _memoryStream = new MemoryStream(_dataUtf8);
            _streamReader = new StreamReader(_memoryStream, Encoding.UTF8, false, 1024, true);
        }

        [Benchmark(Baseline = true)]
        public (int count, int total) Newtonsoft()
        {
            _memoryStream.Seek(0, SeekOrigin.Begin);

            int count = 0;
            int total = 0;

            using var json = new JsonTextReader(_streamReader);

            while (json.Read())
            {
                JsonToken tokenType = json.TokenType;

                switch (tokenType)
                {
                    case JsonToken.StartObject:
                        total++;
                        break;
                    case JsonToken.PropertyName:
                        if (json.Value.ToString() == "name")
                        {
                            bool result = json.Read();

                            Debug.Assert(result);  // Assume valid JSON
                            Debug.Assert(json.TokenType == JsonToken.String);   // Assume known, valid JSON schema

                            if (json.Value.ToString().StartsWith("University of"))
                            {
                                count++;
                            }
                        }
                        break;
                }
            }
            return (count, total);
        }

        private static readonly byte[] s_nameUtf8 = Encoding.UTF8.GetBytes("name");
        private static readonly byte[] s_universityOfUtf8 = Encoding.UTF8.GetBytes("University of");

        [Benchmark]
        public (int count, int total) TextJson()
        {
            int count = 0;
            int total = 0;

            var json = new Utf8JsonReader(_dataUtf8, isFinalBlock: true, state: default);

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;

                switch (tokenType)
                {
                    case JsonTokenType.StartObject:
                        total++;
                        break;
                    case JsonTokenType.PropertyName:
                        ReadOnlySpan<byte> name = json.HasValueSequence ? json.ValueSequence.ToArray() : json.ValueSpan;
                        int idx = name.IndexOf((byte)'\\');

                        bool isMatch = idx == -1 ? name.SequenceEqual(s_nameUtf8) : (json.GetString() == "name");

                        if (isMatch)
                        {
                            bool result = json.Read();

                            Debug.Assert(result);  // Assume valid JSON
                            Debug.Assert(json.TokenType == JsonTokenType.String);   // Assume known, valid JSON schema

                            ReadOnlySpan<byte> value = json.HasValueSequence ? json.ValueSequence.ToArray() : json.ValueSpan;
                            idx = name.IndexOf((byte)'\\');

                            isMatch = idx == -1 ? value.StartsWith(s_universityOfUtf8) : json.GetString().StartsWith("University of");

                            if (isMatch)
                            {
                                count++;
                            }
                        }
                        break;
                }
            }
            return (count, total);
        }
    }
}
