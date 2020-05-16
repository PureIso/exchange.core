using System.Text.Json.Serialization;

namespace exchange.core.Models
{
    public class Detail
    {
        [JsonPropertyName("order_id")]
        public string OrderID { get; set; }
        [JsonPropertyName("trade_id")]
        public string TradeID { get; set; }
        [JsonPropertyName("product_id")]
        public string ProductID { get; set; }
    }
}
