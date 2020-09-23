using System.Text.Json.Serialization;

namespace exchange.core.Models
{
    public class Statistics
    {
        //{"open":"10222.25","high":"10411.44","low":"10215.28","volume":"6998.81991496","last":"10355.5","volume_30day":"384820.57886121"}
        [JsonPropertyName("open")] public string Open { get; set; }
        [JsonPropertyName("high")] public string High { get; set; }
        [JsonPropertyName("low")] public string Low { get; set; }
        [JsonPropertyName("volume")] public string Volume { get; set; }
        [JsonPropertyName("last")] public string Last { get; set; }
        [JsonPropertyName("volume_30day")] public string Volume30Day { get; set; }
    }
}
