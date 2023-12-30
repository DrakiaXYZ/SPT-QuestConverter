using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace QuestConverter014
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Drag/drop a quests.json file onto the exe");
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
                return;
            }

            var inputFile = args[0];
            var outputFile = Path.ChangeExtension(inputFile, ".fixed.json");

            using (StreamReader reader = new StreamReader(inputFile))
            {
                var jsonText = reader.ReadToEnd();
                var jsonData = JsonNode.Parse(jsonText).AsObject();

                foreach (var quest in jsonData)
                {
                    // Add missing `declinePlayerMessage` property
                    quest.Value["declinePlayerMessage"] = $"{quest.Key} declinePlayerMessage";

                    // We need to handle nested properties, so use recursive functions to handle the object
                    HandleJsonObject(quest.Value.AsObject());
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                };
                var fixedJsonText = JsonSerializer.Serialize(jsonData, jsonOptions);
                using (StreamWriter writer = new StreamWriter(outputFile))
                {
                    writer.Write(fixedJsonText);
                }
            }

            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        static void HandleJsonArray(JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item is JsonObject)
                {
                    HandleJsonObject(item.AsObject());
                }
                else if (item is JsonArray)
                {
                    HandleJsonArray(item.AsArray());
                }
            }
        }

        static void HandleJsonObject(JsonObject jsonObject)
        {
            // `_parent` was renamed to `ConditionType`
            if (jsonObject.ContainsKey("_parent"))
            {
                jsonObject["conditionType"] = jsonObject["_parent"].ToString();
                jsonObject.Remove("_parent");
            }

            // Everything in `props` was moved up a level
            if (jsonObject.ContainsKey("_props"))
            {
                var propsObject = jsonObject["_props"].AsObject();
                foreach (var prop in propsObject.ToList())
                {
                    propsObject.Remove(prop.Key);
                    jsonObject[prop.Key] = prop.Value;
                }
                jsonObject.Remove("_props");
            }

            // Look for any nested objects/arrays to handle
            foreach (var nestedObject in jsonObject)
            {
                if (nestedObject.Value is JsonObject)
                {
                    HandleJsonObject(nestedObject.Value.AsObject());
                }
                else if (nestedObject.Value is JsonArray)
                {
                    HandleJsonArray(nestedObject.Value.AsArray());
                }
            }

            // Order the object by key for consistency
            var orderedObject = jsonObject.OrderBy(x => x.Key).ToList();
            jsonObject.Clear();
            foreach (var item in orderedObject)
            {
                jsonObject.Add(item.Key, item.Value);
            }
        }
    }
}
