using exchange.coinbase.models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace exchange.coinbase
{
    public static class Extensions
    {
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        ///     Seconds since January 1st, 1970.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static double ToUnixTimestamp(this DateTime dateTime)
        {
            return (dateTime - UnixEpoch).TotalSeconds;
        }

        public static decimal ToDecimal(this string value)
        {
            if (decimal.TryParse(value, out decimal decimalValue))
                return decimalValue;
            return 0;
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
            if(arrayLists != null)
            {
                foreach (ArrayList array in arrayLists)
                {
                    Order order = new Order()
                    {
                        Price = (string)array[0],
                        Size = (string)array[1],
                        Quantity = (long)array[2]
                    };
                    orders.Add(order);
                }
            }
            return orders;
        }

        public static List<HistoricRate> ToHistoricRateList(this ArrayList[] arrayLists)
        {
            List<HistoricRate> historicRates = new List<HistoricRate>();
            if (arrayLists != null)
            {
                foreach (ArrayList array in arrayLists)
                {
                    DateTime unix = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    HistoricRate historicRate = new HistoricRate()
                    {
                        DateAndTime = unix.AddSeconds((long)array[0]),
                        Low = Convert.ToDecimal(array[1]),
                        High = Convert.ToDecimal(array[2]),
                        Open = Convert.ToDecimal(array[3]),
                        Close = Convert.ToDecimal(array[4]),
                        Volume = Convert.ToDecimal(array[5]),
                    };
                    historicRates.Add(historicRate);
                }
            }
            return historicRates;
        }

        public static DateTime RoundCurrentToNextFiveMinutes(this DateTime dateTime)
        {
            DateTime result = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);
            return result.AddMinutes((dateTime.Minute / 5 + 1) * 5);
        }
    }
}
