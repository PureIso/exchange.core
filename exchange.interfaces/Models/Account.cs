using System.Text.Json.Serialization;
using exchange.core.Models;

namespace exchange.core.models
{
    public class Account : Error
    {
        #region Properties
        [JsonPropertyName("id")]
        public string ID { get; set; }
        [JsonPropertyName("currency")]
        public string Currency { get; set; }
        [JsonPropertyName("balance")]
        public string Balance { get; set; }
        [JsonPropertyName("hold")]
        public string Hold { get; set; }
        [JsonPropertyName("available")]
        public string Available { get; set; }
        [JsonPropertyName("trading_enabled")]
        public bool TradingEnabled { get; set; }
        #endregion
    }
}