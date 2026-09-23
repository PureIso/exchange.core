using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace exchange.coinbase.advancedtrade.models
{
    internal sealed class AdvancedTradeOrdersResponse
    {
        [JsonPropertyName("orders")]
        public List<AdvancedTradeOrder> Orders { get; set; }

        [JsonPropertyName("has_next")]
        public bool HasNext { get; set; }

        [JsonPropertyName("cursor")]
        public string Cursor { get; set; }

        [JsonPropertyName("proof_token_required")]
        public bool ProofTokenRequired { get; set; }
    }

    internal sealed class AdvancedTradeOrder
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("product_id")]
        public string ProductId { get; set; }

        [JsonPropertyName("order_configuration")]
        public JsonElement OrderConfiguration { get; set; }

        [JsonPropertyName("side")]
        public string Side { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("created_time")]
        public string CreatedTime { get; set; }

        [JsonPropertyName("average_filled_price")]
        public string AverageFilledPrice { get; set; }

        [JsonPropertyName("total_fees")]
        public string TotalFees { get; set; }

        [JsonPropertyName("time_in_force")]
        public string TimeInForce { get; set; }

        [JsonPropertyName("filled_size")]
        public string FilledSize { get; set; }

        [JsonPropertyName("filled_value")]
        public string FilledValue { get; set; }

        [JsonPropertyName("order_type")]
        public string OrderType { get; set; }

        [JsonPropertyName("settled")]
        public bool Settled { get; set; }

        [JsonPropertyName("size_in_quote")]
        public bool SizeInQuote { get; set; }
    }
}
