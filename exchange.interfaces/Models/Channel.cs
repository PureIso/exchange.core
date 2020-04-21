using System.Text.Json.Serialization;
using exchange.core.Models;

namespace exchange.core.models
{
    public class Channel : Error
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("product_ids")]
        public string[] ProductIDs { get; set; }
    }
}
