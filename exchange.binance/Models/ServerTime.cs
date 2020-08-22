using System;
using System.Text.Json.Serialization;
using exchange.core.helpers;

namespace exchange.binance.models
{
    public class ServerTime
    {
        public ServerTime()
        {
        }

        public ServerTime(long initialValue)
        {
            ServerTimeLong = initialValue;
        }

        [JsonPropertyName("serverTime")] public long ServerTimeLong { get; set; }

        public int GetDelay()
        {
            long serverTime = ServerTimeLong + 1000;
            int delay = (int) (long.Parse(DateTime.Now.GenerateDateTimeOffsetToUnixTimeMilliseconds()) - serverTime);
            if (delay < 0) delay = 0;
            if (delay > 5000) delay = 5000;
            return delay;
        }
    }
}