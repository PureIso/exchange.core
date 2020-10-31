using System.Text.Json.Serialization;

namespace exchange.core.models
{
    public class PriceAndSize
    {
        [JsonPropertyName("price")]
        public decimal Price { get; set; }
        [JsonPropertyName("size")]
        public decimal Size { get; set; }
    }
}
