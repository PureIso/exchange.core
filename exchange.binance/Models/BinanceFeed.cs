using System.Text.Json.Serialization;

namespace exchange.binance.models
{
    public class BinanceFeed
    {
        [JsonPropertyName("best_bid")] public string BestBid { get; set; }
        [JsonPropertyName("best_ask")] public string BestAsk { get; set; }
        [JsonPropertyName("side")] public string Side { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("data")] public BinanceData BinanceData { get; set; }
        [JsonPropertyName("stream")] public string Stream { get; set; }
    }
}
