using System;
using System.Collections.Generic;

namespace exchange.coinbase.advancedtrade
{
    public sealed class CoinbaseMarketTradesSnapshot
    {
        public IReadOnlyList<CoinbaseMarketTrade> Trades { get; init; } = Array.Empty<CoinbaseMarketTrade>();
        public string BestBid { get; init; }
        public string BestAsk { get; init; }
    }

    public sealed class CoinbaseMarketTrade
    {
        public string TradeId { get; init; }
        public string ProductId { get; init; }
        public string Price { get; init; }
        public string Size { get; init; }
        public DateTimeOffset Time { get; init; }
        public string Side { get; init; }
        public string Exchange { get; init; }
    }
}
