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
        [JsonPropertyName("sequence")]
        public long Sequence { get; set; }
        [JsonPropertyName("time")]
        public string Time { get; set; }
        [JsonPropertyName("product_id")]
        public string ProductID { get; set; }
        [JsonPropertyName("price")]
        public string Price { get; set; }
        [JsonPropertyName("side")]
        public string Side { get; set; }
        [JsonPropertyName("last_size")]
        public string LastSize { get; set; }
        [JsonPropertyName("best_bid")]
        public string BestBid { get; set; }
        [JsonPropertyName("best_ask")]
        public string BestAsk { get; set; }
    }
}
