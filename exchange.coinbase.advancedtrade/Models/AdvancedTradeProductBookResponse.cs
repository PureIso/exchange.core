using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace exchange.coinbase.advancedtrade.models
{
    internal sealed class AdvancedTradeProductBookResponse
    {
        [JsonPropertyName("pricebook")]
        public AdvancedTradePriceBook PriceBook { get; set; }
    }

    internal sealed class AdvancedTradePriceBook
    {
        [JsonPropertyName("product_id")]
        public string ProductId { get; set; }

        [JsonPropertyName("bids")]
        public List<AdvancedTradePriceLevel> Bids { get; set; }

        [JsonPropertyName("asks")]
        public List<AdvancedTradePriceLevel> Asks { get; set; }

        [JsonPropertyName("time")]
        public string Time { get; set; }
    }

    internal sealed class AdvancedTradePriceLevel
    {
        [JsonPropertyName("price")]
        public string Price { get; set; }

        [JsonPropertyName("size")]
        public string Size { get; set; }
    }
}
