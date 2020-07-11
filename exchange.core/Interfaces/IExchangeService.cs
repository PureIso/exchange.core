using exchange.core.models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.Enums;
using exchange.core.Models;

namespace exchange.core.interfaces
{
    /// <summary>
    /// All exchange services have to implement the IExchangeService interface
    /// This interface will also be used to forward Hub Requests
    /// </summary>
    public interface IExchangeService
    {
        #region Actions
        Action<Feed> FeedBroadcast { get; set; }
        Action<MessageType, string> ProcessLogBroadcast { get; set; }
        #endregion

        #region Public Properties
        public ServerTime ServerTime { get; set; }
        Dictionary<string, decimal> CurrentPrices { get; set; }
        List<Ticker> Tickers { get; set; }
        List<Account> Accounts { get; set; }
        List<Product> Products { get; set; }
        List<HistoricRate> HistoricRates { get; set; }
        List<Fill> Fills { get; set; }
        List<BinanceFill> BinanceFill { get; set; }
        List<Order> Orders { get; set; }
        OrderBook OrderBook { get; set; }
        Product SelectedProduct { get; set; } 
        List<AccountHistory> AccountHistories { get; set; } 
        List<AccountHold> AccountHolds { get; set; }
        #endregion

        #region Methods

        Task<BinanceAccount> UpdateBinanceAccountAsync();
        Task<ExchangeInfo> UpdateExchangeInfoAsync();
        Task<ServerTime> UpdateTimeServerAsync();
        Task<List<Account>> UpdateAccountsAsync(string accountId="");
        Task<List<AccountHistory>> UpdateAccountHistoryAsync(string accountId);
        Task<List<AccountHold>> UpdateAccountHoldsAsync(string accountId);
        Task<List<Order>> UpdateOrdersAsync(Product product = null);
        Task<Order> PostOrdersAsync(Order order);
        Task<List<Order>> CancelOrderAsync(Order order);
        Task<List<Order>> CancelOrdersAsync(Product product);
        Task<List<Product>> UpdateProductsAsync();
        Task<List<Ticker>> UpdateTickersAsync(List<Product> products);
        Task<List<Fill>> UpdateFillsAsync(Product product);
        Task<List<BinanceFill>> UpdateBinanceFillsAsync(Product product);
        Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2);
        Task<List<BinanceFill>> BinancePostOrdersAsync(BinanceOrder order);
        Task<List<BinanceOrder>> BinanceCancelOrdersAsync(BinanceOrder binanceOrder);
        Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(Product product, DateTime startingDateTime, DateTime endingDateTime, int granularity = 86400);
        Task<bool> CloseFeed();
        bool ChangeFeed(string message);
        void StartProcessingFeed();
        #endregion
    }
}
