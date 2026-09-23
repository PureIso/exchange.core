using System.Text.Json.Serialization;

namespace exchange.coinbase.advancedtrade
{
    public sealed class CoinbaseApiKeyPermissions
    {
        [JsonPropertyName("can_view")]
        public bool CanView { get; init; }

        [JsonPropertyName("can_trade")]
        public bool CanTrade { get; init; }

        [JsonPropertyName("can_transfer")]
        public bool CanTransfer { get; init; }

        [JsonPropertyName("can_receive")]
        public bool CanReceive { get; init; }

        [JsonPropertyName("portfolio_uuid")]
        public string PortfolioUuid { get; init; }

        [JsonPropertyName("portfolio_type")]
        public string PortfolioType { get; init; }
    }
}
