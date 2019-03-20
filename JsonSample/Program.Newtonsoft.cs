using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Sample
{
    public partial class Program
    {
        private static string NewtonsoftReader(string jsonString)
        {
            string message = "";
            using var json = new JsonTextReader(new StringReader(jsonString));
            if (!json.Read() || json.TokenType != JsonToken.StartObject)
            {
                throw new ArgumentException();
            }
            while (json.Read())
            {
                if (json.TokenType == JsonToken.PropertyName)
                {
                    message = json.ReadAsString();
                }
                else if (json.TokenType == JsonToken.EndObject)
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

        private static string NewtonsoftDocument(string jsonString)
        {
            JObject document = JObject.Parse(jsonString);
            JToken message = document.GetValue("message");
            return message.ToString();
        }




        private static void NewtonsoftWriter(StringWriter output)
        {
            using var json = new JsonTextWriter(output)
            {
                Formatting = Formatting.Indented
            };

            json.WriteStartObject();
            json.WritePropertyName("message");
            json.WriteValue("Hello, World!");
            json.WriteEndObject();
        }

        private static string NewtonsoftSerializer(string jsonString)
        {
            HelloWorld obj = JsonConvert.DeserializeObject<HelloWorld>(jsonString);
            Console.WriteLine($"Original Message: {obj.message}");
            obj.message = "Hello, MVPs!";

            string json = JsonConvert.SerializeObject(obj);
            return json;
        }
    }
}
