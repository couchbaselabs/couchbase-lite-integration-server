using System.Text.Json.Serialization;

namespace IntegrationMacroServer.Models
{
    public class CountEqualRequest
    {
        public int Count { get; set; }
    }

    public class CountEqualResponse
    {
        public bool Success {  get; }

        [JsonPropertyName("actual_count")]
        public int ActualCount { get; }

        public CountEqualResponse(bool success, int actualCount)
        {
            Success = success;
            ActualCount = actualCount;
        }
    }
}
