using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using exchange.core.models;

namespace exchange.coinbase
{
    [Serializable]
    public class CoinbaseSettings
    {
        [JsonPropertyName("current_prices")] public Dictionary<string, decimal> CurrentPrices { get; set; }

        [JsonPropertyName("tickers")] public List<Ticker> Tickers { get; set; }

        [JsonPropertyName("accounts")] public List<Account> Accounts { get; set; }
    }
}