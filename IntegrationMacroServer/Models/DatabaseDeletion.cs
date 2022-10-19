using System.Text.Json.Serialization;

namespace IntegrationMacroServer.Models
{
    public sealed class DatabaseDeletion
    {
        [JsonPropertyName("delete_bucket")]
        public bool DeleteBucket { get; set; }

        [JsonPropertyName("delete_collections")]
        public bool DeleteCollections { get; set; }
    }
}
