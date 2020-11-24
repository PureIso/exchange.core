using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using exchange.binance.models;
using exchange.core.models;

namespace exchange.binance.Helpers
{
    public static class BinanceExtensions
    {
        public static Order ToOrder(this BinanceOrder binanceOrder)
        {
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Order convertedOrder = new Order
            {
                Size = binanceOrder.OrigQty.ToString(CultureInfo.InvariantCulture),
                Side = binanceOrder.OrderSide.ToString(),
                Type = binanceOrder.OrderType.ToString(),
                Price = binanceOrder.Price,
                ProductID = binanceOrder.Symbol,
                CreatedAt = start.AddMilliseconds(binanceOrder.TransactTime).ToLocalTime()
                    .ToString(CultureInfo.InvariantCulture),
                ExecutedValue = binanceOrder.ExecutedQty,
                ID = binanceOrder.ID.ToString(),
                TimeInForce = binanceOrder.TimeInForce,
                Status = binanceOrder.Status
            };
            return convertedOrder;
        }

        public static List<Fill> ToOrderFills(this BinanceOrder binanceOrder)
        {
            List<Fill> fills = new List<Fill>();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Order convertedOrder = new Order
            {
                Size = binanceOrder.OrigQty.ToString(CultureInfo.InvariantCulture),
                Side = binanceOrder.OrderSide.ToString(),
                Type = binanceOrder.OrderType.ToString(),
                Price = binanceOrder.Price,
                ProductID = binanceOrder.Symbol,
                CreatedAt = start.AddMilliseconds(binanceOrder.TransactTime).ToLocalTime()
                    .ToString(CultureInfo.InvariantCulture),
                ExecutedValue = binanceOrder.ExecutedQty,
                ID = binanceOrder.ID.ToString(),
                TimeInForce = binanceOrder.TimeInForce,
                Status = binanceOrder.Status
            };
            if (binanceOrder.Fills == null || !binanceOrder.Fills.Any()) 
                return fills;
            foreach (Fill binanceOrderFill in binanceOrder.Fills)
            {
                binanceOrderFill.Size = binanceOrder.OrigQty.ToString(CultureInfo.InvariantCulture);
                binanceOrderFill.Side = binanceOrder.OrderSide.ToString();
                binanceOrderFill.ProductID = binanceOrder.Symbol;
                binanceOrderFill.Time = start.AddMilliseconds(binanceOrder.TransactTime).ToLocalTime();
                fills.Add(binanceOrderFill);
            }
            return fills;
        }
    }
}
