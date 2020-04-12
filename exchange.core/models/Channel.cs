using System.Text.Json.Serialization;

namespace exchange.coinbase.models
{
    public class Channel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("product_ids")]
        public string[] ProductIDs { get; set; }
    }
}
