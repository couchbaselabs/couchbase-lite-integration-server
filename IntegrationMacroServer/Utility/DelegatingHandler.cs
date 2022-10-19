using System.Net.Http.Headers;
using System.Text;

namespace IntegrationMacroServer.Utility
{
    public class AuthHandler : DelegatingHandler
    {
        public AuthHandler()
        {
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var combined = $"{SyncGateway.Settings.Username}:{SyncGateway.Settings.Password}";

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(combined)));

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
