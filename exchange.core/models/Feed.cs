using System.Collections;
using System.Text.Json.Serialization;

namespace exchange.coinbase.models
{
    public class Feed
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("reason")]
        public string Reason { get; set; }
        [JsonPropertyName("channels")]
        public Channel[] Channels { get; set; }
    }
}
