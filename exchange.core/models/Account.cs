using System.Text.Json.Serialization;


namespace exchange.coinbase.models
{
    public class Account
    {
        #region Properties
        [JsonPropertyName("id")]
        public string ID { get; set; }
        [JsonPropertyName("currency")]
        public string Currency { get; set; }
        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }
        [JsonPropertyName("hold")]
        public decimal Hold { get; set; }
        [JsonPropertyName("available")]
        public decimal Available { get; set; }
        [JsonPropertyName("trading_enabled")]
        public bool TradingEnabled { get; set; }
        #endregion
    }
}
