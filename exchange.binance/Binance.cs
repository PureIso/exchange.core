using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using exchange.binance.models;
using exchange.core.Enums;
using exchange.core.helpers;
using exchange.core.implementations;
using exchange.core.indicators;
using exchange.core.interfaces;
using exchange.core.models;

namespace exchange.binance
{
    public class Binance : AbstractExchangePlugin, IDisposable
    {
        #region Fields

        private readonly object _ioLock;

        #endregion

        #region Public Properties
        public string FileName { get; set; }
        public ServerTime ServerTime { get; set; }
        public List<Account> Accounts { get; set; }
        public List<Ticker> Tickers { get; set; }
        public BinanceAccount BinanceAccount { get; set; }
        public ExchangeInfo ExchangeInfo { get; set; }
        public List<HistoricRate> HistoricRates { get; set; }
        public List<Fill> Fills { get; set; }
        public List<BinanceFill> BinanceFill { get; set; }
        public List<BinanceOrder> Orders { get; set; }
        public OrderBook OrderBook { get; set; }
        public Product SelectedProduct { get; set; }
        #endregion
        
        public Binance()
        {
            Tickers = new List<Ticker>();
            BinanceAccount = new BinanceAccount();
            HistoricRates = new List<HistoricRate>();
            Fills = new List<Fill>();
            Orders = new List<BinanceOrder>();
            OrderBook = new OrderBook();
            SelectedProduct = new Product();
            ServerTime = new ServerTime(0);
            _ioLock = new object();
        }

        #region Private Methods
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
                }

                ClientWebSocket = new ClientWebSocket();
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
            try
            {
                lock (_ioLock)
                {
                    if (string.IsNullOrEmpty(FileName))
                        return;
                    BinanceSettings binanceSettings = new BinanceSettings
                    {
                        Accounts = new List<Account>(),
                        BinanceAccount = BinanceAccount,
                        CurrentPrices = CurrentPrices,
                        ExchangeInfo = ExchangeInfo,
                        ServerTime = ServerTime,
                        Tickers = Tickers
                    };
                    string json = JsonSerializer.Serialize(binanceSettings,
                        new JsonSerializerOptions {WriteIndented = true});
                    File.WriteAllText(FileName, json);
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: Save\r\nException Stack Trace: {e.StackTrace}");
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
                BinanceSettings binanceSettings = JsonSerializer.Deserialize<BinanceSettings>(json);
                if (binanceSettings == null)
                    return;
                binanceSettings.Accounts = Accounts;
                binanceSettings.CurrentPrices = CurrentPrices;
                binanceSettings.Tickers = Tickers;
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: Load\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }
        }
        #endregion

        #region Public Methods

