using System.Text.Json.Serialization;

namespace exchange.binance.models
{
    public class ExchangeInfo
    {
        [JsonPropertyName("timezone")] public string Timezone { get; set; }

        [JsonPropertyName("serverTime")] public long ServerTimeLong { get; set; }

        [JsonPropertyName("rateLimits")] public RateLimit[] RateLimits { get; set; }

        [JsonPropertyName("exchangeFilters")] public string[] ExchangeFilters { get; set; }

        [JsonPropertyName("symbols")] public Symbol[] Symbols { get; set; }
    }
}