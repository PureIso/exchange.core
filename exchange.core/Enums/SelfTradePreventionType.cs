using exchange.core.helpers;
using System;
using System.Text.Json.Serialization;

namespace exchange.core.Enums
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SelfTradePreventionType
    {
        [StringValue("dc")]
        DecreaseAndCancel,
        [StringValue("co")]
        CancelOldest,
        [StringValue("cn")]
        CancelNewest,
        [StringValue("cb")]
        CancelBoth,
    }
}
