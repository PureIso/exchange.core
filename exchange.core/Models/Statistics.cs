using System.Text.Json.Serialization;

namespace exchange.core.Models
{
    public class Statistics
    { 
        [JsonPropertyName("open")] public string Open { get; set; }
        [JsonPropertyName("high")] public string High { get; set; }
        [JsonPropertyName("low")] public string Low { get; set; }
        [JsonPropertyName("volume")] public string Volume { get; set; }
        [JsonPropertyName("last")] public string Last { get; set; }
        [JsonPropertyName("volume_30day")] public string Volume30Day { get; set; }
    }
}