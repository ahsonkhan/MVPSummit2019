using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sample
{
    public partial class Program
    {
        private static string TextJsonReader(byte[] utf8Json)
        {
            string message = "";
            var json = new Utf8JsonReader(utf8Json, isFinalBlock: true, state: default);
            if (!json.Read() || json.TokenType != JsonTokenType.StartObject)
            {
                throw new ArgumentException();
            }
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.PropertyName)
                {
                    json.Read();
                    message = json.GetString();
                }
                else if (json.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            return message;
        }

        private static string TextJsonDocument(byte[] utf8Json)
        {
            using JsonDocument document = JsonDocument.Parse(utf8Json);
            JsonElement root = document.RootElement;
            JsonElement message = root.GetProperty(Message);
            return message.GetString();
        }

        private static readonly byte[] Message = Encoding.UTF8.GetBytes("message");
        private static readonly byte[] HelloWorld = Encoding.UTF8.GetBytes("Hello, World!");

        private static void TextJsonWriter(IBufferWriter<byte> output)
        {
            JsonWriterState state = new JsonWriterState(
                options: new JsonWriterOptions { Indented = true });

            var json = new Utf8JsonWriter(output, state);

            json.WriteStartObject();
            json.WriteString(Message, HelloWorld);
            json.WriteEndObject();
            json.Flush(isFinalBlock: true);
        }

        private static string TextJsonSerializer(byte[] utf8Json)
        {
            HelloWorld obj = JsonSerializer.Parse<HelloWorld>(utf8Json);
            Console.WriteLine($"Original Message: {obj.message}");
            obj.message = "Hello, MVPs!";

            string json = JsonSerializer.ToString(obj);
            return json;
        }
    }
}
