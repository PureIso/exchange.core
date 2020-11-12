using System;
using System.Text.Json.Serialization;
using exchange.core.helpers;

namespace exchange.core.enums
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessageType
    {
        [StringValue("General")] General,
        [StringValue("JsonOutput")] JsonOutput,
        [StringValue("Error")] Error
    }
}