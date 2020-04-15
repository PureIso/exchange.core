using exchange.core;
using exchange.core.interfaces;
using exchange.core.Interfaces;
using exchange.core.models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.coinbase
{
    public class Coinbase : IExchangeService
    {
        #region Fields
        private readonly SemaphoreSlim _ioRequestSemaphoreSlim;
        private readonly SemaphoreSlim _ioSemaphoreSlim;
        private readonly Authentication _authentication;
        private readonly HttpClient _httpClient;      
        private readonly ClientWebSocket _clientWebsocket;
        #endregion

        #region Events
        public Action<Feed> FeedBroadCast { get; set; }
        #endregion

        #region Virtual Properties
        public virtual WebSocketState GetWebSocketState()
        { return _clientWebsocket.State; }
        #endregion

        #region Public Properties
        public Dictionary<string, decimal> CurrentPrices { get; set; }
        public List<Ticker> Tickers { get; set; }
        public List<Account> Accounts { get; set; }
        public List<Product> Products { get; set; }
        public List<HistoricRate> HistoricRates { get; set; }
        public List<Fill> Fills { get; set; }
        public List<Order> Orders { get; set; }
        public OrderBook OrderBook { get; set; }
        public bool IsWebSocketClosed => !_connectionFactory.IsWebSocketConnected();
        public Product SelectedProduct { get; set; }
        private IConnectionFactory _connectionFactory;
        #endregion

        public Coinbase(IConnectionFactory connectionFactory)
        {
            _authentication = connectionFactory.Authentication;
            _httpClient = connectionFactory.HttpClient;
            _ioRequestSemaphoreSlim = new SemaphoreSlim(1, 1);
            _ioSemaphoreSlim = new SemaphoreSlim(1, 1);
            _clientWebsocket = connectionFactory.ClientWebSocket;
            CurrentPrices = new Dictionary<string, decimal>();
            Tickers = new List<Ticker>();
            _connectionFactory = connectionFactory;
        }

        #region Public Methods

        public async Task<List<Account>> UpdateAccountsAsync(string accountId = "")
        {
            /***
             * Account ID
             * {
    "id": "a1b2c3d4",
    "balance": "1.100",
    "holds": "0.100",
    "available": "1.00",
    "currency": "USD"
}
             */
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/accounts/{accountId}");
            string json = await _connectionFactory.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Accounts;
            Accounts = JsonSerializer.Deserialize<List<Account>>(json);
            return Accounts;
        }
        public async Task<List<Account>> UpdateAccountHistoryAsync(string accountId)
        {
            /***
             * Account History
             *[
    {
        "id": "100",
        "created_at": "2014-11-07T08:19:27.028459Z",
        "amount": "0.001",
        "balance": "239.669",
        "type": "fee",
        "details": {
            "order_id": "d50ec984-77a8-460a-b958-66f114b0de9b",
            "trade_id": "74",
            "product_id": "BTC-USD"
        }
    }
]
             */
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/accounts/{accountId}/ledger");
            string json = await _connectionFactory.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Accounts;
            Accounts = JsonSerializer.Deserialize<List<Account>>(json);
            return Accounts;
        }
        public async Task<List<Account>> UpdateAccountHoldsAsync(string accountId)
        {
            /***
             * Account Holds
             *[
    {
        "id": "82dcd140-c3c7-4507-8de4-2c529cd1a28f",
        "account_id": "e0b3f39a-183d-453e-b754-0c13e5bab0b3",
        "created_at": "2014-11-06T10:34:47.123456Z",
        "updated_at": "2014-11-06T10:40:47.123456Z",
        "amount": "4.23",
        "type": "order",
        "ref": "0a205de4-dd35-4370-a285-fe8fc375a273",
    }
]
             */
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/accounts/{accountId}/holds");
            string json = await _connectionFactory.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Accounts;
            Accounts = JsonSerializer.Deserialize<List<Account>>(json);
            return Accounts;
        }
        public async Task<List<Order>> UpdateOrdersAsync(Product product = null)
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/orders?status=open&status=pending&status=active&product_id={product?.ID ?? string.Empty}");
            string json = await _connectionFactory.RequestAsync(request);
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
            Request request = new Request(_authentication.EndpointUrl, "POST", $"/orders");
            string json = await _connectionFactory.RequestAsync(request);
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
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/products");
            string json = await _connectionFactory.RequestAsync(request);
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
                Request request = new Request(_authentication.EndpointUrl, "GET", $"/products/{product.ID}/ticker");
                string json = await _connectionFactory.RequestAsync(request);
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
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/fills?product_id={product.ID ?? string.Empty}");
            string json = await _connectionFactory.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Fills;
            Fills = JsonSerializer.Deserialize<List<Fill>>(json);
            return Fills;
        }
        
        public async Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2)
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/products/{product.ID}/book?level={level}");
            string json = await _connectionFactory.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return OrderBook;
            OrderBook = JsonSerializer.Deserialize<OrderBook>(json);
            return OrderBook;
        }
        public async Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(Product product, DateTime startingDateTime, DateTime endingDateTime, int granularity = 86400)
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/products/{product.ID}/candles?start={startingDateTime:o}&end={endingDateTime:o}&granularity={granularity}");
            string json = await _connectionFactory.RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return HistoricRates;
            ArrayList[] candles = JsonSerializer.Deserialize<ArrayList[]>(json);
            HistoricRates = candles.ToHistoricRateList();
            return HistoricRates;
        }
        
        public bool Close()
        {
            bool isClosed = _connectionFactory.WebSocketCloseAsync().GetAwaiter().GetResult();
            return isClosed;
        }
        public bool Subscribe(List<Product> products)
        {
            if (products == null || !products.Any())
                return false;
            string productIds = null;
            foreach (Product product in products)
            {
                productIds += $@"""{product.ID}"",";
            }
            if (productIds == null)
                return false;
            productIds = productIds.Remove(productIds.Length - 1, 1);
            string message =
                $@"{{""type"": ""subscribe"",""channels"": [{{""name"": ""ticker"",""product_ids"": [{productIds}]}}]}}";
            string json = _connectionFactory.WebSocketSendAsync(message).Result;
            Feed feed = JsonSerializer.Deserialize<Feed>(json);
            if (feed == null || feed.Type == "error")
                return false;

            Task.Run(async () =>
            {
                try
                {
                    while (_connectionFactory.IsWebSocketConnected())
                    {
                        json = await _connectionFactory.WebSocketReceiveAsync().ConfigureAwait(false);
                        feed = JsonSerializer.Deserialize<Feed>(json);
                        if (feed == null || feed.Type == "error")
                        {
                            //IsWebSocketClosed = true;
                            return;
                        }
                        FeedBroadCast?.Invoke(feed);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            });
            Task.Delay(1000).Wait();
            return true;
        }
        #endregion

       
    }
}