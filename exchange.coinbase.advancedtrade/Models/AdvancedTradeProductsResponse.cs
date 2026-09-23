using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace exchange.coinbase.advancedtrade.models
{
    internal sealed class AdvancedTradeProductsResponse
    {
        [JsonPropertyName("products")]
        public List<AdvancedTradeProduct> Products { get; set; } = new List<AdvancedTradeProduct>();

        [JsonPropertyName("pagination")]
        public AdvancedTradePagination Pagination { get; set; }
    }

    internal sealed class AdvancedTradePagination
    {
        [JsonPropertyName("next_cursor")]
        public string NextCursor { get; set; }

        [JsonPropertyName("has_next")]
        public bool HasNext { get; set; }
    }
}
