using System.Text.Json.Serialization;
using exchange.core.Models;

namespace exchange.core.models
{
    public class Product : Error
    {
        #region Properties
        [JsonPropertyName("id")]
        public string ID { get; set; }
        [JsonPropertyName("base_currency")]
        public string BaseCurrency { get; set; }
        [JsonPropertyName("quote_currency")]
        public string QuoteCurrency { get; set; }
        [JsonPropertyName("base_min_size")]
        public string BaseMinSize { get; set; }
        [JsonPropertyName("base_max_size")]
        public string BaseMaxSize { get; set; }
        [JsonPropertyName("quote_increment")]
        public string QuoteIncrement { get; set; }
        #endregion
    }
}
