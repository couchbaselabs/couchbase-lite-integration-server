using System.Text.Json.Serialization;

namespace IntegrationMacroServer.Models.SyncGateway
{
    public sealed class Vendor
    {
        public string? Name { get; set; }

        public string? Version { get; set; }
    }

    public class RootResponse
    {
        public string? CouchDb { get; set; }

        public Vendor? Vendor { get; set; }

        public string? Version { get; set; }

        [JsonPropertyName("persistent_config")]
        public bool PersistentConfig { get; set; }
    }

    public class AllDocsValue
    {
        public string? Rev { get; set; }
    }

    public class AllDocsRow
    {
        public string? Key { get; set; }

        public string? Id { get; set; }

        public AllDocsValue? Value { get; set; }

        [JsonPropertyName("doc")]
        public IReadOnlyDictionary<string, object>? Document { get; set; }

        public string? RevId => Value?.Rev;
    }

    public class AllDocsResponse
    {
        [JsonPropertyName("total_rows")]
        public int TotalRows { get; set; }

        [JsonPropertyName("update_seq")]
        public int UpdateSequence { get; set; }
    }

    public sealed class BulkDocsResponseEntry
    {
        public string? Id { get; set; }

        public string? Rev { get; set; }
    }
}
