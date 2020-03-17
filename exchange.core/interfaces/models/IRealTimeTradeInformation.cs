namespace exchange.core.interfaces.models
{
    public interface IRealTimeTradeInformation
    {
        bool IsSideBuy();
        IProduct Product { get; set; } 
        decimal CurrentTradingPrice { get; set; } 
        decimal PreviousTradingPrice { get; set; } 
        decimal SellVolume { get; set; } 
        decimal BuyVolume { get; set; } 
        decimal LargestSellOrder { get; set; } 
        decimal LargestBuyOrder { get; set; } 
        decimal VolumePercentageDifference { get; set; } 
        int LargestSellOrderIndex { get; set; } 
        int LargestBuyOrderIndex { get; set; } 
        decimal TopSellPrice { get; set; } 
        decimal TopBuyPrice { get; set; }
    }
}
