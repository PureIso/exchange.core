using exchange.core.models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace exchange.core
{
    public static class Extensions
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Seconds since January 1st, 1970.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long ToUnixTimestamp(this DateTime dateTime)
        {
            return (long)(dateTime - UnixEpoch).TotalSeconds;
        }

        public static decimal ToDecimal(this string value)
        {
            return decimal.TryParse(value, out decimal decimalValue) ? decimalValue : 0;
        }

        public static DateTime ToDateTime(this string value)
        {
            if (DateTime.TryParse(value, out DateTime dateTimeValue))
                return dateTimeValue;
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public static List<Order> ToOrderList(this ArrayList[] arrayLists)
        {
            List<Order> orders = new List<Order>();
            if (arrayLists == null) 
                return orders;
            foreach (ArrayList array in arrayLists)
            {
                Order order = new Order()
                {
                    Price = ((JsonElement)array[0]).GetString(),
                    Size = ((JsonElement)array[1]).GetString(),
                    Quantity = ((JsonElement)array[2]).GetInt32()
                };
                orders.Add(order);
            }
            return orders;
        }

        public static List<HistoricRate> ToHistoricRateList(this ArrayList[] arrayLists)
        {
            List<HistoricRate> historicRates = new List<HistoricRate>();
            if (arrayLists == null) 
                return historicRates;
            foreach (ArrayList array in arrayLists)
            {
                DateTime unix = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                HistoricRate historicRate = new HistoricRate()
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
    }
}
