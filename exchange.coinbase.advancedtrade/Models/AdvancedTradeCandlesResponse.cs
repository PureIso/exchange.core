using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace exchange.coinbase.advancedtrade.models
{
    internal sealed class AdvancedTradeCandlesResponse
    {
        [JsonPropertyName("candles")]
        public List<AdvancedTradeCandle> Candles { get; set; }
    }

    internal sealed class AdvancedTradeCandle
    {
        [JsonPropertyName("start")]
        public string Start { get; set; }

        [JsonPropertyName("low")]
        public string Low { get; set; }

        [JsonPropertyName("high")]
        public string High { get; set; }

        [JsonPropertyName("open")]
        public string Open { get; set; }

        [JsonPropertyName("close")]
        public string Close { get; set; }

        [JsonPropertyName("volume")]
        public string Volume { get; set; }
    }
}
