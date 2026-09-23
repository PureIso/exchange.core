using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace exchange.coinbase.advancedtrade.models
{
    internal sealed class AdvancedTradeAccountsResponse
    {
        [JsonPropertyName("has_next")]
        public bool HasNext { get; set; }

        [JsonPropertyName("accounts")]
        public List<AdvancedTradeAccount> Accounts { get; set; }

        [JsonPropertyName("cursor")]
        public string Cursor { get; set; }
    }

    internal sealed class AdvancedTradeAccountResponse
    {
        [JsonPropertyName("account")]
        public AdvancedTradeAccount Account { get; set; }
    }

    internal sealed class AdvancedTradeAccount
    {
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("available_balance")]
        public AdvancedTradeBalance AvailableBalance { get; set; }

        [JsonPropertyName("hold")]
        public AdvancedTradeBalance Hold { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("ready")]
        public bool Ready { get; set; }
    }

    internal sealed class AdvancedTradeBalance
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }
}
