using System;
using System.Text.Json.Serialization;
using exchange.core.helpers;

namespace exchange.core.enums
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderType
    {
        [StringValue("limit")] Limit,
        [StringValue("market")] Market
    }
}