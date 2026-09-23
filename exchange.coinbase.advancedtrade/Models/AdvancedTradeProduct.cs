using System.Text.Json.Serialization;

namespace exchange.coinbase.advancedtrade.models
{
    internal sealed class AdvancedTradeProduct
    {
        [JsonPropertyName("product_id")]
        public string ProductId { get; set; }

        [JsonPropertyName("base_currency_id")]
        public string BaseCurrencyId { get; set; }

        [JsonPropertyName("quote_currency_id")]
        public string QuoteCurrencyId { get; set; }

        [JsonPropertyName("base_min_size")]
        public string BaseMinSize { get; set; }

        [JsonPropertyName("base_max_size")]
        public string BaseMaxSize { get; set; }

        [JsonPropertyName("quote_increment")]
        public string QuoteIncrement { get; set; }

        [JsonPropertyName("price")]
        public string Price { get; set; }

        [JsonPropertyName("price_percentage_change_24h")]
        public string PricePercentageChange24Hour { get; set; }

        [JsonPropertyName("volume_24h")]
        public string Volume24Hour { get; set; }

        [JsonPropertyName("high_24h")]
        public string High24Hour { get; set; }

        [JsonPropertyName("low_24h")]
        public string Low24Hour { get; set; }
    }
}
