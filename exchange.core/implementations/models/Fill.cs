#region
using System;
using exchange.core.enums;
using exchange.core.interfaces.models;
using Newtonsoft.Json.Linq;
#endregion

namespace exchange.core.implementations.models
{
    public class Fill : IFill
    {
        #region Properties
        public string ID { get; set; }
        public string OrderID { get; set; }
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public decimal Size { get; set; }
        public OrderSide Side { get; set; }
        public decimal Fee { get; set; }
        public string FeeSymbol { get; set; }
        public DateTime DateTime { get; set; }
        public bool Settled { get; set; }
        #endregion

        public Fill() { }
        public Fill(JToken jToken)
        {
            if (jToken["trade_id"] == null || jToken["product_id"] == null ||
                jToken["price"] == null || jToken["size"] == null ||
                jToken["order_id"] == null || jToken["created_at"] == null ||
                jToken["fee"] == null || jToken["settled"] == null ||
                jToken["side"] == null) return;
            ID = jToken["trade_id"].Value<string>();
            Symbol = jToken["product_id"].Value<string>();
            Price = jToken["price"].Value<decimal>();
            Size = jToken["size"].Value<decimal>();
            OrderID = jToken["order_id"].Value<string>();
            DateTime = jToken["created_at"].Value<DateTime>();
            Fee = jToken["fee"].Value<decimal>();
            Settled = jToken["settled"].Value<bool>();
            Side = jToken["side"].Value<string>().ToLower() == "buy" ? OrderSide.Buy : OrderSide.Sell;
        }

        #region Static Method
        public static Fill FromJToken(JToken jToken)
        {
            if (jToken["trade_id"] == null || jToken["product_id"] == null ||
                jToken["price"] == null || jToken["size"] == null ||
                jToken["order_id"] == null || jToken["created_at"] == null ||
                jToken["fee"] == null || jToken["settled"] == null ||
                jToken["side"] == null) return null;
            Fill fill = new Fill
            {
                ID = jToken["trade_id"].Value<string>(),
                Symbol = jToken["product_id"].Value<string>(),
                Price = jToken["price"].Value<decimal>(),
                Size = jToken["size"].Value<decimal>(),
                OrderID = jToken["order_id"].Value<string>(),
                DateTime = jToken["created_at"].Value<DateTime>(),
                Fee = jToken["fee"].Value<decimal>(),
                Settled = jToken["settled"].Value<bool>(),
                Side = jToken["side"].Value<string>().ToLower() == "buy" ? OrderSide.Buy : OrderSide.Sell
            };
            return fill;
        }
        public static Fill FromJToken(JToken jToken, string symbol, OrderSide orderSide = OrderSide.Unknown)
        {
            Fill fill;
            if (jToken["symbol"] == null || jToken["id"] == null ||
                jToken["orderId"] == null || jToken["price"] == null ||
                jToken["qty"] == null || jToken["commission"] == null ||
                jToken["commissionAsset"] == null || jToken["time"] == null ||
                jToken["isBuyer"] == null)
            {
                if (jToken["tradeId"] == null || jToken["price"] == null ||
                    jToken["qty"] == null || jToken["commission"] == null ||
                    jToken["commissionAsset"] == null)
                    return null;
                fill = new Fill
                {
                    ID = jToken["tradeId"].Value<string>(),
                    Symbol = symbol,
                    Price = jToken["price"].Value<decimal>(),
                    Size = jToken["qty"].Value<decimal>(),
                    Fee = jToken["commission"].Value<decimal>(),
                    FeeSymbol = jToken["commissionAsset"].Value<string>(),
                    Side = orderSide
                };
                return fill;
            }

            DateTime unix = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            fill = new Fill
            {
                ID = jToken["id"].Value<string>(),
                Symbol = jToken["symbol"].Value<string>(),
                Price = jToken["price"].Value<decimal>(),
                Size = jToken["qty"].Value<decimal>(),
                OrderID = jToken["orderId"].Value<string>(),
                DateTime = unix.AddMilliseconds(jToken["time"].Value<long>()),
                Fee = jToken["commission"].Value<decimal>(),
                FeeSymbol = jToken["commissionAsset"].Value<string>(),
                Side = jToken["isBuyer"].Value<bool>() ? OrderSide.Buy : OrderSide.Sell
            };
            return fill;
        }
        #endregion
    }
}