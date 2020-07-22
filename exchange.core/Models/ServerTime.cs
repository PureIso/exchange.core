using exchange.core.helpers;
using System;
using System.Text.Json.Serialization;

namespace exchange.core.Models
{
    public class ServerTime
    {
        
        [JsonPropertyName("serverTime")]
        public long ServerTimeLong { get; set; }
        public ServerTime() { }

        public ServerTime(long initialValue)
        {
            ServerTimeLong = initialValue;
        }
        public int GetDelay()
        {
            long serverTime = ServerTimeLong + 1000;
            int delay = (int)(long.Parse(DateTime.Now.GenerateDateTimeOffsetToUnixTimeMilliseconds()) - serverTime);
            if (delay < 0) delay = 0;
            if (delay > 5000) delay = 5000;
            return delay;
        }
    }
}
