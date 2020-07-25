using System.Text.Json.Serialization;

namespace exchange.binance.models
{
    public class Symbol
    {
        [JsonPropertyName("symbol")]
        public string ID { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("baseAsset")]
        public string BaseAsset { get; set; }
        [JsonPropertyName("baseAssetPrecision")]
        public int BaseAssetPrecision { get; set; }
        [JsonPropertyName("quoteAsset")]
        public string QuoteAsset { get; set; }
        [JsonPropertyName("quotePrecision")]
        public int QuotePrecision { get; set; }
        [JsonPropertyName("quoteAssetPrecision")]
        public int QuoteAssetPrecision { get; set; }
        [JsonPropertyName("orderTypes")]
        public string[] OrderTypes { get; set; }
        [JsonPropertyName("icebergAllowed")]
        public bool IcebergAllowed { get; set; }
        [JsonPropertyName("ocoAllowed")]
        public bool OcoAllowed { get; set; }
        [JsonPropertyName("isSpotTradingAllowed")]
        public bool IsSpotTradingAllowed { get; set; }
        [JsonPropertyName("isMarginTradingAllowed")]
        public bool IsMarginTradingAllowed { get; set; }
        [JsonPropertyName("filters")]
        public Filter[] Filters { get; set; }
        [JsonPropertyName("permissions")]
        public string[] Permissions { get; set; }
    }
}
