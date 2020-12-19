using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using exchange.core.enums;

namespace exchange.core.models
{
    public class AssetInformation
    {
        [JsonPropertyName("product_id")]
        public string ProductID { get; set; }
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
        [JsonPropertyName("base_currency_balance")]
        public decimal BaseCurrencyBalance { get; set; }
        [JsonPropertyName("base_currency_available")]
        public decimal BaseCurrencyAvailable { get; set; }
        [JsonPropertyName("base_currency_hold")]
        public decimal BaseCurrencyHold { get; set; }
        [JsonPropertyName("base_currency_symbol")]
        public string BaseCurrencySymbol { get; set; }
        [JsonPropertyName("quote_currency_balance")]
        public decimal QuoteCurrencyBalance { get; set; }
        [JsonPropertyName("quote_currency_available")]
        public decimal QuoteCurrencyAvailable { get; set; }
        [JsonPropertyName("quote_currency_hold")]
        public decimal QuoteCurrencyHold { get; set; }
        [JsonPropertyName("quote_currency_symbol")]
        public string QuoteCurrencySymbol { get; set; }
        [JsonPropertyName("base_and_quote_price")]
        public decimal BaseAndQuotePrice { get; set; }
        [JsonPropertyName("base_and_selected_main_price")]
        public decimal BaseAndSelectedMainPrice { get; set; }
        [JsonPropertyName("selected_main_currency_balance")]
        public decimal SelectedMainCurrencyBalance { get; set; }
        [JsonPropertyName("selected_main_currency_available")]
        public decimal SelectedMainCurrencyAvailable { get; set; }
        [JsonPropertyName("selected_main_currency_hold")]
        public decimal SelectedMainCurrencyHold { get; set; }
        [JsonPropertyName("selected_main_currency_symbol")]
        public string SelectedMainCurrencySymbol { get; set; }
        [JsonPropertyName("base_and_selected_main_balance")]
        public decimal BaseAndSelectedMainBalance { get; set; }
        [JsonPropertyName("base_and_quote_balance")]
        public decimal BaseAndQuoteBalance { get; set; }
        [JsonPropertyName("aggregated_selected_main_balance")]
        public decimal AggregatedSelectedMainBalance { get; set; }
        [JsonPropertyName("is_volume_buy_side")]
        public bool IsVolumeBuySide { get; set; }
        [JsonPropertyName("size_percentage_difference")]
        public decimal SizePercentageDifference { get; set; }

        public void RoundDecimals()
        {
            BaseAndSelectedMainBalance = Math.Round(BaseAndSelectedMainBalance, 2);
            BaseAndQuoteBalance = Math.Round(BaseAndQuoteBalance, 2);
            SelectedMainCurrencyBalance = Math.Round(SelectedMainCurrencyBalance, 2);
            AggregatedSelectedMainBalance = Math.Round(AggregatedSelectedMainBalance, 2);
            BaseCurrencyBalance = Math.Round(BaseCurrencyBalance, 2);
            QuoteCurrencyBalance = Math.Round(QuoteCurrencyBalance, 2);

        }
    }
}
