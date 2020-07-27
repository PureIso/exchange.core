using exchange.core.models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using exchange.core.Models;
using System.Globalization;

namespace exchange.core.helpers
{
    public static class Extensions
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Seconds since January 1st, 1970.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static decimal ToUnixTimestamp(this DateTime dateTime)
        {
            return (decimal)(dateTime - UnixEpoch).TotalSeconds;
        }

        public static string GenerateDateTimeOffsetToUnixTimeMilliseconds(this DateTime baseDateTime)
        {
            DateTimeOffset dtOffset = new DateTimeOffset(baseDateTime);
            return dtOffset.ToUnixTimeMilliseconds().ToString();
        }

        public static decimal ToDecimal(this string value)
        {
            return decimal.TryParse(value, out decimal decimalValue) ? decimalValue : 0;
        }

        public static long ToLong(this string value)
        {
            return long.TryParse(value, out long longValue) ? longValue : 0;
        }

        public static DateTime ToDateTime(this string value)
        {
            if (DateTime.TryParse(value, out DateTime dateTimeValue))
                return dateTimeValue.ToUniversalTime();
            if (DateTime.TryParseExact(value, "MM/dd/yyyy HH:mm:ss",CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out DateTime dateTimeValueExact))
                return dateTimeValueExact.ToUniversalTime();
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public static List<Order> ToOrderList(this ArrayList[] arrayLists)
        {
            List<Order> orders = new List<Order>();
            if (arrayLists == null) 
                return orders;
            foreach (ArrayList array in arrayLists)
            {
                Order order = new Order();
                switch (array.Count)
                {
                    case 2:
                        order = new Order
                        {
                            Price = ((JsonElement)array[0]).GetString(),
                            Quantity = decimal.Parse(((JsonElement)array[1]).GetString())
                        };
                        break;
                    case 3:
                        order = new Order
                        {
                            Price = ((JsonElement)array[0]).GetString(),
                            Size = ((JsonElement)array[1]).GetString(),
                            Quantity = ((JsonElement)array[2]).GetInt32()
                        };
                        break;
                }
                orders.Add(order);
            }
            return orders;
        }

        public static List<HistoricRate> ToHistoricCandleList(this ArrayList[] arrayLists)
        {
            List<HistoricRate> historicRates = new List<HistoricRate>();
            if (arrayLists == null)
                return historicRates;
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (ArrayList array in arrayLists)
            {
                HistoricRate historicRate = new HistoricRate();
                switch (array.Count)
                {
                    case 12:
                        historicRate = new HistoricRate
                        {
                            //1499040000000,      // Open time
                            //"0.01634790",       // Open
                            //"0.80000000",       // High
                            //"0.01575800",       // Low
                            //"0.01577100",       // Close
                            //"148976.11427815",  // Volume

                            //1499644799999,      // Close time
                            //"2434.19055334",    // Quote asset volume
                            //308,                // Number of trades
                            //"1756.87402397",    // Taker buy base asset volume
                            //"17928899.62484339" // Ignore.
                            DateAndTime = start.AddMilliseconds(((JsonElement)array[0]).GetInt64()).ToLocalTime(),
                            Open = decimal.Parse(((JsonElement)array[1]).GetString()),
                            High = decimal.Parse(((JsonElement)array[2]).GetString()),
                            Low = decimal.Parse(((JsonElement)array[3]).GetString()),
                            Close = decimal.Parse(((JsonElement)array[4]).GetString()),
                            Volume = decimal.Parse(((JsonElement)array[5]).GetString()),
                        };
                        break;
                }
                historicRates.Add(historicRate);
            }
            return historicRates;
        }

        public static List<HistoricRate> ToHistoricRateList(this ArrayList[] arrayLists)
        {
            List<HistoricRate> historicRates = new List<HistoricRate>();
            if (arrayLists == null) 
                return historicRates;
            foreach (ArrayList array in arrayLists)
            {
                DateTime unix = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                HistoricRate historicRate = new HistoricRate
                {
                    DateAndTime = unix.AddSeconds(((JsonElement)array[0]).GetInt64()),
                    Low = ((JsonElement)array[1]).GetDecimal(),
                    High = ((JsonElement)array[2]).GetDecimal(),
                    Open = ((JsonElement)array[3]).GetDecimal(),
                    Close = ((JsonElement)array[4]).GetDecimal(),
                    Volume = ((JsonElement)array[5]).GetDecimal()
                };
                historicRates.Add(historicRate);
            }
            return historicRates;
        }

        public static DateTime RoundCurrentToNextFiveMinutes(this DateTime dateTime)
        {
            DateTime result = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);
            return result.AddMinutes((dateTime.Minute / 5 + 1) * 5);
        }

        public static string ToSubscribeString(this List<Product> products)
        {
            if (products == null || !products.Any())
                return null;
            string productIds = null;
            foreach (Product product in products)
            {
                productIds += $@"""{product.ID}"",";
            }
            return $@"{{""type"": ""subscribe"",""channels"": [{{""name"": ""ticker"",""product_ids"": [{productIds?.Remove(productIds.Length - 1, 1)}]}}]}}";
        }
        public static string ToUnSubscribeString(this List<Product> products)
        {
            if (products == null || !products.Any())
                return null;
            string productIds = null;
            foreach (Product product in products)
            {
                productIds += $@"""{product.ID}"",";
            }
            return $@"{{""type"": ""subscribe"",""channels"": [{{""name"": ""ticker"",""product_ids"": [{productIds?.Remove(productIds.Length - 1, 1)}]}}]}}";
        }
        /// <summary>
        /// Will get the string value for a given enums value, this will
        /// only work if you assign the StringValue attribute to
        /// the items in your enum.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetStringValue(this Enum value)
        {
            // Get the type
            Type type = value.GetType();
            // Get fieldinfo for this type
            FieldInfo fieldInfo = type.GetField(value.ToString());
            // Get the stringvalue attributes
            // Return the first if there was a match.
            return fieldInfo.GetCustomAttributes(
                typeof(StringValueAttribute), false) is StringValueAttribute[] stringValueAttributes && stringValueAttributes.Length > 0 ?
                stringValueAttributes[0].StringValue : null;
        }
    }
}
