using System.Text.Json.Serialization;

namespace exchange.coinbase.models
{
    public class Product
    {
        #region Properties
        [JsonPropertyName("id")]
        public string ID { get; set; }
        [JsonPropertyName("base_currency")]
        public string Base_Currency { get; set; }
        [JsonPropertyName("quote_currency")]
        public string Quote_Currency { get; set; }
        [JsonPropertyName("base_min_size")]
        public string Base_Min_Size { get; set; }
        [JsonPropertyName("base_max_size")]
        public string Base_Max_Size { get; set; }
        [JsonPropertyName("quote_increment")]
        public string Quote_Increment { get; set; }
        #endregion
    }
}
