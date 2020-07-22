using exchange.core.helpers;
using System;
using System.Text.Json.Serialization;

namespace exchange.core.Enums
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderSide
    {
        [StringValue("buy")]
        Buy,
        [StringValue("sell")]
        Sell
    }
}
