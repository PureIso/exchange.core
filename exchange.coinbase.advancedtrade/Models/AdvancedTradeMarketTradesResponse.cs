using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace exchange.coinbase.advancedtrade.models
{
    internal sealed class AdvancedTradeMarketTradesResponse
    {
        [JsonPropertyName("trades")]
        public List<AdvancedTradeMarketTrade> Trades { get; set; }

        [JsonPropertyName("best_bid")]
        public string BestBid { get; set; }

        [JsonPropertyName("best_ask")]
        public string BestAsk { get; set; }
    }

    internal sealed class AdvancedTradeMarketTrade
    {
        [JsonPropertyName("trade_id")]
        public string TradeId { get; set; }

        [JsonPropertyName("product_id")]
        public string ProductId { get; set; }

        [JsonPropertyName("price")]
        public string Price { get; set; }

        [JsonPropertyName("size")]
        public string Size { get; set; }

        [JsonPropertyName("time")]
        public string Time { get; set; }

        [JsonPropertyName("side")]
        public string Side { get; set; }

        [JsonPropertyName("exchange")]
        public string Exchange { get; set; }
    }
}