        #region Trading
        public async Task<ServerTime> UpdateTimeServerAsync()
        {
            Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                "/api/v1/time");
            string json = await ConnectionAdapter.RequestUnsignedAsync(request);
            ServerTime = JsonSerializer.Deserialize<ServerTime>(json);
            return ServerTime;
        }
        public async Task<ExchangeInfo> UpdateExchangeInfoAsync()
        {
            try
            {
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    "/api/v1/exchangeInfo");
                string json = await ConnectionAdapter.RequestUnsignedAsync(request);
                if (!string.IsNullOrEmpty(json))
                {
                    ExchangeInfo = JsonSerializer.Deserialize<ExchangeInfo>(json);
                    if (ExchangeInfo.Symbols == null || !ExchangeInfo.Symbols.Any())
                        return ExchangeInfo;
                    foreach (Symbol symbol in ExchangeInfo.Symbols)
                    {
                        if (symbol.BaseAsset != "BTC" && symbol.BaseAsset != "LTC" && symbol.BaseAsset != "ETH")
                            continue;
                        Filter filter = symbol.Filters.FirstOrDefault(x => x.FilterType == "PRICE_FILTER");
                        if (filter != null)
                        {
                            Products.Add(new Product
                            {
                                ID = symbol.ID,
                                BaseCurrency = symbol.BaseAsset,
                                QuoteCurrency = symbol.QuoteAsset,
                                BaseMaxSize = filter.MaxPrice,
                                BaseMinSize = filter.MinPrice,
                                QuoteIncrement = filter.TickSize
                            });
                        }
                    }
                    Save();
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateExchangeInfoAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return ExchangeInfo;
        }
        public async Task<BinanceAccount> UpdateAccountsAsync()
        {
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "[Binance] Updating Account Information.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET", "/api/v3/account?")
                {
                    RequestQuery = $"timestamp={serverTime.ServerTimeLong}"
                };
                string json = await ConnectionAdapter.RequestAsync(request);
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.JsonOutput,
                    $"UpdateAccountsAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                BinanceAccount = JsonSerializer.Deserialize<BinanceAccount>(json);
                if (BinanceAccount != null && BinanceAccount.Balances != null && BinanceAccount.Balances.Any())
                {
                    List<Asset> assets = BinanceAccount.Balances.Where(x => x.Free.ToDecimal() > 0 || x.ID == "BTC")
                        .ToList();
                    Accounts = new List<Account>();
                    foreach (Asset asset in assets)
                        Accounts.Add(new Account
                            {Hold = asset.Free, Balance = asset.Free, ID = asset.ID, Currency = asset.ID});
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
                        NotifyMainCurrency?.Invoke(ApplicationName, MainCurrency);
                        Save();
                    }

                    if (ExchangeInfo.Symbols == null || !ExchangeInfo.Symbols.Any())
                        return BinanceAccount;
                    foreach (Symbol symbol in ExchangeInfo.Symbols)
                    {
                        if (!BinanceAccount.Balances.Any(x => x.Free.ToDecimal() > 0 && x.ID == symbol.QuoteAsset
                                                              || x.ID == "BTC" || x.ID == "LTC" || x.ID == "ETH"))
                            continue;
                        {
                            Filter filter = symbol.Filters.FirstOrDefault(x => x.FilterType == "PRICE_FILTER");
                            if (filter != null)
                            {
                                Products.Add(new Product
                                {
                                    ID = symbol.ID,
                                    BaseCurrency = symbol.BaseAsset,
                                    QuoteCurrency = symbol.QuoteAsset,
                                    BaseMaxSize = filter.MaxPrice,
                                    BaseMinSize = filter.MinPrice,
                                    QuoteIncrement = filter.TickSize
                                });
                            }
                        }
                    }
                }
                return BinanceAccount;
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateAccountsAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return BinanceAccount;
        }
        public async Task<List<BinanceOrder>> UpdateOrdersAsync(Product product = null)
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General, "Updating Orders Information.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                if (product != null)
                {
                    Request request =
                        new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET", "/api/v3/openOrders?")
                        {
                            RequestQuery = $"symbol={product.ID}&recvWindow=5000&timestamp={serverTime.ServerTimeLong}"
                        };
                    json = await ConnectionAdapter.RequestAsync(request);
                    Orders = JsonSerializer.Deserialize<List<BinanceOrder>>(json);
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateOrdersAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return Orders;
        }
        public async Task<BinanceOrder> PostOrdersAsync(BinanceOrder order)
        {
            BinanceOrder binanceOrder = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General, "[Binance] Post Order Information.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "POST",
                    "/api/v3/order?");
                if (order.OrderType == OrderType.Market)
                    request.RequestQuery = $"timestamp={serverTime.ServerTimeLong}&symbol={order.Symbol.ToUpper()}" +
                                           $"&side={order.OrderSide.ToString().ToUpper()}" +
                                           $"&type={order.OrderType.ToString().ToUpper()}&quantity={order.OrderSize}";
                else
                    request.RequestQuery = $"timestamp={serverTime.ServerTimeLong}&symbol={order.Symbol.ToUpper()}" +
                                           $"&side={order.OrderSide.ToString().ToUpper()}&type={order.OrderType.ToString().ToUpper()}" +
                                           $"&quantity={order.OrderSize}&price={order.LimitPrice}&timeInForce=GTC";
                string json = await ConnectionAdapter.RequestAsync(request);
                binanceOrder = JsonSerializer.Deserialize<BinanceOrder>(json);
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.JsonOutput,
                    $"UpdateAccountsAsync JSON:\r\n{json}");
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: BinancePostOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return binanceOrder;
        }
        public async Task<BinanceOrder> CancelOrderAsync(BinanceOrder binanceOrder)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General, "Cancelling order.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl,
                    "DELETE",
                    "/api/v3/order?")
                {
                    RequestQuery =
                        $"symbol={binanceOrder.Symbol}&orderId={binanceOrder.ID}&timestamp={serverTime.ServerTimeLong}"
                };
                string json = await ConnectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    binanceOrder = JsonSerializer.Deserialize<BinanceOrder>(json);
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.JsonOutput,
                    $"BinanceCancelOrdersAsync JSON:\r\n{json}");
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: CancelOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return binanceOrder;
        }
        public async Task<List<BinanceOrder>> CancelOrdersAsync(Product product)
        {
            List<BinanceOrder> binanceOrders = new List<BinanceOrder>();
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General, "Cancelling order.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request =
                    new Request(ConnectionAdapter.Authentication.EndpointUrl, "DELETE", "/api/v3/openOrders?")
                    {
                        RequestQuery =
                            $"symbol={product.ID}&timestamp={serverTime.ServerTimeLong}"
                    };
                string json = await ConnectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    binanceOrders = JsonSerializer.Deserialize<BinanceOrder[]>(json).ToList();
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.JsonOutput,
                    $"CancelOrdersAsync JSON:\r\n{json}");
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: CancelOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return binanceOrders;
        }
        public async Task<List<Ticker>> UpdateTickersAsync(List<Product> products)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "Updating Update Tickers Information.");
                if (products == null || !products.Any())
                    return Tickers;
                Tickers ??= new List<Ticker>();
                //Get price of all products
                foreach (Product product in products)
                {
                    Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                        "/api/v3/ticker/price") {RequestQuery = $"?symbol={product.ID}"};
                    string json = await ConnectionAdapter.RequestUnsignedAsync(request);
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
                    if (!decimal.TryParse(ticker.Price, out decimal decimalPrice)) 
                        continue;
                    CurrentPrices ??= new Dictionary<string, decimal>();
                    if (!CurrentPrices.ContainsKey(ticker.ProductID))
                        CurrentPrices.Add(ticker.ProductID, decimalPrice);
                    CurrentPrices[ticker.ProductID] = decimalPrice;
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateTickersAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return Tickers;
        }
        public async Task<List<BinanceFill>> UpdateFillsAsync(Product product)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General, "Updating Fills Information.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl,
                    "GET",
                    "/api/v3/myTrades?")
                {
                    RequestQuery =
                        $"symbol={product.ID}&recvWindow=5000&timestamp={serverTime.ServerTimeLong}&limit=10"
                };
                string json = await ConnectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    BinanceFill = JsonSerializer.Deserialize<List<BinanceFill>>(json);
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.JsonOutput,
                    $"UpdateAccountsAsync JSON:\r\n{json}");
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateFillsAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return BinanceFill;
        }
        public async Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "[Binance] Updating Product Order Book.");
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET", "/api/v3/depth?")
                {
                    RequestQuery = $"symbol={product.ID}&limit={level}"
                };
                string json = await ConnectionAdapter.RequestUnsignedAsync(request);
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.JsonOutput,
                    $"UpdateProductOrderBookAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                OrderBook = JsonSerializer.Deserialize<OrderBook>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateAccountsAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return OrderBook;
        }
        #endregion

        #region Override

        #region Feed
        public override Task ChangeFeed(List<Product> products)
        {
            return Task.Run(() =>
            {
                ThreadPool.QueueUserWorkItem(async x =>
                {
                    SubscribedPrices = new Dictionary<string, decimal>();
                    if (products == null || !products.Any())
                        return;
                    string json = null;
                    try
                    {
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
                        if (SubscribeProducts != null && SubscribeProducts.Any())
                            await ConnectionAdapter.WebSocketCloseAsync();
                        //Prepare subscription
                        SubscribeProducts ??= new List<Product>();
                        string message = SubscribeProducts.Aggregate("stream?streams=", (currentMessage, product) => 
                            currentMessage + (product.ID.ToLower() + "@trade/"));
                        if (string.IsNullOrWhiteSpace(message) || !message.Contains("@trade"))
                            return;
                        int lastIndexOfSlash = message.LastIndexOf("/", StringComparison.Ordinal);
                        if (lastIndexOfSlash != -1)
                            message = message.Remove(lastIndexOfSlash, 1);
                        //Begin Processing
                        while (SubscribeProducts.Any())
                        {
                            string uriString = ConnectionAdapter.Authentication.WebSocketUri + message;
                            await ConnectionAdapter.ConnectAsync(uriString);
                            ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                                $"Started Processing Feed Information.\r\nURL-STRING: {uriString}");
                            while (ConnectionAdapter.IsWebSocketConnected())
                            {
                                json = await ConnectionAdapter.WebSocketReceiveAsync();
                                if (string.IsNullOrEmpty(json))
                                    continue;
                                Feed feed = JsonSerializer.Deserialize<Feed>(json);
                                if (feed == null || feed.Type == "error" || string.IsNullOrEmpty(feed.BinanceData.Symbol) || string.IsNullOrEmpty(feed.BinanceData.Price))
                                    return;
                                //update current price
                                AssetInformation ??= new Dictionary<string, AssetInformation>();
                                if (!AssetInformation.ContainsKey(feed.BinanceData.Symbol))
                                    AssetInformation.Add(feed.BinanceData.Symbol, new AssetInformation());
                                AssetInformation[feed.BinanceData.Symbol].CurrentPrice = feed.BinanceData.Price.ToDecimal();
                                CurrentPrices ??= new Dictionary<string, decimal>();
                                CurrentPrices[feed.BinanceData.Symbol] = feed.BinanceData.Price.ToDecimal();
                                SubscribedPrices[feed.BinanceData.Symbol] = feed.BinanceData.Price.ToDecimal();
                                //update product data
                                Products ??= new List<Product>();
                                Product product = Products.FirstOrDefault(currentProduct => currentProduct.ID == feed.BinanceData.Symbol);
                                if (product != null)
                                {
                                    Statistics twentyFourHourPrice = await TwentyFourHoursRollingStatsAsync(product);
                                    decimal priceChangeDifference = (twentyFourHourPrice.Last.ToDecimal() -
                                                                     twentyFourHourPrice.High.ToDecimal());
                                    decimal change = priceChangeDifference / Math.Abs(twentyFourHourPrice.High.ToDecimal());
                                    decimal percentage = change * 100;
                                    //update stat changes
                                    AssetInformation[feed.BinanceData.Symbol].TwentyFourHourPriceChange = priceChangeDifference;
                                    AssetInformation[feed.BinanceData.Symbol].TwentyFourHourPricePercentageChange = Math.Round(percentage,2);
                                    //Order Book
                                    OrderBook orderBook = await UpdateProductOrderBookAsync(product);
                                    List<Order> bidOrderList = orderBook.Bids.ToOrderList();
                                    List<Order> askOrderList = orderBook.Asks.ToOrderList();
                                    decimal bidMaxOrderSize = bidOrderList.Max(order => order.Size.ToDecimal());
                                    int indexOfMaxBidOrderSize = bidOrderList.FindIndex(a => a.Size.ToDecimal() == bidMaxOrderSize);
                                    bidOrderList = bidOrderList.Take(indexOfMaxBidOrderSize + 1).ToList();
                                    AssetInformation[feed.BinanceData.Symbol].BidMaxOrderSize = bidMaxOrderSize;
                                    AssetInformation[feed.BinanceData.Symbol].IndexOfMaxBidOrderSize = indexOfMaxBidOrderSize;
                                    decimal askMaxOrderSize = askOrderList.Max(order => order.Size.ToDecimal());
                                    int indexOfMaxAskOrderSize = askOrderList.FindIndex(a => a.Size.ToDecimal() == askMaxOrderSize);
                                    AssetInformation[feed.BinanceData.Symbol].AskMaxOrderSize = askMaxOrderSize;
                                    AssetInformation[feed.BinanceData.Symbol].IndexOfMaxAskOrderSize = indexOfMaxAskOrderSize;
                                    askOrderList = askOrderList.Take(indexOfMaxAskOrderSize + 1).ToList();
                                    //price and size
                                    AssetInformation[feed.BinanceData.Symbol].BidPriceAndSize = (from orderList in bidOrderList
                                                                                        select new PriceAndSize { Size = orderList.Size.ToDecimal(), Price = orderList.Price.ToDecimal() }).ToList();
                                    AssetInformation[feed.BinanceData.Symbol].AskPriceAndSize = (from orderList in askOrderList
                                                                                        select new PriceAndSize { Size = orderList.Size.ToDecimal(), Price = orderList.Price.ToDecimal() }).ToList();
                                }
                                //update order side
                                if (Enum.TryParse(feed.Side, out OrderSide orderSide))
                                    AssetInformation[feed.BinanceData.Symbol].OrderSide = orderSide;
                                feed.ProductID = feed.BinanceData.Symbol;
                                feed.Price = feed.BinanceData.Price;
                                //update best bid and ask
                                AssetInformation[feed.ProductID].BestAsk = feed.BestAsk;
                                AssetInformation[feed.ProductID].BestBid = feed.BestBid;
                                //Indicator
                                RelativeStrengthIndex relativeStrengthIndex = RelativeStrengthIndices.FirstOrDefault(currentProduct =>
                                    currentProduct.RelativeStrengthIndexSettings.Product.ID == feed.ProductID);
                                if (relativeStrengthIndex != null)
                                {
                                    AssetInformation[feed.ProductID].RelativeIndexQuarterly = relativeStrengthIndex
                                        .RelativeStrengthIndexSettings.RelativeIndexQuarterly;
                                    AssetInformation[feed.ProductID].RelativeIndexHourly = relativeStrengthIndex
                                        .RelativeStrengthIndexSettings.RelativeIndexHourly;
                                    AssetInformation[feed.ProductID].RelativeIndexDaily = relativeStrengthIndex
                                        .RelativeStrengthIndexSettings.RelativeIndexDaily;
                                }
                                NotifyAssetInformation?.Invoke(ApplicationName, AssetInformation);
                                //update feed and notifications
                                feed.CurrentPrices = CurrentPrices;
                                CurrentFeed = feed;
                                NotifyCurrentPrices?.Invoke(ApplicationName, SubscribedPrices);
                                //FeedBroadcast?.Invoke(ApplicationName, feed);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //TODO
                        //Notify
                        ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                            $"Method: StartProcessingFeed\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
                    }
                });
            });
        }
        #endregion

        public override async Task<bool> InitAsync(IExchangeSettings exchangeSettings)
        {
            try
            {
                ApplicationName = "Binance Exchange";
                AccountInfo = new Dictionary<string, decimal>();
                CurrentPrices = new Dictionary<string, decimal>();
                SubscribedPrices = new Dictionary<string, decimal>();
                ConnectionAdapter = new ConnectionAdapter();
                Products = new List<Product>();
                Statistics = new Dictionary<string, Statistics>();
                AssetInformation = new Dictionary<string, AssetInformation>();
                RelativeStrengthIndices = new List<RelativeStrengthIndex>();
                TestMode = exchangeSettings.TestMode;
                IndicatorSaveDataPath = exchangeSettings.IndicatorDirectoryPath;
                INIFilePath = exchangeSettings.INIDirectoryPath;
                string env = TestMode ? "test" : "live";
                if (string.IsNullOrEmpty(INIFilePath))
                {
                    string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        INIFilePath = Path.Combine(directoryName, "binance.config.ini");
                    }
                }
                else
                {
                    INIFilePath = Path.Combine(INIFilePath, "binance.config.ini");
                    if (!File.Exists(INIFilePath))
                    {
                        string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                        if (!string.IsNullOrEmpty(directoryName))
                        {
                            string tempPath = Path.Combine(directoryName, "binance.config.ini");
                            if (File.Exists(tempPath))
                            {
                                FileInfo file = new FileInfo(INIFilePath);
                                file.Directory?.Create();
                                File.Copy(tempPath, INIFilePath, true);
                            }
                        }
                    }
                }
                LoadINI(INIFilePath); 
                if (!string.IsNullOrEmpty(ConnectionAdapter.Authentication.EndpointUrl))
                {
                    FileInfo file = new FileInfo(INIFilePath);
                    if (file.Directory != null)
                    {
                        string connectionContainsEnvironment =
                            ConnectionAdapter.Authentication.EndpointUrl.ToLower().Contains("test") ||
                            ConnectionAdapter.Authentication.EndpointUrl.ToLower().Contains("sandbox")
                                ? "t_endpoint" : "l_endpoint";
                        FileName = Path.Combine(file.Directory.FullName, $"binance_{env}_{connectionContainsEnvironment}.json");
                    }
                }
                Load();
                await UpdateExchangeInfoAsync();
                await UpdateAccountsAsync();
                //List<Product> products = new List<Product>
                //{
                //    Products.FirstOrDefault(x => x.BaseCurrency == "BNB" && x.QuoteCurrency == "BUSD"),
                //    Products.FirstOrDefault(x => x.BaseCurrency == "ETH" && x.QuoteCurrency == "BTC")
                //};
                //products.RemoveAll(x => x == null);
                //if (products.Any())
                //{
                //    await UpdateProductOrderBookAsync(products[0], 20);
                //    HistoricCandlesSearch historicCandlesSearch = new HistoricCandlesSearch();
                //    historicCandlesSearch.Symbol = products[0].ID;
                //    historicCandlesSearch.StartingDateTime = DateTime.Now.AddHours(-2).ToUniversalTime();
                //    historicCandlesSearch.EndingDateTime = DateTime.Now.ToUniversalTime();
                //    historicCandlesSearch.Granularity = (Granularity) 900;
                //    await UpdateProductHistoricCandlesAsync(historicCandlesSearch);
                //    await UpdateTickersAsync(products);

                //BinanceOrder binanceOrderMarket = new BinanceOrder();
                //binanceOrderMarket.OrderType = OrderType.Market;
                //binanceOrderMarket.OrderSide = OrderSide.Buy;
                //binanceOrderMarket.OrderSize = (decimal) 0.1;
                //binanceOrderMarket.Symbol = "BNBBTC";

                //BinanceOrder binanceOrderLimit = new BinanceOrder();
                //binanceOrderLimit.OrderType = OrderType.Limit;
                //binanceOrderLimit.OrderSide = OrderSide.Buy;
                //binanceOrderLimit.OrderSize = (decimal) 0.1;
                //binanceOrderLimit.LimitPrice = (decimal) 0.0010000;
                //binanceOrderLimit.Symbol = "BNBBTC";

                //Product productToCancel = new Product {ID = "BNBBTC"};

                //BinanceOrder binanceOrderMarketPostedResults = await PostOrdersAsync(binanceOrderMarket);
                //BinanceOrder binanceOrderLimitPostedResults = await PostOrdersAsync(binanceOrderLimit);
                //BinanceOrder binanceOrderToCancel = await CancelOrderAsync(binanceOrderLimitPostedResults);

                //List<BinanceOrder> currentOrders = await UpdateOrdersAsync(new Product {ID = "BNBBTC"});
                //if (currentOrders.Any())
                //{
                //    List<BinanceOrder> binanceOrderCancelProduct = await CancelOrdersAsync(productToCancel);
                //}
                // }

                return true;
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: InitAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return false;
        }
        public override bool InitIndicatorsAsync(List<Product> products)
        {
            string binanceRSIFile = null;
            string env = TestMode ? "test" : "live";
            if (string.IsNullOrEmpty(ConnectionAdapter.Authentication.EndpointUrl))
                return false;
            string connectionContainsEnvironment = ConnectionAdapter.Authentication.EndpointUrl.ToLower().Contains("test") ||
                                                   ConnectionAdapter.Authentication.EndpointUrl.ToLower().Contains("sandbox")
                ? "t_endpoint"
                : "l_endpoint";
            if (string.IsNullOrEmpty(IndicatorSaveDataPath))
            {
                string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                if (!string.IsNullOrEmpty(directoryName))
                    binanceRSIFile = Path.Combine(directoryName, $"data\\binance_{env}_{connectionContainsEnvironment}");
            }
            else
            {
                binanceRSIFile = Path.Combine(IndicatorSaveDataPath, $"binance_{env}_{connectionContainsEnvironment}");
            }
            if (string.IsNullOrEmpty(binanceRSIFile))
                return false;
            if (products == null)
                return false;
            foreach (Product product in products)
            {
                RelativeStrengthIndex relativeStrengthIndex = new RelativeStrengthIndex(binanceRSIFile, product);
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
        public override async Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(
            HistoricCandlesSearch historicCandlesSearch)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "[Binance] Updating Product Historic Candles.");
                string interval = historicCandlesSearch.Granularity switch
                {
                    Granularity.FiveMinutes => "5m",
                    Granularity.Fifteen => "15m",
                    Granularity.OneHour => "1h",
                    Granularity.OneDay => "1d",
                    _ => "1d",
                };
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET", "/api/v1/klines?")
                {
                    RequestQuery =
                        $"symbol={historicCandlesSearch.Symbol}&" +
                        $"interval={interval}&startTime={historicCandlesSearch.StartingDateTime.GenerateDateTimeOffsetToUnixTimeMilliseconds()}&" +
                        $"endTime={historicCandlesSearch.EndingDateTime.GenerateDateTimeOffsetToUnixTimeMilliseconds()}"
                };
                string json = await ConnectionAdapter.RequestUnsignedAsync(request);
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.JsonOutput,
                    $"UpdateProductOrderBookAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                if (json.StartsWith("[") && json.EndsWith("]"))
                {
                    ArrayList[] arrayListOfHistory = JsonSerializer.Deserialize<ArrayList[]>(json);
                    HistoricRates = arrayListOfHistory.ToHistoricCandleList();
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateProductHistoricCandlesAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return HistoricRates;
        }
        public override async Task<Statistics> TwentyFourHoursRollingStatsAsync(Product product)
        {
            string json = null;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                    "Updating 24 hour stats Information.");
                Request request =
                    new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET", "/api/v3/ticker/24hr?")
                    {
                        RequestQuery = $"symbol={product.ID}"
                    };
                json = await ConnectionAdapter.RequestUnsignedAsync(request);
                if (!string.IsNullOrEmpty(json))
                {
                    Statistics statistics = JsonSerializer.Deserialize<Statistics>(json);
                    Statistics ??= new Dictionary<string, Statistics>();
                    if (!Statistics.ContainsKey(product.ID))
                        Statistics.Add(product.ID, statistics);
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

        #endregion

        #endregion
    }
}