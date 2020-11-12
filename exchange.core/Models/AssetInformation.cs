using System.Collections.Generic;
using System.Text.Json.Serialization;
using exchange.core.enums;

namespace exchange.core.models
{
    public class AssetInformation
    {
        [JsonPropertyName("twenty_four_hour_price_change")]
        public decimal TwentyFourHourPriceChange { get; set; }
        [JsonPropertyName("twenty_four_hour_price_percentage_change")]
        public decimal TwentyFourHourPricePercentageChange { get; set; }
        [JsonPropertyName("currency_price")]
        public decimal CurrentPrice { get; set; }
        [JsonPropertyName("order_side")]
        public OrderSide OrderSide { get; set; }
        [JsonPropertyName("best_bid")]
        public string BestBid { get; set; }
        [JsonPropertyName("best_ask")]
        public string BestAsk { get; set; }
        [JsonPropertyName("bid_max_order_size")]
        public decimal BidMaxOrderSize { get; set; }
        [JsonPropertyName("index_of_max_bid_order_size")]
        public int IndexOfMaxBidOrderSize { get; set; }
        [JsonPropertyName("ask_max_order_size")]
        public decimal AskMaxOrderSize { get; set; }
        [JsonPropertyName("index_of_max_ask_order_size")]
        public int IndexOfMaxAskOrderSize { get; set; }
        [JsonPropertyName("bid_price_and_size")]
        public List<PriceAndSize> BidPriceAndSize { get; set; }
        [JsonPropertyName("ask_price_and_size")]
        public List<PriceAndSize> AskPriceAndSize { get; set; }
        [JsonPropertyName("relative_index_quarterly")]
        public decimal RelativeIndexQuarterly { get; set; }
        [JsonPropertyName("relative_index_daily")]
        public decimal RelativeIndexDaily { get; set; }
        [JsonPropertyName("relative_index_hourly")]
        public decimal RelativeIndexHourly { get; set; }
    }
}
