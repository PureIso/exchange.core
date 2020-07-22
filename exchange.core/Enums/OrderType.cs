using exchange.core.helpers;
using System;
using System.Text.Json.Serialization;

namespace exchange.core.Enums
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderType
    {
        [StringValue("limit")]
        Limit,
        [StringValue("market")]
        Market
    }
}
