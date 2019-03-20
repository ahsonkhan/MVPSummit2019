using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public class ReadWritePerf
    {
        private readonly string _fileName = "world_universities_and_domains.json";

        private string _dir;

        private byte[] _dataUtf8;

        private MemoryStream _memoryStream;
        private StreamReader _streamReader;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _dir = Directory.GetCurrentDirectory();

            string fileName = _dir + "\\" + _fileName;

            while (true)
            {
                if (File.Exists(fileName))
                {
                    break;
                }
                _dir = Path.GetFullPath(Path.Combine(_dir, @"..\"));
                if (_dir.EndsWith("MVPSummit2019"))
                {
                    break;
                }
                fileName = _dir + "\\" + _fileName;
            }

            string jsonString = File.ReadAllText(fileName);
            _dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            _memoryStream = new MemoryStream(_dataUtf8);
            _streamReader = new StreamReader(_memoryStream, Encoding.UTF8, false, 1024, true);
        }

        //[Benchmark(Baseline = true)]
        public void Newtonsoft_JToken()
        {
            _memoryStream.Seek(0, SeekOrigin.Begin);

            using var jsonReader = new JsonTextReader(_streamReader);

            JToken jtoken = JToken.ReadFrom(jsonReader);
            var stringWriter = new StringWriter();

            using var jsonWriter = new JsonTextWriter(stringWriter)
            {
                Formatting = Formatting.Indented
            };

            jtoken.WriteTo(jsonWriter);
            File.WriteAllText(_dir + "\\" + "Formatted2.json", stringWriter.ToString());
        }

        [Benchmark(Baseline = true)]
        public void Newtonsoft()
        {
            _memoryStream.Seek(0, SeekOrigin.Begin);

            using var json = new JsonTextReader(_streamReader);

            using var file = new StreamWriter(_dir + "\\" + "Formatted2.json");

            using var jsonWriter = new JsonTextWriter(file)
            {
                Formatting = Formatting.Indented
            };

            while (json.Read())
            {
                JsonToken tokenType = json.TokenType;

                switch (tokenType)
                {
                    case JsonToken.StartObject:
                        jsonWriter.WriteStartObject();
                        break;
                    case JsonToken.StartArray:
                        jsonWriter.WriteStartArray();
                        break;
                    case JsonToken.PropertyName:
                        jsonWriter.WritePropertyName((string)json.Value, escape: false);
                        break;
                    case JsonToken.Integer:
                    case JsonToken.Float:
                        jsonWriter.WriteValue((double)json.Value);
                        break;
                    case JsonToken.String:
                        jsonWriter.WriteValue((string)json.Value);
                        break;
                    case JsonToken.Boolean:
                        jsonWriter.WriteValue((bool)json.Value);
                        break;
                    case JsonToken.Null:
                        jsonWriter.WriteNull();
                        break;
                    case JsonToken.EndObject:
                        jsonWriter.WriteEndObject();
                        break;
                    case JsonToken.EndArray:
                        jsonWriter.WriteEndArray();
                        break;
                    case JsonToken.EndConstructor:
                    case JsonToken.Date:
                    case JsonToken.Bytes:
                    case JsonToken.StartConstructor:
                    case JsonToken.Comment:
                    case JsonToken.Raw:
                    case JsonToken.None:
                    case JsonToken.Undefined:
                        break;
                }
            }

            jsonWriter.Flush();
        }

        [Benchmark]
        public void TextJson()
        {
            ReadWrite(_dataUtf8, _dir);
        }

        const int SyncWriteThreshold = 1_000_000;

        public static void ReadWrite(ReadOnlySpan<byte> dataUtf8, string directory)
        {
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);

            using var fileStream = File.OpenWrite(directory + "\\" + "Formatted.json");

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

                if (writer.BytesWritten > SyncWriteThreshold)
                {
                    writer.Flush(isFinalBlock: false);
                    fileStream.Write(output.WrittenMemory.Span);
                    output.Clear();
                    writer = new Utf8JsonWriter(output, writer.GetCurrentState());
                }
            }

            if (writer.BytesWritten != 0)
            {
                writer.Flush(isFinalBlock: true);
                fileStream.Write(output.WrittenMemory.Span);
            }

            //File.WriteAllBytes(directory + "\\" + "Formatted.json", output.WrittenMemory.ToArray());
        }
    }
}
