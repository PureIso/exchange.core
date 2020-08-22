using System;
using System.Text.Json.Serialization;

namespace exchange.core.models
{
    public class Fill
    {
        #region Properties

        [JsonPropertyName("trade_id")] public int TradeID { get; set; }

        [JsonPropertyName("product_id")] public string ProductID { get; set; }

        [JsonPropertyName("price")] public string Price { get; set; }

        [JsonPropertyName("size")] public string Size { get; set; }

        [JsonPropertyName("order_id")] public string OrderID { get; set; }

        [JsonPropertyName("side")] public string Side { get; set; }

        [JsonPropertyName("fee")] public string Fee { get; set; }

        [JsonPropertyName("settled")] public bool Settled { get; set; }

        [JsonPropertyName("created_at")] public DateTime Time { get; set; }

        #endregion
    }
}