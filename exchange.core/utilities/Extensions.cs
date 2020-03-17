using System;

namespace exchange.core.utilities
{
    public static class Extensions
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static double ToUnixTimestamp(this DateTime dateTime)
        {
            return (dateTime - UnixEpoch).TotalSeconds;
        }
        public static bool IsJson(this string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            input = input.Trim();
            return input.StartsWith("{") && input.EndsWith("}")
                   || input.StartsWith("[") && input.EndsWith("]");
        }
        public static DateTime RoundToNextFiveMinutes(this DateTime dateTime)
        {
            DateTime result = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);
            return result.AddMinutes((dateTime.Minute / 5 + 1) * 5);
        }
        public static string GenerateUnixStringTimeStamp(this DateTime baseDateTime)
        {
            DateTimeOffset dtOffset = new DateTimeOffset(baseDateTime);
            return dtOffset.ToUnixTimeMilliseconds().ToString();
        }
    }
}
