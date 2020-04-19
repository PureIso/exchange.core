using exchange.core;
using exchange.core.interfaces;
using exchange.core.Interfaces;
using exchange.core.models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using exchange.core.Models;

namespace exchange.coinbase
{
    public class Coinbase : IExchangeService, IDisposable
    {
        #region Fields
        private readonly IConnectionAdapter _connectionAdapter;
        #endregion

        #region Events
        public Action<Feed> FeedBroadCast { get; set; }
        #endregion

        #region Public Properties
        public Dictionary<string, decimal> CurrentPrices { get; set; }
        public List<Ticker> Tickers { get; set; }
        public List<Account> Accounts { get; set; }
        public List<AccountHistory> AccountHistories { get; set; }
        public List<AccountHold> AccountHolds { get; set; }
        public List<Product> Products { get; set; }
        public List<HistoricRate> HistoricRates { get; set; }
        public List<Fill> Fills { get; set; }
        public List<Order> Orders { get; set; }
        public OrderBook OrderBook { get; set; }
        public Product SelectedProduct { get; set; }
        
        #endregion

        public Coinbase(IConnectionAdapter connectionAdapter)
        {
            CurrentPrices = new Dictionary<string, decimal>();
            Tickers = new List<Ticker>();
            _connectionAdapter = connectionAdapter;
        }

        #region Public Methods

        public async Task<List<Account>> UpdateAccountsAsync(string accountId = "")
        {
            Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/accounts/{accountId}");
            string json = await _connectionAdapter.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Accounts;
            Accounts = JsonSerializer.Deserialize<List<Account>>(json);
            return Accounts;
        }
        public async Task<List<AccountHistory>> UpdateAccountHistoryAsync(string accountId)
        {
            Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/accounts/{accountId}/ledger");
            string json = await _connectionAdapter.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return AccountHistories;
            AccountHistories = JsonSerializer.Deserialize<List<AccountHistory>>(json);
            return AccountHistories;
        }
        public async Task<List<AccountHold>> UpdateAccountHoldsAsync(string accountId)
        {
            Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/accounts/{accountId}/holds");
            string json = await _connectionAdapter.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return AccountHolds;
            AccountHolds = JsonSerializer.Deserialize<List<AccountHold>>(json);
            return AccountHolds;
        }
        public async Task<List<Order>> UpdateOrdersAsync(Product product = null)
        {
            Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/orders?status=open&status=pending&status=active&product_id={product?.ID ?? string.Empty}");
            string json = await _connectionAdapter.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Orders;
            Orders = JsonSerializer.Deserialize<List<Order>>(json);
            return Orders;
        }
        public async Task<List<Order>> PostOrdersAsync(Product product = null)
        {
            /***
             * {
    "size": "0.01",
    "price": "0.100",
    "side": "buy",
    "product_id": "BTC-USD"
}
             */
            Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "POST", $"/orders");
            string json = await _connectionAdapter.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Orders;
            Orders = JsonSerializer.Deserialize<List<Order>>(json);
            return Orders;

            /***
             *{
    "id": "d0c5340b-6d6c-49d9-b567-48c4bfca13d2",
    "price": "0.10000000",
    "size": "0.01000000",
    "product_id": "BTC-USD",
    "side": "buy",
    "stp": "dc",
    "type": "limit",
    "time_in_force": "GTC",
    "post_only": false,
    "created_at": "2016-12-08T20:02:28.53864Z",
    "fill_fees": "0.0000000000000000",
    "filled_size": "0.00000000",
    "executed_value": "0.0000000000000000",
    "status": "pending",
    "settled": false
}
             */
        }

        public async Task<List<Product>> UpdateProductsAsync()
        {
            Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/products");
            string json = await _connectionAdapter.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Products;
            Products = JsonSerializer.Deserialize<List<Product>>(json);
            return Products;
        }
        public async Task<List<Ticker>> UpdateTickersAsync(List<Product> products)
        {
            if (products == null || !products.Any())
                return Tickers;
            if (Tickers == null)
                Tickers = new List<Ticker>();
            //Get price of all products
            foreach (Product product in products)
            {
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/products/{product.ID}/ticker");
                string json = await _connectionAdapter.RequestAsync(request);
                if (string.IsNullOrWhiteSpace(json))
                    return Tickers;
                Ticker ticker = JsonSerializer.Deserialize<Ticker>(json);
                Tickers?.RemoveAll(x => x.ProductID == product.ID);
                if (ticker == null) 
                    continue;
                ticker.ProductID = product.ID;
                Tickers.Add(ticker);
            }
            foreach (Ticker ticker in Tickers)
            {
                if(decimal.TryParse(ticker.Price, out decimal decimalPrice))
                    CurrentPrices[ticker.ProductID] = decimalPrice;
            }
            return Tickers;
        }
        public async Task<List<Fill>> UpdateFillsAsync(Product product)
        {
            Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/fills?product_id={product.ID ?? string.Empty}");
            string json = await _connectionAdapter.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Fills;
            Fills = JsonSerializer.Deserialize<List<Fill>>(json);
            return Fills;
        }
        
        public async Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2)
        {
            Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/products/{product.ID}/book?level={level}");
            string json = await _connectionAdapter.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return OrderBook;
            OrderBook = JsonSerializer.Deserialize<OrderBook>(json);
            return OrderBook;
        }
        public async Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(Product product, DateTime startingDateTime, DateTime endingDateTime, int granularity = 86400)
        {
            Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/products/{product.ID}/candles?start={startingDateTime:o}&end={endingDateTime:o}&granularity={granularity}");
            string json = await _connectionAdapter.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return HistoricRates;
            ArrayList[] candles = JsonSerializer.Deserialize<ArrayList[]>(json);
            HistoricRates = candles.ToHistoricRateList();
            return HistoricRates;
        }
        public bool Subscribe(string message)
        {
            string json = _connectionAdapter.WebSocketSendAsync(message).Result;
            if (string.IsNullOrEmpty(json))
                return false;
            Feed feed = JsonSerializer.Deserialize<Feed>(json);
            return feed != null && feed.Type != "error";
        }
        public void ProcessFeed()
        {
            Task.Run(async () =>
            {
                try
                {
                    while (_connectionAdapter.IsWebSocketConnected())
                    {
                        string json = await _connectionAdapter.WebSocketReceiveAsync().ConfigureAwait(false);
                        Feed feed = JsonSerializer.Deserialize<Feed>(json);
                        if (feed == null || feed.Type == "error")
                            return; 
                        FeedBroadCast?.Invoke(feed);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            });
        }
        public async Task<bool> Close()
        {
            bool isClosed = await _connectionAdapter.WebSocketCloseAsync();
            return isClosed;
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}