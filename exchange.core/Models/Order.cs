using System.Text.Json.Serialization;

namespace exchange.core.models
{
    public class Order
    {
        [JsonPropertyName("id")] public string ID { get; set; }

        [JsonPropertyName("size")] public string Size { get; set; }

        [JsonPropertyName("product_id")] public string ProductID { get; set; }

        [JsonPropertyName("side")] public string Side { get; set; }

        [JsonPropertyName("stp")] public string SelfTradePreventionType { get; set; }

        [JsonPropertyName("fund")] public string Fund { get; set; }

        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("post_only")] public bool PostOnly { get; set; }

        [JsonPropertyName("price")] public string Price { get; set; }

        [JsonPropertyName("time_in_force")] public string TimeInForce { get; set; }

        [JsonPropertyName("created_at")] public string CreatedAt { get; set; }

        [JsonPropertyName("fill_fees")] public string FillFees { get; set; }

        [JsonPropertyName("filled_size")] public string FilledSize { get; set; }

        [JsonPropertyName("executed_value")] public string ExecutedValue { get; set; }

        [JsonPropertyName("status")] public string Status { get; set; }

        [JsonPropertyName("settled")] public bool Settled { get; set; }

        public decimal Quantity { get; set; }
    }
}