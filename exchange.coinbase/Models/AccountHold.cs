using System.Text.Json.Serialization;

namespace exchange.coinbase.models
{
    public class AccountHold
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }
        [JsonPropertyName("account_id")]
        public string AccountID { get; set; }
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }
        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }
        [JsonPropertyName("amount")]
        public string Amount { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("ref")]
        public string Reference { get; set; }
    }
}
