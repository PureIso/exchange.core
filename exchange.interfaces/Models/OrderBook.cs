using System.Collections;
using System.Text.Json.Serialization;
using exchange.core.Models;

namespace exchange.core.models
{
    public class OrderBook : Error
    {
        [JsonPropertyName("sequence")]
        public long Sequence { get; set; }
        [JsonPropertyName("bids")]
        public ArrayList[] Bids { get; set; }
        [JsonPropertyName("asks")]
        public ArrayList[] Asks { get; set; }
    }
}
