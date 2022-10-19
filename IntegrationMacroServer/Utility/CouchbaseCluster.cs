using Couchbase;
using IntegrationMacroServer.Models;

namespace IntegrationMacroServer.Utility
{
    public static class CouchbaseCluster
    {
        private static ICluster? _instance;
        public static readonly ClusterConfig Settings;

        static CouchbaseCluster()
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true)
               .Build()
               ?? throw new ApplicationException("appsettings.json not found");

            var section = config.GetSection("Cluster")
                ?? throw new ApplicationException("No cluster configuration present in appsettings.json");

            Settings = section.Get<ClusterConfig>()
                ?? throw new ApplicationException("Corrupt configuration for cluster");
        }

        public static async Task<ICluster> Instance()
        {
            if(_instance != null) {
                await _instance.WaitUntilReadyAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                return _instance;
            }

            _instance = await Cluster.ConnectAsync($"couchbase://{Settings.Url}",
                Settings.Username!, Settings.Password!).ConfigureAwait(false);

            await _instance.WaitUntilReadyAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            return _instance;
        }
    }
}
