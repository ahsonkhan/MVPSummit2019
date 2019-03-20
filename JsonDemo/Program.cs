using BenchmarkDotNet.Running;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Demo
{
    class Program
    {
        // The JSON data used for the samples was borrowed from https://github.com/Hipo/university-domains-list
        // under the MIT License (MIT).
        /*
        [
            {
                "web_pages": ["https://www.cstj.qc.ca", "https://ccmt.cstj.qc.ca", "https://ccml.cstj.qc.ca"],
                "alpha_two_code": "CA",
                "state-province": null,
                "country": "Canada",
                "domains": ["cstj.qc.ca"],
                "name": "C\u00e9gep de Saint-J\u00e9r\u00f4me"
            },
            ...
            ,
            {
                "web_pages": ["http://www.dhbw-mannheim.de/"],
                "alpha_two_code": "DE",
                "state-province": "null",
                "country": "Germany",
                "domains": ["dhbw-mannheim.de"],
                "name": "Duale Hochschule Baden-Wuerttemberg Mannheim"
            }
        ]
        */

        public static (int count, int total) CountUniversityOf_Demo(ReadOnlySpan<byte> dataUtf8)
        {
            int count = 0;
            int total = 0;

            // 1. Create reader
            // 2. Reader loop
            // 3. Increment total universities on start object
            // 4. Check if property name is "name" and value starts with "University of", increment counter

            return (count, total);
        }

        public static (int count, int total) CountUniversityOf(ReadOnlySpan<byte> dataUtf8)
        {
            int count = 0;
            int total = 0;

            // 1. Create reader
            // 2. Reader loop
            // 3. Increment total universities on start object
            // 4. Check if property name is "name" and value starts with "University of", increment counter

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);

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

        private static readonly byte[] s_nameUtf8 = Encoding.UTF8.GetBytes("name");
        private static readonly byte[] s_universityOfUtf8 = Encoding.UTF8.GetBytes("University of");

        public static /*async Task*/ void Main(string[] args)
        {
            if (args.Length > 0)
            {
                BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
                return;
            }

            // The JSON data used for the samples was borrowed from https://github.com/Hipo/university-domains-list
            // under the MIT License (MIT).

            Console.WriteLine("Reading JSON from file, sync.");
            string outputMessage = SyncFileExample("world_universities_and_domains.json");
            Console.WriteLine(outputMessage);
            Console.WriteLine();

            //Console.WriteLine("Reading JSON from web, async.");
            //outputMessage = await AsyncWebExample(@"http://universities.hipolabs.com/search?", worldWide: true);
            //Console.WriteLine(outputMessage);
            //Console.WriteLine();

            //Console.WriteLine("Reading JSON from web, async.");
            //outputMessage = await AsyncWebExample(@"http://universities.hipolabs.com/search?country=United%20States");
            //Console.WriteLine(outputMessage);
            //Console.WriteLine();

            ReadWrite(GetUtf8JsonFromDisk("world_universities_and_domains.json"), "world_universities_and_domains.json");
        }

        private static string SyncFileExample(string fileName)
        {
            // Follow the async web example if you want to read asynchronously from a FileStream instead.

            ReadOnlySpan<byte> dataWorld = GetUtf8JsonFromDisk(fileName);
            (int count, int total) = CountUniversityOf(dataWorld);
            double ratio = (double)count / total;
            return $"{count} out of {total} universities worldwide have names starting with 'University of' (i.e. {ratio.ToString("#.##%")})!";
        }

        private static ReadOnlySpan<byte> GetUtf8JsonFromDisk(string fileName)
        {
            // Read as UTF-16 and transcode to UTF-8 to return as a Span<byte>
            // For example:
            // string jsonString = File.ReadAllText(fileName);
            // return Encoding.UTF8.GetBytes(jsonString);

            (_, string file) = FindFullPathUpToRoot(fileName);

            // OR ReadAllBytes if the file encoding is known to be UTF-8 and skip the encoding step:
            byte[] jsonBytes = File.ReadAllBytes(file);
            return jsonBytes;
        }

        private static async Task<string> AsyncWebExample(string url, bool worldWide = false)
        {
            using (var client = new HttpClient())
            {
                using (Stream stream = await client.GetStreamAsync(url))
                {
                    (int count, int total) = await ReadJsonFromStreamUsingSpan(stream);

                    double ratio = (double)count / total;
                    string percentage = ratio.ToString("#.##%");
                    string outputMessage = worldWide ?
                        $"{count} out of {total} universities worldwide have names starting with 'University of' (i.e. {percentage})!" :
                        $"{count} out of {total} American universities have names starting with 'University of' (i.e. {percentage})!";

                    return outputMessage;
                }
            }
        }

        public static async Task<(int count, int total)> ReadJsonFromStreamUsingSpan(Stream stream)
        {
            // Assumes all JSON strings in the payload are small (say < 500 bytes)
            var buffer = new byte[1_024];
            int count = 0;
            int total = 0;

            JsonReaderState state = default;
            int leftOver = 0;
            int partialCount = 0;
            int partialTotalCount = 0;
            bool foundName = false;

            while (true)
            {
                // The Memory<byte> ReadAsync overload returns ValueTask which is allocation-free
                // if the operation completes synchronously
                int dataLength = await stream.ReadAsync(buffer.AsMemory(leftOver, buffer.Length - leftOver));
                int dataSize = dataLength + leftOver;
                bool isFinalBlock = dataSize == 0;
                (state, partialCount, partialTotalCount) = PartialCountUniversityOf(buffer.AsSpan(0, dataSize), isFinalBlock, ref foundName, state);

                // Based on your scenario and input data, you may need to grow your buffer here
                // It's possible that leftOver == dataSize (if a JSON token is too large)
                // so you need to resize and read more than 1_024 bytes.
                leftOver = dataSize - (int)state.BytesConsumed;
                if (leftOver != 0)
                {
                    buffer.AsSpan(dataSize - leftOver, leftOver).CopyTo(buffer);
                }

                count += partialCount;
                total += partialTotalCount;

                if (isFinalBlock)
                {
                    break;
                }
            }

            return (count, total);
        }

        public static (JsonReaderState state, int count, int total) PartialCountUniversityOf(ReadOnlySpan<byte> dataUtf8, bool isFinalBlock, ref bool foundName, JsonReaderState state)
        {
            int count = 0;
            int total = 0;

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock, state);

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;

                switch (tokenType)
                {
                    case JsonTokenType.StartObject:
                        total++;
                        break;
                    case JsonTokenType.PropertyName:
                        if (json.ValueSpan.SequenceEqual(s_nameUtf8))
                        {
                            foundName = true;
                        }
                        break;
                    case JsonTokenType.String:
                        if (foundName && json.ValueSpan.StartsWith(s_universityOfUtf8))
                        {
                            count++;
                        }
                        foundName = false;
                        break;
                }
            }

            return (json.CurrentState, count, total);
        }

        public static void ReadWrite(ReadOnlySpan<byte> dataUtf8, string originalFileName)
        {
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);

            using var output = new ArrayBufferWriter<byte>();

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = true });
            var writer = new Utf8JsonWriter(output, state);

            bool hasPropertyName = false;
            ReadOnlySpan<byte> name = default;

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;

                ReadOnlySpan<byte> span = json.ValueSpan;

                switch (tokenType)
                {
                    case JsonTokenType.StartArray:
                        if (hasPropertyName)
                        {
                            writer.WriteStartArray(name, escape: false);
                            hasPropertyName = false;
                        }
                        else
                        {
                            writer.WriteStartArray();
                        }
                        break;
                    case JsonTokenType.StartObject:
                        if (hasPropertyName)
                        {
                            writer.WriteStartObject(name, escape: false);
                            hasPropertyName = false;
                        }
                        else
                        {
                            writer.WriteStartObject();
                        }
                        break;
                    case JsonTokenType.EndArray:
                        writer.WriteEndArray();
                        break;
                    case JsonTokenType.EndObject:
                        writer.WriteEndObject();
                        break;
                    case JsonTokenType.Null:
                        if (hasPropertyName)
                        {
                            writer.WriteNull(name, escape: false);
                            hasPropertyName = false;
                        }
                        else
                        {
                            writer.WriteNullValue();
                        }
                        break;
                    case JsonTokenType.Number:
                        double value = json.GetDouble();
                        if (hasPropertyName)
                        {
                            writer.WriteNumber(name, value, escape: false);
                            hasPropertyName = false;
                        }
                        else
                        {
                            writer.WriteNumberValue(value);
                        }
                        break;
                    case JsonTokenType.String:
                        if (hasPropertyName)
                        {
                            writer.WriteString(name, span, escape: false);
                            hasPropertyName = false;
                        }
                        else
                        {
                            writer.WriteStringValue(span, escape: false);
                            hasPropertyName = false;
                        }
                        break;
                    case JsonTokenType.False:
                    case JsonTokenType.True:
                        bool valueBool = json.GetBoolean();
                        if (hasPropertyName)
                        {
                            writer.WriteBoolean(name, valueBool, escape: false);
                            hasPropertyName = false;
                        }
                        else
                        {
                            writer.WriteBooleanValue(valueBool);
                        }
                        break;
                    case JsonTokenType.PropertyName:
                        hasPropertyName = true;
                        name = json.ValueSpan;
                        break;
                    case JsonTokenType.None:
                    case JsonTokenType.Comment:
                        break;
                }
            }

            writer.Flush(isFinalBlock: true);

            (string directory, _) = FindFullPathUpToRoot(originalFileName);

            File.WriteAllBytes(directory + "\\" + "Formatted.json", output.WrittenMemory.ToArray());
        }

        private static (string directory, string filePath) FindFullPathUpToRoot(string originalFileName)
        {
            string dir = Directory.GetCurrentDirectory();

            string file = dir + "\\" + originalFileName;

            while (true)
            {
                if (File.Exists(file))
                {
                    break;
                }
                dir = Path.GetFullPath(Path.Combine(dir, @"..\"));
                if (dir.EndsWith("MVPSummit2019"))
                {
                    break;
                }
                file = dir + "\\" + originalFileName;
            }
            return (dir, file);
        }
    }
}
