using System.Text.Json.Serialization;

namespace exchange.core.Models
{
    public class Asset
    {
        [JsonPropertyName("asset")]
        public string ID { get; set; }
        [JsonPropertyName("free")]
        public string Free { get; set; }
        [JsonPropertyName("locked")]
        public string Locked { get; set; }
    }
}
