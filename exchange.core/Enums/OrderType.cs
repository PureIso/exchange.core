using System;
using System.Text.Json.Serialization;
using exchange.core.helpers;

namespace exchange.core.Enums
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderType
    {
        [StringValue("limit")] Limit,
        [StringValue("market")] Market
    }
}