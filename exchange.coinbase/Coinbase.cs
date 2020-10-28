using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase.models;
using exchange.core.Enums;
using exchange.core.helpers;
using exchange.core.implementations;
using exchange.core.Indicators;
using exchange.core.models;
using exchange.core.Models;

namespace exchange.coinbase
{
    public class Coinbase : AbstractExchangePlugin, IDisposable
    {
        #region Fields

        private readonly object _ioLock;

        #endregion

        public Coinbase()
        {
            Tickers = new List<Ticker>();
            Accounts = new List<Account>();
            AccountHistories = new List<AccountHistory>();
            AccountHolds = new List<AccountHold>();
            HistoricRates = new List<HistoricRate>();
            Fills = new List<Fill>();
            Orders = new List<Order>();
            OrderBook = new OrderBook();
            SelectedProduct = new Product();
            _ioLock = new object();
        }

        private void LoadINI(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) 
                    return;
                Authentication ??= new Authentication();
                string line;
                StreamReader streamReader = new StreamReader(filePath);
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;
                    string env = TestMode ? "test" : "live";
                    if (line.StartsWith($"{env}_uri="))
                    {
                        line = line.Replace($"{env}_uri=", "").Trim();
                        if (string.IsNullOrEmpty(line))
                            continue;
                        Authentication.WebSocketUri = new Uri(line);
                    }
                    else if (line.StartsWith($"{env}_key="))
                    {
                        line = line.Replace($"{env}_key=", "").Trim();
                        if (string.IsNullOrEmpty(line))
                            continue;
                        Authentication.ApiKey = line;
                    }
                    else if (line.StartsWith($"{env}_secret="))
                    {
                        line = line.Replace($"{env}_secret=", "").Trim();
                        if (string.IsNullOrEmpty(line))
                            continue;
                        Authentication.Secret = line;
                    }
                    else if (line.StartsWith($"{env}_endpoint="))
                    {
                        line = line.Replace($"{env}_endpoint=", "").Trim();
                        if (string.IsNullOrEmpty(line))
                            continue;
                        Authentication.EndpointUrl = line;
                    }
                    else if (line.StartsWith($"{env}_passphrase="))
                    {
                        line = line.Replace($"{env}_passphrase=", "").Trim();
                        if (string.IsNullOrEmpty(line))
                            continue;
                        Authentication.Passphrase = line;
                    }
                }

