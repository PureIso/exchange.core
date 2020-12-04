using System;
using System.Text.Json.Serialization;
using exchange.core.helpers;

namespace exchange.core.Enums
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MainCurrencyName
    {
        [StringValue("EUR")] Euro,
        [StringValue("USD")] USDollar,
        [StringValue("BTC")] Bitcoin,
        [StringValue("BNB")] BinanceCoin,
    }
}
