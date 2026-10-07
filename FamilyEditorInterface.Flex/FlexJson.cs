using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FamilyEditorInterface.Flex
{
    /// <summary>JSON for reports (out) and plans (in and out). camelCase, enums as strings, nulls omitted.</summary>
    public static class FlexJson
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static string Serialize(FlexReport report) => JsonSerializer.Serialize(report, Options);
        public static string Serialize(FlexPlan plan) => JsonSerializer.Serialize(plan, Options);

        public static FlexPlan DeserializePlan(string json) => JsonSerializer.Deserialize<FlexPlan>(json, Options);

        /// <summary>Writes the report to a file, creating the folder if needed.</summary>
        public static void Write(FlexReport report, string path)
        {
            string folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(path, Serialize(report));
        }
    }
}