                ConnectionAdapter.Authentication = Authentication;
                ConnectionAdapter.ClientWebSocket = ClientWebSocket;
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: LoadINI\r\nException Stack Trace: {e.StackTrace}");
            }
        }
        private void Save()
        {
            string json = null;
            try
            {
                lock (_ioLock)
                {
                    if (string.IsNullOrEmpty(FileName))
                    {
                        string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                        string env = TestMode ? "test" : "live";
                        FileName = Path.Combine(directoryName, $"data\\coinbase_{env}.json");
                        if (!File.Exists(FileName))
                            File.Create(FileName).Close();
                    }

                    CoinbaseSettings coinbaseSettings = new CoinbaseSettings();
                    coinbaseSettings.Accounts = Accounts;
                    coinbaseSettings.CurrentPrices = CurrentPrices;
                    coinbaseSettings.Tickers = Tickers;
                    json = JsonSerializer.Serialize(coinbaseSettings, new JsonSerializerOptions {WriteIndented = true});
                    File.WriteAllText(FileName, json);
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: Save\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }
        }
        private void Load()
        {
            string json = null;
            try
            {
                if (string.IsNullOrEmpty(FileName))
                    return;
                if (!File.Exists(FileName))
                    return;
                json = File.ReadAllText(FileName);
                CoinbaseSettings coinbaseSettings = JsonSerializer.Deserialize<CoinbaseSettings>(json);
                if (coinbaseSettings == null)
                    return;
                coinbaseSettings.Accounts = Accounts;
                coinbaseSettings.CurrentPrices = CurrentPrices;
                coinbaseSettings.Tickers = Tickers;
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: Load\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }
        }

        #region Public Properties
        public string FileName { get; set; }
        public List<Ticker> Tickers { get; set; }
        public List<Account> Accounts { get; set; }
        public List<AccountHistory> AccountHistories { get; set; }
        public List<AccountHold> AccountHolds { get; set; }
        public List<HistoricRate> HistoricRates { get; set; }
        public List<Fill> Fills { get; set; }
        public List<Order> Orders { get; set; }
        public OrderBook OrderBook { get; set; }
        public Product SelectedProduct { get; set; }
        #endregion

        #region Public Methods

        #region Trading
        public async Task<List<Account>> UpdateAccountsAsync(string accountId = "")
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General, "Updating Account Information.");
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/accounts/{accountId}");
                json = await ConnectionAdapter.RequestAsync(request);
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.JsonOutput,
                    $"UpdateAccountsAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                Accounts = JsonSerializer.Deserialize<List<Account>>(json);
                if (Accounts != null)
                {
                    Accounts.ForEach(account =>
                    {
                        AccountInfo ??= new Dictionary<string, decimal>();
                        if (account.Balance.ToDecimal() <= 0)
                            return;
                        if (AccountInfo.ContainsKey(account.Currency))
                            AccountInfo[account.Currency] = account.Balance.ToDecimal();
                        else
                            AccountInfo.Add(account.Currency, account.Balance.ToDecimal());
                    });
                    NotifyAccountInfo?.Invoke(ApplicationName, AccountInfo);
                    Save();
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateAccountsAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return Accounts;
        }
        public override async Task<Statistics> TwentyFourHoursRollingStatsAsync(Product product)
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "Updating 24 hour stats Information.");
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/products/{product.ID}/stats");
                json = await ConnectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                {
                    Statistics statistics = JsonSerializer.Deserialize<Statistics>(json);
                    Statistics[product.ID] = statistics;
                    return statistics;
                }

                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.JsonOutput,
                    $"TwentyFourHoursRollingStatsAsync JSON:\r\n{json}");
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: TwentyFourHoursRollingStatsAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return null;
        }
        public async Task<List<AccountHistory>> UpdateAccountHistoryAsync(string accountId)
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "Updating Account History Information.");
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/accounts/{accountId}/ledger");
                json = await ConnectionAdapter.RequestAsync(request);
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.JsonOutput,
                    $"UpdateAccountHistoryAsync JSON:\r\n{json}");
                AccountHistories = JsonSerializer.Deserialize<List<AccountHistory>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateAccountHistoryAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return AccountHistories;
        }
        public async Task<List<AccountHold>> UpdateAccountHoldsAsync(string accountId)
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "Updating Account Holds Information.");
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/accounts/{accountId}/holds");
                json = await ConnectionAdapter.RequestAsync(request);
                AccountHolds = JsonSerializer.Deserialize<List<AccountHold>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateAccountHoldsAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return AccountHolds;
        }
        public async Task<List<Order>> UpdateOrdersAsync(Product product = null)
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General, "Updating Orders Information.");
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/orders?status=open&status=pending&status=active&product_id={product?.ID ?? string.Empty}");
                json = await ConnectionAdapter.RequestAsync(request);
                Orders = JsonSerializer.Deserialize<List<Order>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateOrdersAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return Orders;
        }
        public async Task<Order> PostOrdersAsync(Order order)
        {
            string json = null;
            Order outputOrder = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General, "Updating Post Order Information.");
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
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "POST", "/orders")
                {
                    RequestBody = JsonSerializer.Serialize(data)
                };
                json = await ConnectionAdapter.RequestAsync(request);
                outputOrder = JsonSerializer.Deserialize<Order>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: PostOrdersAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return outputOrder;
        }
        public async Task<List<Order>> CancelOrderAsync(Order order)
        {
            string json = null;
            List<Order> ordersOutput = new List<Order>();
            try
            {
                if (order == null)
                    return ordersOutput;
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "Updating Cancel Orders Information.");
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "DELETE",
                    $"/orders/{order.ID ?? string.Empty}");
                json = await ConnectionAdapter.RequestAsync(request);
                if (!json.StartsWith('[') && !json.EndsWith(']'))
                {
                    string orderId = JsonSerializer.Deserialize<string>(json);
                    if (string.IsNullOrEmpty(orderId))
                        return ordersOutput;
                    ordersOutput = Orders.Where(x => x.ID == orderId)?.ToList();
                    int removed = Orders.RemoveAll(x => x.ID == orderId);
                    ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                        removed > 0
                            ? $"Removing Order IDs: {orderId} from Orders."
                            : $"No update from order cancel\r\nRequested URL: {request.RequestUrl}");
                    if (!ordersOutput.Any())
                        ordersOutput.Add(new Order {ID = orderId});
                    return ordersOutput;
                }
                else
                {
                    List<string> orderIds = JsonSerializer.Deserialize<string[]>(json)?.ToList();
                    if (orderIds == null)
                        return ordersOutput;
                    ordersOutput = Orders.Where(x => orderIds.Contains(x.ID))?.ToList();
                    int removed = Orders.RemoveAll(x => orderIds.Contains(x.ID));
                    ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                        removed > 0
                            ? $"Removing Order IDs: {orderIds} from Orders."
                            : $"No update from order cancel\r\nRequested URL: {request.RequestUrl}");
                    if (!ordersOutput.Any())
                        ordersOutput = (from id in orderIds select new Order {ID = id})?.ToList();
                    return ordersOutput;
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: CancelOrdersAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return ordersOutput;
        }
        public async Task<List<Order>> CancelOrdersAsync(Product product)
        {
            string json = null;
            List<Order> ordersOutput = new List<Order>();
            try
            {
                if (product == null)
                    return ordersOutput;
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "Updating Cancel Orders Information.");
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "DELETE",
                    $"/orders?product_id={product.ID ?? string.Empty}");
                json = await ConnectionAdapter.RequestAsync(request);
                if (!json.StartsWith('[') && !json.EndsWith(']'))
                {
                    string orderId = JsonSerializer.Deserialize<string>(json);
                    if (string.IsNullOrEmpty(orderId))
                        return ordersOutput;
                    ordersOutput = Orders.Where(x => x.ID == orderId)?.ToList();
                    int removed = Orders.RemoveAll(x => x.ID == orderId);
                    ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                        removed > 0
                            ? $"Removing Order IDs: {orderId} from Orders."
                            : $"No update from order cancel\r\nRequested URL: {request.RequestUrl}");
                    if (!ordersOutput.Any())
                        ordersOutput.Add(new Order {ID = orderId});
                    return ordersOutput;
                }
                else
                {
                    List<string> orderIds = JsonSerializer.Deserialize<string[]>(json)?.ToList();
                    if (orderIds == null)
                        return ordersOutput;
                    ordersOutput = Orders.Where(x => orderIds.Contains(x.ID))?.ToList();
                    int removed = Orders.RemoveAll(x => orderIds.Contains(x.ID));
                    ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                        removed > 0
                            ? $"Removing Order IDs: {orderIds} from Orders."
                            : $"No update from order cancel\r\nRequested URL: {request.RequestUrl}");
                    if (!ordersOutput.Any())
                        ordersOutput = (from id in orderIds select new Order {ID = id})?.ToList();
                    return ordersOutput;
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: CancelOrdersAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return ordersOutput;
        }
        public async Task<List<Product>> UpdateProductsAsync()
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "Updating Update Products Information.");
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET", "/products");
                json = await ConnectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    Products = JsonSerializer.Deserialize<List<Product>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateProductsAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return Products;
        }
        public async Task<List<Ticker>> UpdateTickersAsync(List<Product> products)
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "Updating Update Tickers Information.");
                if (products == null || !products.Any())
                    return Tickers;
                if (Tickers == null)
                    Tickers = new List<Ticker>();
                //Get price of all products
                foreach (Product product in products)
                {
                    Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                        $"/products/{product.ID}/ticker");
                    json = await ConnectionAdapter.RequestAsync(request);
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
                    if (decimal.TryParse(ticker.Price, out decimal decimalPrice))
                        CurrentPrices[ticker.ProductID] = decimalPrice;
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateTickersAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return Tickers;
        }
        public async Task<List<Fill>> UpdateFillsAsync(Product product)
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General, "Updating Fills Information.");
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/fills?product_id={product.ID ?? string.Empty}");
                json = await ConnectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    Fills = JsonSerializer.Deserialize<List<Fill>>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateFillsAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return Fills;
        }
        public async Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2)
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "Updating Product Orders Information.");
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/products/{product.ID}/book?level={level}");
                json = await ConnectionAdapter.RequestAsync(request);
                OrderBook = JsonSerializer.Deserialize<OrderBook>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateProductOrderBookAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return OrderBook;
        }
        #endregion

        #region Feed
        public override Task ChangeFeed(List<Product> products)
        {
            return Task.Run(() =>
            {
                ThreadPool.QueueUserWorkItem(x =>
                {
                    SubscribedPrices = new Dictionary<string, decimal>();
                    if (products == null)
                        return;
                    string json = null;
                    try
                    {
                        ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                            "Subscribing to Feed Information.");
                        if (SubscribeProducts != null && SubscribeProducts.Any())
                        {
                            foreach (RelativeStrengthIndex relativeStrengthIndex in RelativeStrengthIndices)
                            {
                                relativeStrengthIndex.DisableRelativeStrengthIndexUpdater();
                            }
                            RelativeStrengthIndices = new List<RelativeStrengthIndex>();
                            //unsubscribe
                            ConnectionAdapter.WebSocketSendAsync(SubscribeProducts.ToUnSubscribeString()).GetAwaiter();
                        }
                        SubscribeProducts = products;
                        InitIndicatorsAsync(products);
                        json = ConnectionAdapter.WebSocketSendAsync(SubscribeProducts.ToSubscribeString()).Result;
                        if (string.IsNullOrEmpty(json))
                            return;
                        ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                            "Started Processing Feed Information.");
                        ConnectionAdapter.ConnectAsync(ConnectionAdapter.Authentication.WebSocketUri.ToString())
                            .GetAwaiter();
                        while (ConnectionAdapter.IsWebSocketConnected())
                        {
                            json = ConnectionAdapter.WebSocketReceiveAsync().ConfigureAwait(false).GetAwaiter()
                                .GetResult();
                            Feed feed = JsonSerializer.Deserialize<Feed>(json);
                            if (feed == null || feed.Type == "error")
                                return;
                            //update current price
                            if (!AssetInformation.ContainsKey(feed.ProductID))
                                AssetInformation.Add(feed.ProductID, new AssetInformation());
                            AssetInformation[feed.ProductID].CurrentPrice = feed.Price.ToDecimal();
                            if (!CurrentPrices.ContainsKey(feed.ProductID))
                                CurrentPrices.Add(feed.ProductID, feed.Price.ToDecimal());
                            CurrentPrices[feed.ProductID] = feed.Price.ToDecimal();
                            SubscribedPrices[feed.ProductID] = feed.Price.ToDecimal();
                            Product product = Products.FirstOrDefault(x => x.ID == feed.ProductID);
                            if (product != null)
                            {
                                Statistics twentyFourHourPrice = TwentyFourHoursRollingStatsAsync(product).Result;
                                decimal priceChangeDifference = (twentyFourHourPrice.Last.ToDecimal() -
                                                       twentyFourHourPrice.High.ToDecimal());
                                decimal change = priceChangeDifference / Math.Abs(twentyFourHourPrice.High.ToDecimal());
                                decimal percentage = change * 100;
                                //update stat changes
                                AssetInformation[feed.ProductID].TwentyFourHourPriceChange = priceChangeDifference;
                                AssetInformation[feed.ProductID].TwentyFourHourPricePercentageChange = percentage;
                                //Order Book
                                OrderBook orderBook = UpdateProductOrderBookAsync(product).GetAwaiter().GetResult();
                                List<Order> bidOrderList = orderBook.Bids.ToOrderList();
                                List<Order> askOrderList = orderBook.Asks.ToOrderList();
                                decimal bidMaxOrderSize = bidOrderList.Max(order => order.Size.ToDecimal());
                                int indexOfMaxBidOrderSize = bidOrderList.FindIndex(a => a.Size.ToDecimal() == bidMaxOrderSize);
                                bidOrderList = bidOrderList.Take(indexOfMaxBidOrderSize + 1).ToList();
                                AssetInformation[feed.ProductID].BidMaxOrderSize = bidMaxOrderSize;
                                AssetInformation[feed.ProductID].IndexOfMaxBidOrderSize = indexOfMaxBidOrderSize;
                                decimal askMaxOrderSize = askOrderList.Max(order => order.Size.ToDecimal());
                                int indexOfMaxAskOrderSize = askOrderList.FindIndex(a => a.Size.ToDecimal() == askMaxOrderSize);
                                AssetInformation[feed.ProductID].AskMaxOrderSize = askMaxOrderSize;
                                AssetInformation[feed.ProductID].IndexOfMaxAskOrderSize = indexOfMaxAskOrderSize;
                                askOrderList = askOrderList.Take(indexOfMaxAskOrderSize + 1).ToList();
                                //price and size
                                AssetInformation[feed.ProductID].BidPriceAndSize = (from orderList in bidOrderList 
                                                                                   select new PriceAndSize { Size = orderList.Size.ToDecimal(), Price = orderList.Price.ToDecimal() }).ToList();
                                AssetInformation[feed.ProductID].AskPriceAndSize = (from orderList in askOrderList
                                                                                    select new PriceAndSize { Size = orderList.Size.ToDecimal(), Price = orderList.Price.ToDecimal() }).ToList();
                            }
                            //update order side
                            if(Enum.TryParse(feed.Side, out OrderSide orderSide))
                                AssetInformation[feed.ProductID].OrderSide = orderSide;
                            //update best bid and ask
                            AssetInformation[feed.ProductID].BestAsk = feed.BestAsk;
                            AssetInformation[feed.ProductID].BestBid = feed.BestBid;
                            //update feed and notifications
                            feed.CurrentPrices = CurrentPrices;
                            CurrentFeed = feed;
                            NotifyCurrentPrices?.Invoke(ApplicationName, SubscribedPrices);
                            //FeedBroadcast?.Invoke(ApplicationName, feed);
                        }
                    }
                    catch (Exception e)
                    {
                        ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                            $"Method: StartProcessingFeed\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
                    }
                });
            });
        }
        #endregion

        public override async Task<bool> InitAsync(bool testMode, string indicatorSaveDataPath, string iniFilePath)
        {
            try
            {
                ApplicationName = "Coinbase Exchange";
                AccountInfo = new Dictionary<string, decimal>();
                CurrentPrices = new Dictionary<string, decimal>();
                SubscribedPrices = new Dictionary<string, decimal>();
                ConnectionAdapter = new ConnectionAdapter();
                Products = new List<Product>();
                Statistics = new Dictionary<string, Statistics>();
                AssetInformation = new Dictionary<string, AssetInformation>();
                RelativeStrengthIndices = new List<RelativeStrengthIndex>();
                TestMode = testMode;
                IndicatorSaveDataPath = indicatorSaveDataPath;
                INIFilePath = iniFilePath;
                if (string.IsNullOrEmpty(INIFilePath))
                {
                    string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        INIFilePath = Path.Combine(directoryName, "coinbase.config.ini");
                    }
                }
                else
                {
                    INIFilePath = Path.Combine(INIFilePath, "coinbase.config.ini");
                }
                LoadINI(INIFilePath); 
                Load();
                await UpdateProductsAsync();
                await UpdateAccountsAsync();
                if (Accounts != null && Accounts.Any())
                {
                    await UpdateAccountHistoryAsync(Accounts[0].ID);
                    await UpdateAccountHoldsAsync(Accounts[0].ID);
                    List<Product> products = new List<Product>
                    {
                        Products.FirstOrDefault(x => x.BaseCurrency == "BTC" && x.QuoteCurrency == "EUR")
                    };
                    products.RemoveAll(x => x == null);
                    if (products.Any())
                        //UpdateProductOrderBookAsync(products[0]).Wait();
                        //UpdateOrdersAsync().Wait();
                        //UpdateFillsAsync(products[0]).Wait();
                        //UpdateTickersAsync(products).Wait();
                        ChangeFeed(products);

                    ////market order
                    ////buy
                    //Order marketOrderBuy = new Order {Size = "0.1", Side = OrderSide.Buy, Type = OrderType.Market, ProductID = "BTC-EUR"};
                    //Order marketBuyOrderResponse = await _exchangeService.PostOrdersAsync(marketOrderBuy);
                    ////sell
                    //Order marketOrderSell = new Order { Size = "0.1", Side = OrderSide.Sell, Type = OrderType.Market, ProductID = "BTC-EUR" };
                    //Order marketSellOrderResponse = await _exchangeService.PostOrdersAsync(marketOrderSell);
                    ////limit order
                    //Order limitOrder = new Order { Size = "0.1", Side = OrderSide.Buy, Type = OrderType.Limit, ProductID = "BTC-EUR", Price = "1000" };
                    //Order limitOrderResponse = await _exchangeService.PostOrdersAsync(limitOrder);
                    ////cancel order
                    //await _exchangeService.CancelOrdersAsync(limitOrderResponse);
                    //List<HistoricRate> historicRates =  await _exchangeService.UpdateProductHistoricCandlesAsync(products[0], 
                    //    DateTime.Now.AddHours(-2).ToUniversalTime(),
                    //    DateTime.Now.ToUniversalTime(), 900);//15 minutes
                    // }
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: InitAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return false;
        }
        public override async Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(
            HistoricCandlesSearch historicCandlesSearch)
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "Updating Product Historic Candles Information.");
                if (historicCandlesSearch.StartingDateTime.AddMilliseconds((double) historicCandlesSearch
                    .Granularity) >= historicCandlesSearch.EndingDateTime)
                    return null;
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/products/{historicCandlesSearch.Symbol}/candles?start={historicCandlesSearch.StartingDateTime:o}&end={historicCandlesSearch.EndingDateTime:o}&granularity={(int) historicCandlesSearch.Granularity}");
                json = await ConnectionAdapter.RequestAsync(request);
                if (json.StartsWith('[') && json.EndsWith(']'))
                {
                    ArrayList[] candles = JsonSerializer.Deserialize<ArrayList[]>(json);
                    HistoricRates = candles.ToHistoricRateList();
                    HistoricRates.Reverse();
                }
                else
                {
                    ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.JsonOutput,
                        $"Method: UpdateProductHistoricCandlesAsync\r\nJSON: {json}");
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateProductHistoricCandlesAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return HistoricRates;
        }
        public override bool InitIndicatorsAsync(List<Product> products)
        {
            string coinbaseRSIFile = null;
            string env = TestMode ? "test" : "live";
            if (string.IsNullOrEmpty(IndicatorSaveDataPath))
            {
                string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                if (!string.IsNullOrEmpty(directoryName))
                    coinbaseRSIFile = Path.Combine(directoryName, $"data\\coinbase_{env}");
            }
            else
            {
                coinbaseRSIFile = Path.Combine(IndicatorSaveDataPath, $"binance_{env}");
            }
            if (!string.IsNullOrEmpty(coinbaseRSIFile)) 
                return false;
            if (products == null)
                return false;
            foreach (Product product in products)
            {
                RelativeStrengthIndex relativeStrengthIndex = new RelativeStrengthIndex(coinbaseRSIFile, product);
                relativeStrengthIndex.TechnicalIndicatorInformationBroadcast +=
                    delegate (Dictionary<string, string> input)
                    {
                        TechnicalIndicatorInformationBroadcast?.Invoke(ApplicationName, input);
                    };
                relativeStrengthIndex.ProcessLogBroadcast += delegate (MessageType messageType, string message)
                {
                    ProcessLogBroadcast?.Invoke(ApplicationName, messageType, message);
                };
                relativeStrengthIndex.UpdateProductHistoricCandles += UpdateProductHistoricCandlesAsync;
                relativeStrengthIndex.EnableRelativeStrengthIndexUpdater();
                RelativeStrengthIndices ??= new List<RelativeStrengthIndex>();
                RelativeStrengthIndices.Add(relativeStrengthIndex);
            }
            return true;
        }
        #endregion
    }
}