using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using exchange.core.Enums;

namespace exchange.core.Models
{
    public class BinanceOrder
    {
        [JsonPropertyName("symbol")] public string Symbol { get; set; }
        [JsonPropertyName("orderId")] public string ID { get; set; }
        [JsonPropertyName("clientOrderId")] public string ClientOrderID { get; set; }
        [JsonPropertyName("price")] public string Price { get; set; }
        [JsonPropertyName("origQty")] public string OrigQty { get; set; }
        [JsonPropertyName("executedQty")] public string ExecutedQty { get; set; }
        [JsonPropertyName("cummulativeQuoteQty")]
        public string CummulativeQuoteQty { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("timeInForce")] public string TimeInForce { get; set; }
        [JsonPropertyName("stopPrice")] public string StopPrice { get; set; }
        [JsonPropertyName("icebergQty")] public string IcebergQty { get; set; }
        [JsonPropertyName("isWorking")] public bool IsWorking { get; set; }
        [JsonPropertyName("time")] public long Time { get; set; }
        [JsonPropertyName("updateTime")] public long UpdateTime { get; set; }
        [JsonPropertyName("side")] public OrderSide OrderSide { get; set; }
        [JsonPropertyName("type")] public OrderType OrderType { get; set; }
        public decimal OrderSize { get; set; }
        public decimal LimitPrice { get; set; }
    }
}
