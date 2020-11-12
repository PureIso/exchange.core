using System;
using System.Text.Json.Serialization;
using exchange.core.helpers;

namespace exchange.core.enums
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderSide
    {
        [StringValue("buy")] Buy,
        [StringValue("sell")] Sell
    }
}