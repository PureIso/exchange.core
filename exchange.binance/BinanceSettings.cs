using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using exchange.binance.models;
using exchange.core.models;

namespace exchange.binance
{
    [Serializable]
    public class BinanceSettings
    {
        public List<Ticker> Tickers { get; set; }
        public List<Account> Accounts { get; set; }
        [JsonPropertyName("server_time")] public ServerTime ServerTime { get; set; }
        [JsonPropertyName("current_prices")] public Dictionary<string, decimal> CurrentPrices { get; set; }
        [JsonPropertyName("accounts")] public BinanceAccount BinanceAccount { get; set; }
        [JsonPropertyName("exchange_info")] public ExchangeInfo ExchangeInfo { get; set; }
    }
}