using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Zoo.Configuration;

namespace Zoo.Headless
{
    public static class ConfigurationJson
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IncludeFields = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        public static SimulationSettings Load(string path) => Parse(File.ReadAllText(path));

        public static SimulationSettings Parse(string json)
        {
            // Reject ambiguous duplicate keys before binding to the portable schema.
            using var document = JsonDocument.Parse(json);
            CheckProperties(document.RootElement);
            var settings = JsonSerializer.Deserialize<SimulationSettings>(json, Options) ?? throw new ArgumentException("Configuration is null.");
            settings.CreateDefinitions();
            return settings;
        }

        private static void CheckProperties(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                    {
                        throw new ArgumentException("Duplicate JSON property: " + property.Name);
                    }

                    CheckProperties(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    CheckProperties(item);
                }
            }
        }
    }
}
