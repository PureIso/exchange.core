using System.Text.Json.Serialization;

namespace exchange.core.Models
{
    public class Error
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
