using System;
using System.Text.Json.Serialization;
using exchange.core.helpers;

namespace exchange.core.Enums
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SelfTradePreventionType
    {
        [StringValue("dc")] DecreaseAndCancel,
        [StringValue("co")] CancelOldest,
        [StringValue("cn")] CancelNewest,
        [StringValue("cb")] CancelBoth
    }
}