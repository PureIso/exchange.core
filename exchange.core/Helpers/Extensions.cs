﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using exchange.core.models;

namespace exchange.core.helpers
{
    public static class Extensions
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        ///     Seconds since January 1st, 1970.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static decimal ToUnixTimestamp(this DateTime dateTime)
        {
            return (decimal) (dateTime - UnixEpoch).TotalSeconds;
        }

        public static string GenerateDateTimeOffsetToUnixTimeMilliseconds(this DateTime baseDateTime)
        {
            DateTimeOffset dtOffset = new DateTimeOffset(baseDateTime);
            return dtOffset.ToUnixTimeMilliseconds().ToString();
        }

        public static decimal ToDecimal(this string value)
        {
            return decimal.TryParse(value, out decimal decimalValue) ? Math.Round(decimalValue, 6) : 0;
        }

        public static long ToLong(this string value)
        {
            return long.TryParse(value, out long longValue) ? longValue : 0;
        }

        public static DateTime ToDateTime(this string value)
        {
            if (DateTime.TryParse(value, out DateTime dateTimeValue))
                return dateTimeValue;
            return DateTime.TryParseExact(value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out DateTime dateTimeValueExact) ? dateTimeValueExact : new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public static List<Order> ToOrderList(this ArrayList[] arrayLists)
        {
            List<Order> orders = new List<Order>();
            if (arrayLists == null)
                return orders;
            foreach (ArrayList array in arrayLists)
            {
                string price = string.Empty;
                string quantity;
                decimal quantityDecimal = 0;
                string size = string.Empty;
                switch (array.Count)
                {
                    case 2:
                    {
                        if (array[0] != null && array[1] != null)
                        { 
                            price = ((JsonElement) array[0]).GetString(); 
                            quantity = ((JsonElement) array[1]).GetString();
                            decimal.TryParse(quantity, out quantityDecimal);
                        }
                        break;
                    }
                    case 3:
                    {
                        if (array[0] != null && array[1] != null && array[2] != null)
                        {
                            price = ((JsonElement)array[0]).GetString();
                            size = ((JsonElement) array[1]).GetString();
                            quantityDecimal = ((JsonElement)array[2]).GetInt32();
                        }
                        break;
                    }
                }
                Order order = new Order
                {
                    Price = price,
                    Size = size,
                    Quantity = Math.Round(quantityDecimal, 6)
                };
                orders.Add(order);
            }
            return orders;
        }

        public static List<string> ProductsToSymbols(this List<Product> products)
        {
            List<string> symbols = new List<string>();
            foreach (Product product in products) symbols.Add(product.ID);
            return symbols;
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
                            DateAndTime = start.AddMilliseconds(((JsonElement) array[0]).GetInt64()).ToLocalTime(),
                            Open = decimal.Parse(((JsonElement) array[1]).GetString()),
                            High = decimal.Parse(((JsonElement) array[2]).GetString()),
                            Low = decimal.Parse(((JsonElement) array[3]).GetString()),
                            Close = decimal.Parse(((JsonElement) array[4]).GetString()),
                            Volume = decimal.Parse(((JsonElement) array[5]).GetString())
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
                    //[ time, low, high, open, close, volume ],
                    DateAndTime = unix.AddSeconds(((JsonElement) array[0]).GetInt64()),
                    Low = ((JsonElement) array[1]).GetDecimal(),
                    High = ((JsonElement) array[2]).GetDecimal(),
                    Open = ((JsonElement) array[3]).GetDecimal(),
                    Close = ((JsonElement) array[4]).GetDecimal(),
                    Volume = ((JsonElement) array[5]).GetDecimal()
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
            foreach (Product product in products) productIds += $@"""{product.ID}"",";

            return
                $@"{{""type"": ""subscribe"",""channels"": [{{""name"": ""ticker"",""product_ids"": [{productIds?.Remove(productIds.Length - 1, 1)}]}}]}}";
        }

        public static string ToUnSubscribeString(this List<Product> products)
        {
            if (products == null || !products.Any())
                return null;
            string productIds = null;
            foreach (Product product in products) productIds += $@"""{product.ID}"",";
            return
                $@"{{""type"": ""unsubscribe"",""product_ids"": [{productIds?.Remove(productIds.Length - 1, 1)}]}}";
        }

        /// <summary>
        ///     Will get the string value for a given enums value, this will
        ///     only work if you assign the StringValue attribute to
        ///     the items in your enum.
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
                       typeof(StringValueAttribute), false) is StringValueAttribute[] stringValueAttributes &&
                   stringValueAttributes.Length > 0
                ? stringValueAttributes[0].StringValue
                : null;
        }
    }
}