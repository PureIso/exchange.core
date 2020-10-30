using System.Collections.Generic;
using exchange.core.Enums;

namespace exchange.core.models
{
    public class AssetInformation
    {
        public decimal TwentyFourHourPriceChange { get; set; }
        public decimal TwentyFourHourPricePercentageChange { get; set; }
        public decimal CurrentPrice { get; set; }
        public OrderSide OrderSide { get; set; }
        public string BestBid { get; set; }
        public string BestAsk { get; set; }
        public decimal BidMaxOrderSize { get; set; }
        public int IndexOfMaxBidOrderSize { get; set; }
        public decimal AskMaxOrderSize { get; set; }
        public int IndexOfMaxAskOrderSize { get; set; }
        public List<PriceAndSize> BidPriceAndSize { get; set; }
        public List<PriceAndSize> AskPriceAndSize { get; set; }
    }
}
