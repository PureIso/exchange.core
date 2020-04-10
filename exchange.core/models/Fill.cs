using System;
using System.Text.Json.Serialization;

namespace exchange.coinbase.models
{
    public class Fill
    {
        #region Properties
        [JsonPropertyName("trade_id")]
        public string Trade_ID { get; set; }
        [JsonPropertyName("product_id")]
        public string Product_ID { get; set; }
        [JsonPropertyName("price")]
        public string Price { get; set; }
        [JsonPropertyName("size")]
        public string Size { get; set; }
        [JsonPropertyName("order_id")]
        public string Order_ID { get; set; }
        [JsonPropertyName("side")]
        public string Side { get; set; }
        [JsonPropertyName("fee")]
        public string Fee { get; set; }
        [JsonPropertyName("settled")]
        public bool Settled { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime Time { get; set; }
        #endregion
    }
}
