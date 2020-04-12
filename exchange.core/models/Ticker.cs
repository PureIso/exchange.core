using System.Text.Json.Serialization;

namespace exchange.coinbase.models
{
    public class Ticker
    {
        #region Properties
        [JsonPropertyName("trade_id")]
        public int TradeID { get; set; }
        public string ProductID { get; set; }
        [JsonPropertyName("size")]
        public string Size { get; set; }
        [JsonPropertyName("price")]
        public string Price { get; set; }
        [JsonPropertyName("bid")]
        public string Bid { get; set; }
        [JsonPropertyName("ask")]
        public string Ask { get; set; }
        [JsonPropertyName("volume")]
        public string Volume { get; set; }
        [JsonPropertyName("time")]
        public string Time { get; set; }
        #endregion
    }
}
