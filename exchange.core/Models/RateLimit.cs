using System.Text.Json.Serialization;

namespace exchange.core.Models
{
    public class RateLimit
    {
        [JsonPropertyName("rateLimitType")]
        public string RateLimitType { get; set; }
        [JsonPropertyName("interval")]
        public string Interval { get; set; }
        [JsonPropertyName("intervalNum")]
        public int IntervalNum { get; set; }
        [JsonPropertyName("limit")]
        public int Limit { get; set; }
    }
}
