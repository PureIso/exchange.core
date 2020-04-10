using System.Collections;
using System.Text.Json.Serialization;

namespace exchange.coinbase.models
{
    public class OrderBook
    {

        [JsonPropertyName("sequence")]
        public string Sequence { get; set; }
        [JsonPropertyName("bids")]
        public ArrayList[] Bids { get; set; }
        [JsonPropertyName("asks")]
        public ArrayList[] Asks { get; set; }
    }
}
