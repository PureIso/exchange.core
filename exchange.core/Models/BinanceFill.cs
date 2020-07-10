using System.Text.Json.Serialization;

namespace exchange.core.Models
{
    public class BinanceFill
    {
        [JsonPropertyName("symbol")]
        public string ID { get; set; }
        [JsonPropertyName("id")]
        public int TradeID { get; set; }
        [JsonPropertyName("orderId")]
        public int OrderID { get; set; }
        [JsonPropertyName("orderListId")]
        public int OrderListId { get; set; }
        [JsonPropertyName("price")]
        public string Price { get; set; }
        [JsonPropertyName("qty")]
        public string Quality { get; set; }
        [JsonPropertyName("quoteQty")]
        public string QuoteQuality { get; set; }
        [JsonPropertyName("commission")]
        public string Commission { get; set; }
        [JsonPropertyName("commissionAsset")]
        public string CommissionAsset { get; set; }
        [JsonPropertyName("time")]
        public long Time { get; set; }
        [JsonPropertyName("isBuyer")]
        public bool IsBuyer { get; set; }
        [JsonPropertyName("isMaker")]
        public bool IsMaker { get; set; }
        [JsonPropertyName("isBestMatch")]
        public bool IsBestMatch { get; set; }
    }
}
