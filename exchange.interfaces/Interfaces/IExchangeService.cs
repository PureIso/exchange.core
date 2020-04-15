using exchange.core.models;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace exchange.core.interfaces
{
    /// <summary>
    /// All exchange services have to implement the IExchangeService interface
    /// This interface will also be used to forward Hub Requests
    /// </summary>
    public interface IExchangeService
    {
        Action<Feed> FeedBroadCast { get; set; }

        #region Virtual Properties
        WebSocketState GetWebSocketState();
        #endregion

        #region Public Properties
        Dictionary<string, decimal> CurrentPrices { get; set; }
        List<Ticker> Tickers { get; set; }
        List<Account> Accounts { get; set; }
        List<Product> Products { get; set; }
        List<HistoricRate> HistoricRates { get; set; }
        List<Fill> Fills { get; set; }
        List<Order> Orders { get; set; }
        OrderBook OrderBook { get; set; }
        Product SelectedProduct { get; set; }
        #endregion

        Task<List<Account>> UpdateAccountsAsync(string accountId="");
        Task<List<Product>> UpdateProductsAsync();
        Task<List<Ticker>> UpdateTickersAsync(List<Product> products);
        Task<List<Fill>> UpdateFillsAsync(Product product);
        Task<List<Order>> UpdateOrdersAsync(Product product = null);
        Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2);
        Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(Product product, DateTime startingDateTime, DateTime endingDateTime, int granularity = 86400);

        bool Close();
        bool Subscribe(List<Product> products);
    }
}
