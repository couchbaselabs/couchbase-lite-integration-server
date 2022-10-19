using IntegrationMacroServer.Models;
using IntegrationMacroServer.Models.SyncGateway;
using Refit;
using System.Net.Http.Headers;
using System.Text;

namespace IntegrationMacroServer.Utility
{
    using GetDatabaseResponse = PutDatabaseRequest;

    public sealed class SgwConfig
    {
        public string? Url { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }
    }

    public static class SyncGateway
    {
        public static readonly ISyncGatewayClient Instance;
        public static readonly SgwConfig Settings;

        static SyncGateway()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true)
                .Build()
                ?? throw new ApplicationException("appsettings.json not found");

            var section = config.GetSection("SGW")
                ?? throw new ApplicationException("No cluster configuration present in appsettings.json");

            Settings = section.Get<SgwConfig>()
                ?? throw new ApplicationException("Corrupt configuration for sync gateway");

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri($"http://{Settings.Url!}:4985")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                EncodeBasicAuth(Settings.Username!, Settings.Password!));

            Instance = RestService.For<ISyncGatewayClient>(httpClient);
        }

        private static string EncodeBasicAuth(string username, string password)
        {
            var combined = $"{username}:{password}";
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(combined));
        }
    }

    public sealed class AllDocsQueryParams
    {
        [AliasAs("include_docs")]
        public bool? IncludeDocs { get; set; }
    }

    public interface ISyncGatewayClient
    {
        [Get("/")]
        Task<RootResponse> GetRoot();

        [Put("/{db}/")]
        Task PutDatabase(string db, [Body]PutDatabaseRequest body);

        [Get("/{db}/_config")]
        Task<GetDatabaseResponse> GetDatabase(string db);

        [Delete("/{db}/")]
        Task DeleteDatabase(string db);

        [Post("/{db}/_bulk_docs")]
        Task<IReadOnlyList<BulkDocsResponseEntry>> BulkDocs(string db, [Body]DatabaseAddDocuments body);

        [Post("/{db}/_flush")]
        Task FlushDatabase(string db);

        [Post("/{db}/_user/")]
        Task CreateUser(string db, [Body]CreateUserRequest body);

        [Put("/{db}/_user/GUEST")]
        Task ManageGuestAccess(string db, [Body]GuestAccessRequest body);

        [Get("/{db}/_all_docs")]
        Task<AllDocsResponse> GetAllDocs(string db, AllDocsQueryParams? queryParams = null);

        [Get("/{db}/_user/")]
        Task<IReadOnlyList<string>> GetUsers(string db);

        [Delete("/{db}/_user/{name}")]
        Task DeleteUser(string db, string name);
    }
}
