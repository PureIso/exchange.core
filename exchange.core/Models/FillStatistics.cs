using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace exchange.core.models
{
    public class FillStatistics
    {
        [JsonPropertyName("product_id")] public string ProductID { get; set; }
        [JsonPropertyName("quote_currency")] public string QuoteCurrency { get; set; }
        [JsonPropertyName("base_currency")] public string BaseCurrency { get; set; }
        [JsonPropertyName("mini_fill_sell_above_list")] public List<MiniFill> MiniFillSellAboveList { get; set; }
        [JsonPropertyName("mini_fill_buy_below_list")] public List<MiniFill> MiniFillBuyBelowList { get; set; }
    }
}