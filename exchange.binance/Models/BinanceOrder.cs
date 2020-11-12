using System.Text.Json.Serialization;
using exchange.core.enums;

namespace exchange.binance.models
{
    public class BinanceOrder
    {
        [JsonPropertyName("symbol")] public string Symbol { get; set; }
        [JsonPropertyName("orderId")] public int ID { get; set; }
        [JsonPropertyName("orderListId")] public int OrderListID { get; set; }
        [JsonPropertyName("clientOrderId")] public string ClientOrderID { get; set; }
        [JsonPropertyName("price")] public string Price { get; set; }
        [JsonPropertyName("origQty")] public string OrigQty { get; set; }
        [JsonPropertyName("executedQty")] public string ExecutedQty { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("timeInForce")] public string TimeInForce { get; set; }
        [JsonPropertyName("stopPrice")] public string StopPrice { get; set; }
        [JsonPropertyName("icebergQty")] public string IcebergQty { get; set; }
        [JsonPropertyName("isWorking")] public bool IsWorking { get; set; }
        [JsonPropertyName("time")] public long Time { get; set; }
        [JsonPropertyName("updateTime")] public long UpdateTime { get; set; }
        [JsonPropertyName("transactTime")] public long TransactTime { get; set; }
        [JsonPropertyName("side")] public OrderSide OrderSide { get; set; }
        [JsonPropertyName("type")] public OrderType OrderType { get; set; }
        public decimal OrderSize { get; set; }
        public decimal LimitPrice { get; set; }
    }
}