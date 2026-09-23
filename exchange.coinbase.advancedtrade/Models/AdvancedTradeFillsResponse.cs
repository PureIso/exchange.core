using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace exchange.coinbase.advancedtrade.models
{
    internal sealed class AdvancedTradeFillsResponse
    {
        [JsonPropertyName("fills")]
        public List<AdvancedTradeFill> Fills { get; set; }

        [JsonPropertyName("cursor")]
        public string Cursor { get; set; }

        [JsonPropertyName("proof_token_required")]
        public bool ProofTokenRequired { get; set; }
    }

    internal sealed class AdvancedTradeFill
    {
        [JsonPropertyName("trade_id")]
        public string TradeId { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("trade_time")]
        public string TradeTime { get; set; }

        [JsonPropertyName("price")]
        public string Price { get; set; }

        [JsonPropertyName("size")]
        public string Size { get; set; }

        [JsonPropertyName("commission")]
        public string Commission { get; set; }

        [JsonPropertyName("product_id")]
        public string ProductId { get; set; }

        [JsonPropertyName("side")]
        public string Side { get; set; }
    }
}
