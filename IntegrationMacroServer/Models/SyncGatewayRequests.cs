using System.Text.Json.Serialization;

namespace IntegrationMacroServer.Models.SyncGateway
{
    public class PutDatabaseScopes
    {
        [JsonPropertyName("collections")]
        public IDictionary<string, IDictionary<string, string>> Collections { get; set; }
            = new Dictionary<string, IDictionary<string, string>>();

        public void AddCollection(string collectionName)
        {
            Collections[collectionName] = new Dictionary<string, string>();
        }
    }

    public class ApiEndpoints
    {
        [JsonPropertyName("enable_couchbase_bucket_flush")]
        public bool EnableCouchbaseBucketFlush { get; } = true;
    }

    public class UnsupportedOptions
    {
        [JsonPropertyName("api_endpoints")]
        public ApiEndpoints ApiEndpoints { get; } = new ApiEndpoints();
    }

    public class PutDatabaseRequest
    {
        [JsonPropertyName("num_index_replicas")]
        public int NumIndexReplicas { get; set; }

        [JsonPropertyName("bucket")]
        public string Bucket { get; set; }

        [JsonPropertyName("scopes")]
        public IDictionary<string, PutDatabaseScopes> Scopes { get; set; }
                = new Dictionary<string, PutDatabaseScopes>();

        [JsonPropertyName("unsupported")]
        public UnsupportedOptions UnsupportedOptions { get; } = new UnsupportedOptions();

        public PutDatabaseRequest(string bucket, int numIndexReplicas = 0)
        {
            NumIndexReplicas = numIndexReplicas;
            Bucket = bucket;
        }

        public PutDatabaseScopes GetOrAddScope(string scopeName)
        {
            if (!Scopes.ContainsKey(scopeName)) {
                Scopes[scopeName] = new PutDatabaseScopes();
            }

            return Scopes[scopeName];
        }
    }

    public sealed class CreateUserRequest
    {
        public string Name { get; }

        public string Password { get; }

        [JsonPropertyName("admin_channels")]
        public IList<string> AdminChannels { get; } = new List<string>();

        public CreateUserRequest(string name, string password)
        {
            Name = name;
            Password = password;
        }
    }

    public sealed class GuestAccessRequest
    {
        public bool Disabled { get; set; }

        [JsonPropertyName("admin_channels")]
        public IList<string> AdminChannels { get; } = new List<string>();
    }
}
