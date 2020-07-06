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
using exchange.core.Enums;
using exchange.core.Models;

namespace exchange.coinbase
{
    public class Coinbase : IExchangeService, IDisposable
    {
        #region Fields

        private readonly IConnectionAdapter _connectionAdapter;

        #endregion

        #region Events

        public Action<Feed> FeedBroadcast { get; set; }
        public Action<MessageType, string> ProcessLogBroadcast { get; set; }
        public ServerTime ServerTime { get; set; }

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
            Accounts = new List<Account>();
            AccountHistories = new List<AccountHistory>();
            AccountHolds = new List<AccountHold>();
            Products = new List<Product>();
            HistoricRates = new List<HistoricRate>();
            Fills = new List<Fill>();
            Orders = new List<Order>();
            OrderBook = new OrderBook();
            SelectedProduct = new Product();
            _connectionAdapter = connectionAdapter;
        }

        #region Public Methods

        #region Trading

        public Task<BinanceAccount> UpdateBinanceAccountAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ExchangeInfo> UpdateExchangeInfoAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ServerTime> UpdateTimeServerAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<List<Account>> UpdateAccountsAsync(string accountId = "")
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Account Information.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/accounts/{accountId}");
                string json = await _connectionAdapter.RequestAsync(request);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateAccountsAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                Accounts = JsonSerializer.Deserialize<List<Account>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateAccountsAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return Accounts;
        }

        public async Task<List<AccountHistory>> UpdateAccountHistoryAsync(string accountId)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Account History Information.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/accounts/{accountId}/ledger");
                string json = await _connectionAdapter.RequestAsync(request);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateAccountHistoryAsync JSON:\r\n{json}");
                AccountHistories = JsonSerializer.Deserialize<List<AccountHistory>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateAccountHistoryAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return AccountHistories;
        }

        public async Task<List<AccountHold>> UpdateAccountHoldsAsync(string accountId)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Account Holds Information.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/accounts/{accountId}/holds");
                string json = await _connectionAdapter.RequestAsync(request);
                AccountHolds = JsonSerializer.Deserialize<List<AccountHold>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateAccountHoldsAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return AccountHolds;
        }

        public async Task<List<Order>> UpdateOrdersAsync(Product product = null)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Orders Information.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/orders?status=open&status=pending&status=active&product_id={product?.ID ?? string.Empty}");
                string json = await _connectionAdapter.RequestAsync(request);
                Orders = JsonSerializer.Deserialize<List<Order>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return Orders;
        }

        public async Task<Order> PostOrdersAsync(Order order)
        {
            Order outputOrder = null;
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Post Order Information.");
                object data;
                if (order.Type == OrderType.Market.GetStringValue() || string.IsNullOrEmpty(order.Price))
                    data = new
                    {
                        size = order.Size,
                        side = order.Side,
                        type = OrderType.Market.GetStringValue(),
                        product_id = order.ProductID,
                        stp = order.SelfTradePreventionType
                    };
                else
                    data = new
                    {
                        size = order.Size,
                        price = order.Price,
                        side = order.Side,
                        type = OrderType.Limit.GetStringValue(),
                        product_id = order.ProductID,
                        stp = order.SelfTradePreventionType
                    };
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "POST", $"/orders")
                {
                    RequestBody = JsonSerializer.Serialize(data)
                };
                string json = await _connectionAdapter.RequestAsync(request);
                outputOrder = JsonSerializer.Deserialize<Order>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: PostOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return outputOrder;
        }

        public async Task<List<Order>> CancelOrderAsync(Order order)
        {
            List<Order> ordersOutput = new List<Order>();
            try
            {
                if (order == null)
                    return ordersOutput;
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Cancel Orders Information.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "DELETE",
                    $"/orders/{order.ID ?? string.Empty}");
                string json = await _connectionAdapter.RequestAsync(request);
                if (!json.StartsWith('[') && !json.EndsWith(']'))
                {
                    string orderId = JsonSerializer.Deserialize<string>(json);
                    if (string.IsNullOrEmpty(orderId))
                        return ordersOutput;
                    ordersOutput = Orders.Where(x => x.ID == orderId)?.ToList();
                    int removed = Orders.RemoveAll(x => x.ID == orderId);
                    ProcessLogBroadcast?.Invoke(MessageType.General,
                        removed > 0
                            ? $"Removing Order IDs: {orderId} from Orders."
                            : $"No update from order cancel\r\nRequested URL: {request.RequestUrl}");
                    if (!ordersOutput.Any())
                        ordersOutput.Add(new Order{ID = orderId });
                    return ordersOutput;
                }
                else
                {
                    List<string> orderIds = JsonSerializer.Deserialize<string[]>(json)?.ToList();
                    if (orderIds == null)
                        return ordersOutput;
                    ordersOutput = Orders.Where(x => orderIds.Contains(x.ID))?.ToList();
                    int removed = Orders.RemoveAll(x => orderIds.Contains(x.ID));
                    ProcessLogBroadcast?.Invoke(MessageType.General,
                        removed > 0
                            ? $"Removing Order IDs: {orderIds} from Orders."
                            : $"No update from order cancel\r\nRequested URL: {request.RequestUrl}");
                    if (!ordersOutput.Any())
                        ordersOutput = (from id in orderIds select new Order { ID = id })?.ToList();
                    return ordersOutput;
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: CancelOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return ordersOutput;
        }

        public async Task<List<Order>> CancelOrdersAsync(Product product)
        {
            List<Order> ordersOutput = new List<Order>();
            try
            {
                if (product == null)
                    return ordersOutput;
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Cancel Orders Information.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "DELETE",
                    $"/orders?product_id={product.ID ?? string.Empty}");
                string json = await _connectionAdapter.RequestAsync(request);
                if (!json.StartsWith('[') && !json.EndsWith(']'))
                {
                    string orderId = JsonSerializer.Deserialize<string>(json);
                    if (string.IsNullOrEmpty(orderId))
                        return ordersOutput;
                    ordersOutput = Orders.Where(x => x.ID == orderId)?.ToList();
                    int removed = Orders.RemoveAll(x => x.ID == orderId);
                    ProcessLogBroadcast?.Invoke(MessageType.General,
                        removed > 0
                            ? $"Removing Order IDs: {orderId} from Orders."
                            : $"No update from order cancel\r\nRequested URL: {request.RequestUrl}");
                    if (!ordersOutput.Any())
                        ordersOutput.Add(new Order { ID = orderId });
                    return ordersOutput;
                }
                else
                {
                    List<string> orderIds = JsonSerializer.Deserialize<string[]>(json)?.ToList();
                    if (orderIds == null)
                        return ordersOutput;
                    ordersOutput = Orders.Where(x => orderIds.Contains(x.ID))?.ToList();
                    int removed = Orders.RemoveAll(x => orderIds.Contains(x.ID));
                    ProcessLogBroadcast?.Invoke(MessageType.General,
                        removed > 0
                            ? $"Removing Order IDs: {orderIds} from Orders."
                            : $"No update from order cancel\r\nRequested URL: {request.RequestUrl}");
                    if (!ordersOutput.Any())
                        ordersOutput = (from id in orderIds select new Order { ID = id })?.ToList();
                    return ordersOutput;
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: CancelOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return ordersOutput;
        }

        public async Task<List<Product>> UpdateProductsAsync()
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Update Products Information.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/products");
                string json = await _connectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    Products = JsonSerializer.Deserialize<List<Product>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateProductsAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return Products;
        }

        public async Task<List<Ticker>> UpdateTickersAsync(List<Product> products)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Update Tickers Information.");
                if (products == null || !products.Any())
                    return Tickers;
                if (Tickers == null)
                    Tickers = new List<Ticker>();
                //Get price of all products
                foreach (Product product in products)
                {
                    Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                        $"/products/{product.ID}/ticker");
                    string json = await _connectionAdapter.RequestAsync(request);
                    if (string.IsNullOrEmpty(json))
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
                    if (decimal.TryParse(ticker.Price, out decimal decimalPrice))
                        CurrentPrices[ticker.ProductID] = decimalPrice;
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateTickersAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return Tickers;
        }

        public async Task<List<Fill>> UpdateFillsAsync(Product product)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Fills Information.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/fills?product_id={product.ID ?? string.Empty}");
                string json = await _connectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    Fills = JsonSerializer.Deserialize<List<Fill>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateFillsAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return Fills;
        }

        public async Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Product Orders Information.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/products/{product.ID}/book?level={level}");
                string json = await _connectionAdapter.RequestAsync(request);
                OrderBook = JsonSerializer.Deserialize<OrderBook>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateProductOrderBookAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return OrderBook;
        }

        public async Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(Product product,
            DateTime startingDateTime, DateTime endingDateTime, int granularity = 86400)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Product Historic Candles Information.");
                if (startingDateTime.AddMilliseconds(granularity) >= endingDateTime)
                    return null;
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/products/{product.ID}/candles?start={startingDateTime:o}&end={endingDateTime:o}&granularity={granularity}");
                string json = await _connectionAdapter.RequestAsync(request);
                ArrayList[] candles = JsonSerializer.Deserialize<ArrayList[]>(json);
                HistoricRates = candles.ToHistoricRateList();
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateProductHistoricCandlesAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return HistoricRates;
        }

        #endregion

        #region Feed

        public bool ChangeFeed(string message)
        {
            Feed feed = null;
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Subscribing to Feed Information.");
                string json = _connectionAdapter.WebSocketSendAsync(message).Result;
                if (string.IsNullOrEmpty(json))
                    return false;
                feed = JsonSerializer.Deserialize<Feed>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: Subscribe\r\nException Stack Trace: {e.StackTrace}");
            }

            return feed != null && feed.Type != "error";
        }

        public void StartProcessingFeed()
        {
            Task.Run(async () =>
            {
                try
                {
                    ProcessLogBroadcast?.Invoke(MessageType.General, $"Started Processing Feed Information.");
                    while (_connectionAdapter.IsWebSocketConnected())
                    {
                        string json = await _connectionAdapter.WebSocketReceiveAsync().ConfigureAwait(false);
                        Feed feed = JsonSerializer.Deserialize<Feed>(json);
                        if (feed == null || feed.Type == "error")
                            return;
                        FeedBroadcast?.Invoke(feed);
                    }
                }
                catch (Exception e)
                {
                    ProcessLogBroadcast?.Invoke(MessageType.Error,
                        $"Method: StartProcessingFeed\r\nException Stack Trace: {e.StackTrace}");
                }
            });
        }

        public async Task<bool> CloseFeed()
        {
            bool isClosed = false;
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Closing Feed Subscription.");
                isClosed = await _connectionAdapter.WebSocketCloseAsync();
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: CloseFeed\r\nException Stack Trace: {e.StackTrace}");
            }
            return isClosed;
        }

        #endregion

        public void Dispose()
        {
            _connectionAdapter?.Dispose();
            CurrentPrices = null;
            Tickers = null;
            Accounts = null;
            AccountHistories = null;
            AccountHolds = null;
            Products = null;
            HistoricRates = null;
            Fills = null;
            Orders = null;
            OrderBook = null;
            SelectedProduct = null;
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}