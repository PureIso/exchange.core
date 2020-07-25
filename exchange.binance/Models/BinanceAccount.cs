using System.Text.Json.Serialization;

namespace exchange.binance.models
{
    public class BinanceAccount
    {
        [JsonPropertyName("makerCommission")]
        public int MakerCommission { get; set; }
        [JsonPropertyName("takerCommission")]
        public int TakerCommission { get; set; }
        [JsonPropertyName("buyerCommission")]
        public int BuyerCommission { get; set; }
        [JsonPropertyName("sellerCommission")]
        public int SellerCommission { get; set; }
        [JsonPropertyName("canTrade")]
        public bool CanTrade { get; set; }
        [JsonPropertyName("canWithdraw")]
        public bool CanWithdraw { get; set; }
        [JsonPropertyName("canDeposit")]
        public bool CanDeposit { get; set; }
        [JsonPropertyName("updateTime")]
        public long UpdateTime { get; set; }
        [JsonPropertyName("accountType")]
        public string AccountType { get; set; }
        [JsonPropertyName("balances")]
        public Asset[] Balances { get; set; }
    }
}
