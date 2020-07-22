using exchange.core.helpers;
using System;
using System.Text.Json.Serialization;

namespace exchange.core.Enums
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageType
    {
        [StringValue("General")]
        General,
        [StringValue("JsonOutput")]
        JsonOutput,
        [StringValue("Error")]
        Error
    }
}