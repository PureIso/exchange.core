using System.Text.Json.Serialization;

namespace exchange.core.Models
{
    public class BinanceStatistics
    {
        [JsonPropertyName("symbol")] public string Symbol { get; set; }
        [JsonPropertyName("priceChange")] public string PriceChange { get; set; }
        [JsonPropertyName("priceChangePercent")] public string PriceChangePercentage { get; set; }
        [JsonPropertyName("weightedAvgPrice")] public string WeightedAveragePrice { get; set; }
        [JsonPropertyName("prevClosePrice")] public string PreviousClosePrice { get; set; }
        [JsonPropertyName("lastPrice")] public string LastPrice { get; set; }
        [JsonPropertyName("lastQty")] public string LastQuantity { get; set; }
        [JsonPropertyName("bidPrice")] public string BidPrice { get; set; }
        [JsonPropertyName("bidQty")] public string BidQuantity { get; set; }
        [JsonPropertyName("askPrice")] public string AskPrice { get; set; }
        [JsonPropertyName("askQty")] public string AskQuantity { get; set; }
        [JsonPropertyName("openPrice")] public string OpenPrice { get; set; }
        [JsonPropertyName("highPrice")] public string HighPrice { get; set; }
        [JsonPropertyName("lowPrice")] public string LowPrice { get; set; }
        [JsonPropertyName("volume")] public string Volume { get; set; }
        [JsonPropertyName("quoteVolume")] public string QuoteVolume { get; set; }
        [JsonPropertyName("openTime")] public long OpenTime { get; set; }
        [JsonPropertyName("closeTime")] public long CloseTime { get; set; }
        [JsonPropertyName("firstId")] public long FirstID { get; set; }
        [JsonPropertyName("lastId")] public long LastID { get; set; }
        [JsonPropertyName("count")] public long Count { get; set; }
    }
}
