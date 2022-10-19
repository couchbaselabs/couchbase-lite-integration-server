using IntegrationMacroServer.Models;
using IntegrationMacroServer.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace IntegrationMacroServer.Controllers
{
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly ISyncGatewayClient _syncGatewayClient = SyncGateway.Instance;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{db}/countEqual")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<CountEqualResponse>> TestCountEqual(string db, CountEqualRequest request)
        {
            var allDocs = await _syncGatewayClient.GetAllDocs(db).ConfigureAwait(false);
            var success = request.Count == allDocs.TotalRows;
            return Ok(new CountEqualResponse(success, allDocs.TotalRows));
        }
    }                                 
}
