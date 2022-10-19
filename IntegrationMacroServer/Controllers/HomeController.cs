using IntegrationMacroServer.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;

namespace IntegrationMacroServer.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public async Task<dynamic> Get()
        {
            string serverStatus, sgwStatus;
            try { 
                var cluster = await CouchbaseCluster.Instance();
                var version = await GetServerVersion().ConfigureAwait(false);
                serverStatus = $"Online ({version})";
            } catch (Exception ex) {
                serverStatus = ex.Message;
            }

            try {
                var rootResult = await SyncGateway.Instance.GetRoot();
                sgwStatus = $"Online ({rootResult.Vendor!.Version})";
            } catch(Exception ex) {
                sgwStatus = ex.Message;
            }

            return new {
                serverStatus = serverStatus,
                sgwStatus = sgwStatus
            };
        }

        private async Task<string> GetServerVersion()
        {
            var url = new Uri($"http://{CouchbaseCluster.Settings.Url}:8091/");
            using(var client = new HttpClient()) {
                client.BaseAddress = url;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        Encoding.ASCII.GetBytes($"{CouchbaseCluster.Settings.Username}:{CouchbaseCluster.Settings.Password}")
                    )
                );

                var response = await client.GetAsync("pools").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadFromJsonAsync<IDictionary<string, object>>();
                if(content == null || !content.ContainsKey("implementationVersion")) {
                    return "Invalid response received from server!";
                }

                return content["implementationVersion"].ToString() ?? "Invalid version received...";
            }
        }
    }
}