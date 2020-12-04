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
using exchange.binance.helpers;
using exchange.binance.models;
using exchange.core.enums;
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
        public ServerTime ServerTime { get; set; }
        public BinanceAccount BinanceAccount { get; set; }
        public ExchangeInfo ExchangeInfo { get; set; }
        public List<BinanceFill> BinanceFill { get; set; }
        public List<BinanceOrder> BinanceOrders { get; set; }
        #endregion
        
        public Binance()
        {
            BinanceAccount = new BinanceAccount();
            BinanceOrders = new List<BinanceOrder>();
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
                if (BinanceAccount?.Balances == null || !BinanceAccount.Balances.Any()) 
                    return BinanceAccount;
                List<Asset> assets = BinanceAccount.Balances.Where(x => x.Free.ToDecimal() > 0 || x.ID == "BTC")
                    .ToList();
                Accounts ??= new List<Account>();
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
                return BinanceAccount;
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateAccountsAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return BinanceAccount;
        }
        public async Task<List<BinanceOrder>> UpdateBinanceOrdersAsync(Product product = null)
        {
            string json = null;
            try
            {
                ServerTime serverTime = await UpdateTimeServerAsync();
                if (product != null)
                {
                    Request request =
                        new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET", "/api/v3/openOrders?")
                        {
                            RequestQuery = $"symbol={product.ID}&recvWindow=5000&timestamp={serverTime.ServerTimeLong}"
                        };
                    json = await ConnectionAdapter.RequestAsync(request);
                    BinanceOrders = JsonSerializer.Deserialize<List<BinanceOrder>>(json);
                    Orders = BinanceOrders.Select(binanceOrder => binanceOrder.ToOrder()).ToList();
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateOrdersAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return BinanceOrders;
        }
        public async Task<BinanceOrder> PostOrdersAsync(BinanceOrder order)
        {
            BinanceOrder binanceOrder = null;
            try
            {
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "POST",
                    "/api/v3/order?");
                if (order.OrderType == OrderType.Market)
                    request.RequestQuery = $"timestamp={serverTime.ServerTimeLong}&symbol={order.Symbol.ToUpper()}" +
                                           $"&side={order.OrderSide.ToString().ToUpper()}" +
                                           $"&type={order.OrderType.ToString().ToUpper()}&quantity={order.OrigQty}";
                else
                    request.RequestQuery = $"timestamp={serverTime.ServerTimeLong}&symbol={order.Symbol.ToUpper()}" +
                                           $"&side={order.OrderSide.ToString().ToUpper()}&type={order.OrderType.ToString().ToUpper()}" +
                                           $"&quantity={order.OrigQty}&price={order.Price}&timeInForce=GTC";
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
        public async Task<List<BinanceOrder>> CancelBinanceOrdersAsync(Product product)
        {
            List<BinanceOrder> binanceOrders = new List<BinanceOrder>();
            try
            {
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
        public async Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 20)
        {
            try
            {
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET", "/api/v3/depth?")
                {
                    RequestQuery = $"symbol={product.ID}&limit={level}"
                };
                string json = await ConnectionAdapter.RequestUnsignedAsync(request);
                if (string.IsNullOrEmpty(json))
                    return OrderBook;
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
                    await UpdateAccountsAsync();
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
                                BinanceFeed binanceFeed = JsonSerializer.Deserialize<BinanceFeed>(json);
                                if (binanceFeed == null || binanceFeed.Type == "error" || string.IsNullOrEmpty(binanceFeed.BinanceData.Symbol) || string.IsNullOrEmpty(binanceFeed.BinanceData.Price))
                                    return;
                                //update current price
                                AssetInformation ??= new Dictionary<string, AssetInformation>();
                                if (!AssetInformation.ContainsKey(binanceFeed.BinanceData.Symbol))
                                    AssetInformation.Add(binanceFeed.BinanceData.Symbol, new AssetInformation{ProductID = binanceFeed.BinanceData.Symbol});
                                AssetInformation currentAssetInformation = AssetInformation[binanceFeed.BinanceData.Symbol];
                                //Get current product
                                Products ??= new List<Product>();
                                Product selectedProduct = Products.FirstOrDefault(p => p.ID == currentAssetInformation.ProductID);
                                if (selectedProduct == null)
                                    continue;
                                CurrentFeed ??= new Feed();
                                CurrentFeed.ProductID = binanceFeed.BinanceData.Symbol;
                                CurrentFeed.Price = binanceFeed.BinanceData.Price;
                                //update best bid and ask
                                CurrentFeed.BestAsk = binanceFeed.BestAsk;
                                CurrentFeed.BestBid = binanceFeed.BestBid;
                                currentAssetInformation.ProductID = binanceFeed.BinanceData.Symbol;
                                currentAssetInformation.CurrentPrice = binanceFeed.BinanceData.Price.ToDecimal();
                                //Current price update
                                decimal currentPrice = binanceFeed.BinanceData.Price.ToDecimal();
                                CurrentPrices ??= new Dictionary<string, decimal>();
                                if (!CurrentPrices.ContainsKey(currentAssetInformation.ProductID))
                                    CurrentPrices.Add(currentAssetInformation.ProductID, currentPrice);
                                CurrentPrices[currentAssetInformation.ProductID] = currentPrice;
                                SubscribedPrices[currentAssetInformation.ProductID] = currentPrice;
                                //update feed and notifications
                                CurrentFeed.CurrentPrices = CurrentPrices;
                                //update order side
                                if (Enum.TryParse(binanceFeed.Side, out OrderSide orderSide))
                                    currentAssetInformation.OrderSide = orderSide;
                                //update best bid and ask
                                currentAssetInformation.BestAsk = binanceFeed.BestAsk;
                                currentAssetInformation.BestBid = binanceFeed.BestBid;
                                //Indicator
                                RelativeStrengthIndex relativeStrengthIndex = RelativeStrengthIndices.FirstOrDefault(currentProduct =>
                                    currentProduct.RelativeStrengthIndexSettings.Product.ID == binanceFeed.BinanceData.Symbol);
                                if (relativeStrengthIndex != null)
                                {
                                    currentAssetInformation.RelativeIndexQuarterly = relativeStrengthIndex
                                        .RelativeStrengthIndexSettings.RelativeIndexQuarterly;
                                    currentAssetInformation.RelativeIndexHourly = relativeStrengthIndex
                                        .RelativeStrengthIndexSettings.RelativeIndexHourly;
                                    currentAssetInformation.RelativeIndexDaily = relativeStrengthIndex
                                        .RelativeStrengthIndexSettings.RelativeIndexDaily;
                                }
                                //Account Balances
                                //Quote Currency Balance: example EUR / BTC
                                Account selectedQuoteCurrencyAccount = Accounts.FirstOrDefault(account => account.Currency == selectedProduct.QuoteCurrency);
                                currentAssetInformation.QuoteCurrencySymbol = selectedQuoteCurrencyAccount.Currency;
                                decimal.TryParse(selectedQuoteCurrencyAccount.Balance, out decimal outQuoteCurrencyBalance);
                                currentAssetInformation.QuoteCurrencyBalance = outQuoteCurrencyBalance;
                                decimal.TryParse(selectedQuoteCurrencyAccount.Hold, out decimal outQuoteCurrencyHold);
                                currentAssetInformation.QuoteCurrencyHold = outQuoteCurrencyHold;
                                decimal.TryParse(selectedQuoteCurrencyAccount.Available, out decimal outQuoteCurrencyAvailable);
                                currentAssetInformation.QuoteCurrencyAvailable = outQuoteCurrencyAvailable;
                                //Base Currency Balance: example BTC / ETH
                                Account selectedBaseCurrencyAccount = Accounts.FirstOrDefault(account => account.Currency == selectedProduct.BaseCurrency);
                                currentAssetInformation.BaseCurrencySymbol = selectedBaseCurrencyAccount.Currency;
                                decimal.TryParse(selectedBaseCurrencyAccount.Balance, out decimal outSelectedAssetBalance);
                                currentAssetInformation.BaseCurrencyBalance = outSelectedAssetBalance;
                                decimal.TryParse(selectedBaseCurrencyAccount.Hold, out decimal outSelectedAssetHold);
                                currentAssetInformation.BaseCurrencyHold = outSelectedAssetHold;
                                decimal.TryParse(selectedBaseCurrencyAccount.Available, out decimal outSelectedAssetAvailable);
                                currentAssetInformation.BaseCurrencyAvailable = outSelectedAssetAvailable;
                                //Base and Quote Price: example BTC-EUR / ETH-BTC
                                Product quoteAndBaseProduct = Products.FirstOrDefault(p => p.ID ==
                                    $"{currentAssetInformation.BaseCurrencySymbol}-{currentAssetInformation.QuoteCurrencySymbol}");
                                if (CurrentPrices.ContainsKey(quoteAndBaseProduct.ID))
                                    currentAssetInformation.BaseAndQuotePrice = CurrentPrices[quoteAndBaseProduct.ID];
                                //Selected Main Currency: example EUR
                                Account selectedMainCurrencyAccount = Accounts.FirstOrDefault(account => account.Currency == MainCurrency);
                                currentAssetInformation.SelectedMainCurrencySymbol = selectedMainCurrencyAccount.Currency;
                                decimal.TryParse(selectedMainCurrencyAccount.Balance, out decimal outSelectedMainCurrencyBalance);
                                currentAssetInformation.SelectedMainCurrencyBalance = outSelectedMainCurrencyBalance;
                                decimal.TryParse(selectedMainCurrencyAccount.Hold, out decimal outSelectedMainCurrencyHold);
                                currentAssetInformation.SelectedMainCurrencyHold = outSelectedMainCurrencyHold;
                                decimal.TryParse(selectedMainCurrencyAccount.Available, out decimal outSelectedMainCurrencyAvailable);
                                currentAssetInformation.SelectedMainCurrencyAvailable = outSelectedMainCurrencyAvailable;
                                //Base and Selected Main Price: example BTC-EUR / ETH-BTC
                                Product quoteAndSelectedMainProduct = Products.FirstOrDefault(p => p.ID ==
                                    $"{currentAssetInformation.BaseCurrencySymbol}-{currentAssetInformation.SelectedMainCurrencySymbol}");
                                if (CurrentPrices.ContainsKey(quoteAndSelectedMainProduct.ID))
                                    currentAssetInformation.BaseAndSelectedMainPrice = CurrentPrices[quoteAndSelectedMainProduct.ID];
                                //update product data
                                Statistics twentyFourHourPrice = await TwentyFourHoursRollingStatsAsync(selectedProduct);
                                if (twentyFourHourPrice?.Last != null && twentyFourHourPrice?.High != null)
                                {
                                    decimal priceChangeDifference = (twentyFourHourPrice.Last.ToDecimal() -
                                                                     twentyFourHourPrice.High.ToDecimal());
                                    decimal change = priceChangeDifference / Math.Abs(twentyFourHourPrice.High.ToDecimal());
                                    decimal percentage = change * 100;
                                    //update stat changes
                                    currentAssetInformation.TwentyFourHourPriceChange = priceChangeDifference;
                                    currentAssetInformation.TwentyFourHourPricePercentageChange = Math.Round(percentage, 2);
                                }
                                //Order Book
                                OrderBook orderBook = await UpdateProductOrderBookAsync(selectedProduct);
                                if (orderBook?.Bids != null && orderBook?.Asks != null)
                                {
                                    List<Order> bidOrderList = orderBook.Bids.ToOrderList();
                                    List<Order> askOrderList = orderBook.Asks.ToOrderList();
                                    decimal bidMaxOrderSize = bidOrderList.Max(order => order.Size.ToDecimal());
                                    int indexOfMaxBidOrderSize = bidOrderList.FindIndex(a => a.Size.ToDecimal() == bidMaxOrderSize);
                                    bidOrderList = bidOrderList.Take(indexOfMaxBidOrderSize + 1).ToList();
                                    currentAssetInformation.BidMaxOrderSize = bidMaxOrderSize;
                                    currentAssetInformation.IndexOfMaxBidOrderSize = indexOfMaxBidOrderSize;
                                    decimal askMaxOrderSize = askOrderList.Max(order => order.Size.ToDecimal());
                                    int indexOfMaxAskOrderSize = askOrderList.FindIndex(a => a.Size.ToDecimal() == askMaxOrderSize);
                                    currentAssetInformation.AskMaxOrderSize = askMaxOrderSize;
                                    currentAssetInformation.IndexOfMaxAskOrderSize = indexOfMaxAskOrderSize;
                                    askOrderList = askOrderList.Take(indexOfMaxAskOrderSize + 1).ToList();
                                    //price and size
                                    currentAssetInformation.BidPriceAndSize = (from orderList in bidOrderList
                                                                               select new PriceAndSize { Size = orderList.Size.ToDecimal(), Price = orderList.Price.ToDecimal() }).ToList();
                                    currentAssetInformation.AskPriceAndSize = (from orderList in askOrderList
                                                                               select new PriceAndSize { Size = orderList.Size.ToDecimal(), Price = orderList.Price.ToDecimal() }).ToList();
                                }
                                NotifyAssetInformation?.Invoke(ApplicationName, AssetInformation);
                                NotifyCurrentPrices?.Invoke(ApplicationName, SubscribedPrices);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //TODO
                        //Notify
                        ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                            $"Method: StartProcessingFeed\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
                        SubscribeProducts = new List<Product>();
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
                Orders = new List<Order>();
                Accounts = new List<Account>();
                Tickers = new List<Ticker>();
                HistoricRates = new List<HistoricRate>();
                OrderBook = new OrderBook();
                SelectedProduct = new Product();
                ConnectionAdapter = new ConnectionAdapter
                {
                    ProcessLogBroadcast = (messageType, message) => { ProcessLogBroadcast.Invoke(ApplicationName, messageType, message); }
                };
                Products = new List<Product>();
                Statistics = new Dictionary<string, Statistics>();
                AssetInformation = new Dictionary<string, AssetInformation>();
                RelativeStrengthIndices = new List<RelativeStrengthIndex>();
                Fills = new List<Fill>();
                TestMode = exchangeSettings.TestMode;
                IndicatorSaveDataPath = exchangeSettings.IndicatorDirectoryPath;
                INIFilePath = exchangeSettings.INIDirectoryPath;
                MainCurrency = exchangeSettings.MainCurrency;
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
            try
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
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: InitIndicatorsAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return false;
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
                if (!string.IsNullOrEmpty(json) && json.StartsWith("[") && json.EndsWith("]"))
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
            Statistics statistics = new Statistics();
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
                    BinanceStatistics binanceStatistics = JsonSerializer.Deserialize<BinanceStatistics>(json);
                    statistics.Volume = binanceStatistics.Volume;
                    statistics.Open = binanceStatistics.OpenPrice;
                    statistics.High = binanceStatistics.HighPrice;
                    statistics.Low = binanceStatistics.LowPrice;
                    statistics.Last = binanceStatistics.LastPrice;
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
            return statistics;
        }
        public override async Task<List<Order>> CancelAllOrdersAsync(Product product)
        {
            List<Order> orders = new List<Order>();
            try
            {
                List<BinanceOrder> binanceOrders = await CancelBinanceOrdersAsync(product);
                orders.AddRange(binanceOrders.Select(binanceOrder => binanceOrder.ToOrder()).ToList());
                return orders;
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: CancelAllOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return orders;
        }
        public override async Task<List<Order>> CancelOrderAsync(Order order)
        {
            List<Order> orders = new List<Order>();
            try
            {
                BinanceOrder binanceOrder = new BinanceOrder { ID = int.Parse(order.ID) };
                Product product = new Product {ID = order.ProductID};
                BinanceOrder outputBinanceOrder = await CancelOrderAsync(binanceOrder);
                List<Order> outputOrders = await UpdateOrdersAsync(product);
                return outputOrders;
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: CancelOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return orders;
        }
        public override async Task<Order> PostOrdersAsync(Order order)
        {
            Order resultOrder = new Order();
            try
            {
                BinanceOrder currentBinanceOrder = new BinanceOrder
                {
                    OrderType = Enum.Parse<OrderType>(order.Type),
                    OrderSide = Enum.Parse<OrderSide>(order.Side),
                    OrigQty = order.Size,
                    Price = order.Price,
                    Symbol = order.ProductID
                };
                BinanceOrder postedOrder = await PostOrdersAsync(currentBinanceOrder);
                Product product = new Product {ID = currentBinanceOrder.Symbol};
                List<BinanceOrder> binanceOrders =  await UpdateBinanceOrdersAsync(product);
                Orders = binanceOrders.Select(binanceOrder => binanceOrder.ToOrder()).ToList();
                await UpdateAccountsAsync();
                return postedOrder.ToOrder();
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: PostOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return resultOrder;
        }
        public override async Task<List<Fill>> UpdateFillsAsync(Product product)
        {
            try
            {
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

                Fills ??= new List<Fill>();
                if (BinanceFill == null)
                    return Fills;
                DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                Fills = BinanceFill.Select(binanceFill => new Fill
                {
                    Size = binanceFill.Quantity,
                    Side = binanceFill.IsBuyer ? OrderSide.Buy.ToString() : OrderSide.Sell.ToString(),
                    OrderID = binanceFill.TradeID.ToString(),
                    Price = binanceFill.Price,
                    Fee = binanceFill.Commission,
                    ProductID = binanceFill.ID,
                    Settled = true,
                    Time = start.AddMilliseconds(binanceFill.Time).ToLocalTime(),
                    TradeID = binanceFill.TradeID
                }).ToList();
                NotifyFills?.Invoke(ApplicationName, Fills);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateFillsAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return Fills;
        }
        public override async Task<List<Order>> UpdateOrdersAsync(Product product = null)
        {
            List<Order> orders = new List<Order>();
            try
            {
                List<BinanceOrder> binanceOrders = await UpdateBinanceOrdersAsync(product);
                return binanceOrders.Select(binanceOrder => binanceOrder.ToOrder()).ToList();
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return orders;
        }
        #endregion

        #endregion
    }
}