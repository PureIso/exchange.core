using System.Text.Json.Serialization;

namespace exchange.binance.models
{
    public class BinanceData
    {
        [JsonPropertyName("e")] public string EventType { get; set; }

        [JsonPropertyName("E")] public long EventTime { get; set; }

        [JsonPropertyName("s")] public string Symbol { get; set; }

        [JsonPropertyName("a")] public long AggregatedTradeID { get; set; }

        [JsonPropertyName("p")] public string Price { get; set; }

        [JsonPropertyName("q")] public string Quality { get; set; }

        [JsonPropertyName("f")] public int FirstTradeID { get; set; }

        [JsonPropertyName("l")] public int LastTradeID { get; set; }

        [JsonPropertyName("T")] public long TradeID { get; set; }

        [JsonPropertyName("m")] public bool IsBuyerMarketMaker { get; set; }

        [JsonPropertyName("M")] public bool Ignore { get; set; }
    }
}