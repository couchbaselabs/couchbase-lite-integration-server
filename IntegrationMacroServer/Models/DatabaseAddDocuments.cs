using System.Text.Json.Serialization;

namespace IntegrationMacroServer.Models
{
    public sealed class DatabaseAddDocuments : IValidatable
    {
        [JsonPropertyName("docs")]
        public IReadOnlyList<IReadOnlyDictionary<string, object>>? Documents { get; set; }

        public bool IsValid => Documents != null;
    }
}
