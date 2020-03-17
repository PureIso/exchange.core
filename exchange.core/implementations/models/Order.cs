using System;
using exchange.core.enums;
using exchange.core.interfaces.models;
using Newtonsoft.Json.Linq;

namespace exchange.core.implementations.models
{
    public class Order : IOrder
    {
        #region Properties
        public string ID { get; set; }
        public decimal Size { get; set; }
        public decimal Price { get; set; }
        public decimal Fees { get; set; }
        public IProduct Product { get; set; }
        public OrderSide Side { get; set; }
        public string Status { get; set; }
        public OrderType Type { get; set; }
        #endregion

        public static Order FromJToken(JToken jToken)
        {
            if (jToken == null) return null;
            Order order = new Order
            {
                ID = jToken.Value<string>("id"),
                Price = jToken.Value<decimal?>("price") ?? (decimal)0.0,
                Size = jToken.Value<decimal?>("size") ?? (decimal)0.0,
                //ProductID = jToken.Value<string>("product_id"),
                Side = (OrderSide)Enum.Parse(typeof(OrderSide), jToken.Value<string>("side"), true),
                Type = (OrderType)Enum.Parse(typeof(OrderType), jToken.Value<string>("type"), true),
                Status = jToken.Value<string>("status"),
                Fees = jToken.Value<decimal?>("fill_fees") ?? (decimal)0.0
            };
            return order;
        }
    }
}
