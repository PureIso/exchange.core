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
using exchange.core.enums;
using exchange.core.helpers;
using exchange.core.implementations;
using exchange.core.indicators;
using exchange.core.interfaces;
using exchange.core.models;

namespace exchange.coinbase
{
    public class Coinbase : AbstractExchangePlugin, IDisposable
    {
        #region Fields
        private readonly object _ioLock;
        #endregion

        #region Public Properties
        
        public List<AccountHistory> AccountHistories { get; set; }
        public List<AccountHold> AccountHolds { get; set; }
        #endregion
         
        public Coinbase()
        {
            AccountHistories = new List<AccountHistory>();
            AccountHolds = new List<AccountHold>();
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
                        return;
                    CoinbaseSettings coinbaseSettings = new CoinbaseSettings
                    {
                        Accounts = Accounts, CurrentPrices = CurrentPrices, Tickers = Tickers
                    };
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
        #endregion

        #region Public Methods

        #region Trading
        public async Task<List<Account>> UpdateAccountsAsync(string accountId = "")
        {
            string json = null;
            try
            {
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/accounts/{accountId}");
                json = await ConnectionAdapter.RequestAsync(request);
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
                    NotifyMainCurrency?.Invoke(ApplicationName, MainCurrency);
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
        public async Task<List<AccountHistory>> UpdateAccountHistoryAsync(string accountId)
        {
            string json = null;
            try
            {
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/accounts/{accountId}/ledger");
                json = await ConnectionAdapter.RequestAsync(request);
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
        public async Task<List<Product>> UpdateProductsAsync()
        {
            string json = null;
            try
            {
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
                if (products == null || !products.Any())
                    return Tickers;
                Tickers ??= new List<Ticker>();
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
                    {
                        CurrentPrices ??= new Dictionary<string, decimal>();
                        if (!CurrentPrices.ContainsKey(ticker.ProductID))
                            CurrentPrices.Add(ticker.ProductID, decimalPrice);
                        CurrentPrices[ticker.ProductID] = decimalPrice;
                    }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateTickersAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return Tickers;
        }
        public async Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2)
        {
            string json = null;
            try
            {
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/products/{product.ID}/book?level={level}");
                json = await ConnectionAdapter.RequestAsync(request);
                if (string.IsNullOrEmpty(json))
                    return OrderBook;
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
                    if (products == null)
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
                        foreach (Product product in products)
                        {
                            await UpdateFillStatistics(product);
                        }
                        SubscribeProducts = products;
                        InitIndicatorsAsync(products);
                        if (SubscribeProducts != null && SubscribeProducts.Any())
                            await ConnectionAdapter.WebSocketCloseAsync();
                        //Prepare subscription
                        SubscribeProducts ??= new List<Product>();
                        DelegateNotifySubscriptionStatus?.Invoke(ApplicationName, true);
                        //Begin Processing
                        while (SubscribeProducts.Any())
                        {
                            await ConnectionAdapter.ConnectAsync(ConnectionAdapter.Authentication.WebSocketUri.ToString());
                            json = await ConnectionAdapter.WebSocketSendAsync(SubscribeProducts.ToSubscribeString());
                            if (string.IsNullOrEmpty(json))
                            {
                                DelegateNotifySubscriptionStatus?.Invoke(ApplicationName, false);
                                return;
                            }
                            ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                                $"Started Processing Feed Information.\r\nJSON: {json}");
                            try
                            {
                                while (ConnectionAdapter.IsWebSocketConnected())
                                {
                                    json = await ConnectionAdapter.WebSocketReceiveAsync();
                                    if (string.IsNullOrEmpty(json))
                                        continue;
                                    Feed feed = JsonSerializer.Deserialize<Feed>(json);
                                    if (feed == null || feed.Type == "error" || string.IsNullOrEmpty(feed.ProductID) || string.IsNullOrEmpty(feed.Price))
                                        continue;
                                    //update current price
                                    AssetInformation ??= new Dictionary<string, AssetInformation>();
                                    if (!AssetInformation.ContainsKey(feed.ProductID))
                                        AssetInformation.Add(feed.ProductID, new AssetInformation{ProductID = feed.ProductID });
                                    AssetInformation currentAssetInformation = AssetInformation[feed.ProductID];
                                    //Get current product
                                    Products ??= new List<Product>();
                                    Accounts ??= new List<Account>();
                                    Product selectedProduct = Products.FirstOrDefault(p => p.ID == currentAssetInformation.ProductID);
                                    if(selectedProduct == null)
                                        continue;
                                    currentAssetInformation.ProductID = feed.ProductID;
                                    currentAssetInformation.CurrentPrice = feed.Price.ToDecimal();
                                    //Current price update
                                    decimal currentPrice = feed.Price.ToDecimal();
                                    CurrentPrices ??= new Dictionary<string, decimal>();
                                    if (!CurrentPrices.ContainsKey(currentAssetInformation.ProductID))
                                        CurrentPrices.Add(currentAssetInformation.ProductID, currentPrice);
                                    CurrentPrices[currentAssetInformation.ProductID] = currentPrice;
                                    SubscribedPrices[currentAssetInformation.ProductID] = currentPrice;
                                    //update feed and notifications
                                    feed.CurrentPrices = CurrentPrices;
                                    CurrentFeed = feed;
                                    //update order side
                                    if (Enum.TryParse(feed.Side, out OrderSide orderSide))
                                        currentAssetInformation.OrderSide = orderSide;
                                    //update best bid and ask
                                    currentAssetInformation.BestAsk = feed.BestAsk;
                                    currentAssetInformation.BestBid = feed.BestBid;
                                    //Indicator
                                    RelativeStrengthIndex relativeStrengthIndex = RelativeStrengthIndices.FirstOrDefault(currentProduct =>
                                        currentProduct.RelativeStrengthIndexSettings.Product.ID == feed.ProductID);
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
                                    if (selectedQuoteCurrencyAccount != null)
                                    {
                                        currentAssetInformation.QuoteCurrencySymbol = selectedQuoteCurrencyAccount.Currency;
                                        decimal.TryParse(selectedQuoteCurrencyAccount.Balance, out decimal outQuoteCurrencyBalance);
                                        currentAssetInformation.QuoteCurrencyBalance = outQuoteCurrencyBalance;
                                        decimal.TryParse(selectedQuoteCurrencyAccount.Hold, out decimal outQuoteCurrencyHold);
                                        currentAssetInformation.QuoteCurrencyHold = outQuoteCurrencyHold;
                                        decimal.TryParse(selectedQuoteCurrencyAccount.Available, out decimal outQuoteCurrencyAvailable);
                                        currentAssetInformation.QuoteCurrencyAvailable = outQuoteCurrencyAvailable;
                                    }
                                    //Base Currency Balance: example BTC / ETH
                                    Account selectedBaseCurrencyAccount = Accounts.FirstOrDefault(account => account.Currency == selectedProduct.BaseCurrency);
                                    if (selectedBaseCurrencyAccount != null)
                                    {
                                        currentAssetInformation.BaseCurrencySymbol = selectedBaseCurrencyAccount.Currency;
                                        decimal.TryParse(selectedBaseCurrencyAccount.Balance, out decimal outSelectedAssetBalance);
                                        currentAssetInformation.BaseCurrencyBalance = outSelectedAssetBalance;
                                        decimal.TryParse(selectedBaseCurrencyAccount.Hold, out decimal outSelectedAssetHold);
                                        currentAssetInformation.BaseCurrencyHold = outSelectedAssetHold;
                                        decimal.TryParse(selectedBaseCurrencyAccount.Available, out decimal outSelectedAssetAvailable);
                                        currentAssetInformation.BaseCurrencyAvailable = outSelectedAssetAvailable;
                                    }
                                    //Base and Quote Price: example BTC-EUR / ETH-BTC
                                    Product quoteAndBaseProduct = Products.FirstOrDefault(p => p.ID == 
                                        $"{currentAssetInformation.BaseCurrencySymbol}-{currentAssetInformation.QuoteCurrencySymbol}");
                                    if (quoteAndBaseProduct != null && CurrentPrices.ContainsKey(quoteAndBaseProduct.ID))
                                    {
                                        currentAssetInformation.BaseAndQuotePrice = CurrentPrices[quoteAndBaseProduct.ID];
                                        currentAssetInformation.BaseAndQuoteBalance = CurrentPrices[quoteAndBaseProduct.ID] * currentAssetInformation.BaseCurrencyBalance;
                                    }
                                    //Selected Main Currency: example EUR
                                    Account selectedMainCurrencyAccount = Accounts.FirstOrDefault(account => account.Currency == MainCurrency);
                                    if (selectedMainCurrencyAccount != null)
                                    {
                                        currentAssetInformation.SelectedMainCurrencySymbol = selectedMainCurrencyAccount.Currency;
                                        decimal.TryParse(selectedMainCurrencyAccount.Balance, out decimal outSelectedMainCurrencyBalance);
                                        currentAssetInformation.SelectedMainCurrencyBalance = outSelectedMainCurrencyBalance;
                                        decimal.TryParse(selectedMainCurrencyAccount.Hold, out decimal outSelectedMainCurrencyHold);
                                        currentAssetInformation.SelectedMainCurrencyHold = outSelectedMainCurrencyHold;
                                        decimal.TryParse(selectedMainCurrencyAccount.Available, out decimal outSelectedMainCurrencyAvailable);
                                        currentAssetInformation.SelectedMainCurrencyAvailable = outSelectedMainCurrencyAvailable;
                                    }
                                    //Base and Selected Main Price: example BTC-EUR / ETH-BTC
                                    Product quoteAndSelectedMainProduct = Products.FirstOrDefault(p => p.ID ==
                                        $"{currentAssetInformation.BaseCurrencySymbol}-{currentAssetInformation.SelectedMainCurrencySymbol}");
                                    if (quoteAndSelectedMainProduct != null &&
                                        CurrentPrices.ContainsKey(quoteAndSelectedMainProduct.ID))
                                    {
                                        currentAssetInformation.BaseAndSelectedMainPrice = CurrentPrices[quoteAndSelectedMainProduct.ID];
                                        currentAssetInformation.BaseAndSelectedMainBalance = CurrentPrices[quoteAndSelectedMainProduct.ID] * currentAssetInformation.BaseCurrencyBalance;
                                    }
                                    //Total Balance with Selected Main Currency
                                    if (quoteAndBaseProduct != null && CurrentPrices.ContainsKey(quoteAndBaseProduct.ID) &&
                                        quoteAndSelectedMainProduct != null &&
                                        CurrentPrices.ContainsKey(quoteAndSelectedMainProduct.ID))
                                    {
                                        currentAssetInformation.AggregatedSelectedMainBalance =
                                            Math.Round(currentAssetInformation.SelectedMainCurrencyBalance +
                                                    currentAssetInformation.BaseAndQuoteBalance,2);
                                    }
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
                                        decimal bidSumOrderSize = bidOrderList.Sum(order => order.Size.ToDecimal());
                                        int indexOfMaxBidOrderSize = bidOrderList.FindIndex(a => a.Size.ToDecimal() == bidMaxOrderSize);
                                        decimal askMaxOrderSize = askOrderList.Max(order => order.Size.ToDecimal());
                                        decimal askSumOrderSize = askOrderList.Sum(order => order.Size.ToDecimal());
                                        int indexOfMaxAskOrderSize = askOrderList.FindIndex(a => a.Size.ToDecimal() == askMaxOrderSize);
                                        //price and size
                                        bidOrderList = bidOrderList.Take(indexOfMaxBidOrderSize + 1).ToList();
                                        askOrderList = askOrderList.Take(indexOfMaxAskOrderSize + 1).ToList();
                                        currentAssetInformation.BidPriceAndSize = (from orderList in bidOrderList
                                            select new PriceAndSize { Size = orderList.Size.ToDecimal(), Price = orderList.Price.ToDecimal() }).ToList();
                                        currentAssetInformation.AskPriceAndSize = (from orderList in askOrderList
                                            select new PriceAndSize { Size = orderList.Size.ToDecimal(), Price = orderList.Price.ToDecimal() }).ToList();
                                        //Get volume difference for each side
                                        if (currentAssetInformation.BidPriceAndSize.Count > 0 &&
                                            currentAssetInformation.AskPriceAndSize.Count > 0)
                                        {
                                            //BID: The bid price represents the maximum price that a buyer is willing to pay
                                            //ASK: The ask price represents the minimum price that a seller is willing to take
                                            currentAssetInformation.IsVolumeBuySide = bidSumOrderSize > askSumOrderSize;
                                            //Get the order book difference
                                            if (currentAssetInformation.IsVolumeBuySide)
                                            {
                                                currentAssetInformation.SizePercentageDifference =
                                                    Math.Round((bidSumOrderSize - askSumOrderSize) / bidSumOrderSize, 2) * 100;
                                            }
                                            else
                                            {
                                                currentAssetInformation.SizePercentageDifference =
                                                    Math.Round((askSumOrderSize - bidSumOrderSize) / askSumOrderSize, 2) * 100;
                                            }
                                        }
                                        currentAssetInformation.BidMaxOrderSize = bidMaxOrderSize; 
                                        currentAssetInformation.IndexOfMaxBidOrderSize = indexOfMaxBidOrderSize;
                                        currentAssetInformation.AskMaxOrderSize = askMaxOrderSize; 
                                        currentAssetInformation.IndexOfMaxAskOrderSize = indexOfMaxAskOrderSize;
                                        currentAssetInformation.RoundDecimals();
                                    }
                                    NotifyAssetInformation?.Invoke(ApplicationName, AssetInformation);
                                    NotifyCurrentPrices?.Invoke(ApplicationName, SubscribedPrices);
                                }
                            }
                            catch (Exception ext)
                            {
                                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                                    $"Method: StartProcessingFeed Inner Exception\r\nException Stack Trace: {ext.StackTrace}\r\nJSON: {json}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        DelegateNotifySubscriptionStatus?.Invoke(ApplicationName, false);
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
                ApplicationName = "Coinbase Exchange";
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
                    ProcessLogBroadcast = (messageType, message) => { ProcessLogBroadcast.Invoke(ApplicationName, messageType,message);}
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
                        INIFilePath = Path.Combine(directoryName, "coinbase.config.ini");
                    }
                }
                else
                {
                    INIFilePath = Path.Combine(INIFilePath, "coinbase.config.ini");
                    if (!File.Exists(INIFilePath))
                    {
                        string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                        if (!string.IsNullOrEmpty(directoryName))
                        {
                            string tempPath = Path.Combine(directoryName, "coinbase.config.ini");
                            if (File.Exists(tempPath))
                            {
                                FileInfo file = new FileInfo(INIFilePath);
                                file.Directory?.Create();
                                File.Copy(tempPath, INIFilePath,true);
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
                        FileName = Path.Combine(file.Directory.FullName, $"coinbase_{env}_{connectionContainsEnvironment}.json");
                    }
                }
                Load();
                await UpdateProductsAsync();
                await UpdateAccountsAsync();
                if (Accounts != null && Accounts.Any())
                {
                    await UpdateAccountHistoryAsync(Accounts[0].ID);
                    await UpdateAccountHoldsAsync(Accounts[0].ID);
                    return true;
                }
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
                string coinbaseRSIFile = null;
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
                        coinbaseRSIFile = Path.Combine(directoryName, $"data\\coinbase_{env}_{connectionContainsEnvironment}");
                }
                else
                {
                    coinbaseRSIFile = Path.Combine(IndicatorSaveDataPath, $"coinbase_{env}_{connectionContainsEnvironment}");
                }
                if (string.IsNullOrEmpty(coinbaseRSIFile)) 
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
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: InitIndicatorsAsync\r\nException Stack Trace: {e.StackTrace}");
            }

            return false;
        }
        public override async Task<Statistics> TwentyFourHoursRollingStatsAsync(Product product)
        {
            string json = null;
            try
            {
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/products/{product.ID}/stats");
                json = await ConnectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                {
                    Statistics statistics = JsonSerializer.Deserialize<Statistics>(json);
                    Statistics ??= new Dictionary<string, Statistics>();
                    if (!Statistics.ContainsKey(product.ID))
                        Statistics.Add(product.ID, statistics);
                    Statistics[product.ID] = statistics;
                    return statistics;
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: TwentyFourHoursRollingStatsAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return null;
        }
        public override async Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(HistoricCandlesSearch historicCandlesSearch)
        {
            string json = null;
            try
            {
                if (historicCandlesSearch.StartingDateTime.AddMilliseconds((double)historicCandlesSearch
                    .Granularity) >= historicCandlesSearch.EndingDateTime)
                    return null;
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/products/{historicCandlesSearch.Symbol}/candles?start={historicCandlesSearch.StartingDateTime:o}&end={historicCandlesSearch.EndingDateTime:o}&granularity={(int)historicCandlesSearch.Granularity}");
                json = await ConnectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json) && json.StartsWith('[') && json.EndsWith(']'))
                {
                    ArrayList[] candles = JsonSerializer.Deserialize<ArrayList[]>(json);
                    HistoricRates = candles.ToHistoricRateList();
                    HistoricRates.Reverse();
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateProductHistoricCandlesAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }
            return HistoricRates;
        }
        public override async Task<List<Fill>> UpdateFillsAsync(Product product)
        {
            string json = null;
            try
            {
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/fills?product_id={product.ID ?? string.Empty}");
                json = await ConnectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    Fills = JsonSerializer.Deserialize<List<Fill>>(json);
                NotifyFills?.Invoke(ApplicationName, Fills);
                await UpdateFillStatistics(product, Fills);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: UpdateFillsAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return Fills;
        }
        public override async Task<Order> PostOrdersAsync(Order order)
        {
            string json = null;
            Order outputOrder = null;
            try
            {
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
                await UpdateAccountsAsync();
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: PostOrdersAsync\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
            }

            return outputOrder;
        }
        public override async Task<List<Order>> CancelAllOrdersAsync(Product product)
        {
            string json = null;
            List<Order> ordersOutput = new List<Order>();
            try
            {
                if (product == null)
                    return ordersOutput;
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "DELETE",
                    $"/orders?product_id={product.ID ?? string.Empty}");
                json = await ConnectionAdapter.RequestAsync(request);
                if (!json.StartsWith('[') && !json.EndsWith(']'))
                {
                    string orderId = JsonSerializer.Deserialize<string>(json);
                    if (string.IsNullOrEmpty(orderId))
                        return ordersOutput;
                    ordersOutput = Orders.Where(x => x.ID == orderId).ToList();
                    int removed = Orders.RemoveAll(x => x.ID == orderId);
                    ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                        removed > 0
                            ? $"Removing Order IDs: {orderId} from Orders."
                            : $"No update from order cancel\r\nRequested URL: {request.RequestUrl}");
                    if (!ordersOutput.Any())
                        ordersOutput.Add(new Order { ID = orderId });
                    await UpdateAccountsAsync();
                    return ordersOutput;
                }
                else
                {
                    List<string> orderIds = JsonSerializer.Deserialize<string[]>(json)?.ToList();
                    if (orderIds == null)
                        return ordersOutput;
                    ordersOutput = Orders.Where(x => orderIds.Contains(x.ID)).ToList();
                    int removed = Orders.RemoveAll(x => orderIds.Contains(x.ID));
                    ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                        removed > 0
                            ? $"Removing Order IDs: {orderIds} from Orders."
                            : $"No update from order cancel\r\nRequested URL: {request.RequestUrl}");
                    if (!ordersOutput.Any())
                        ordersOutput = (from id in orderIds select new Order { ID = id }).ToList();
                    await UpdateAccountsAsync();
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
        public override async Task<List<Order>> CancelOrderAsync(Order order)
        {
            string json = null;
            List<Order> ordersOutput = new List<Order>();
            try
            {
                if (order == null)
                    return ordersOutput;
                Request request = new Request(ConnectionAdapter.Authentication.EndpointUrl, "DELETE",
                    $"/orders/{order.ID ?? string.Empty}");
                json = await ConnectionAdapter.RequestAsync(request);
                if (!json.StartsWith('[') && !json.EndsWith(']'))
                {
                    string orderId = JsonSerializer.Deserialize<string>(json);
                    if (string.IsNullOrEmpty(orderId))
                        return ordersOutput;
                    ordersOutput = Orders.Where(x => x.ID == orderId).ToList();
                    int removed = Orders.RemoveAll(x => x.ID == orderId);
                    ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
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
                    ordersOutput = Orders.Where(x => orderIds.Contains(x.ID)).ToList();
                    int removed = Orders.RemoveAll(x => orderIds.Contains(x.ID));
                    ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General,
                        removed > 0
                            ? $"Removing Order IDs: {orderIds} from Orders."
                            : $"No update from order cancel\r\nRequested URL: {request.RequestUrl}");
                    if (!ordersOutput.Any())
                        ordersOutput = (from id in orderIds select new Order { ID = id }).ToList();
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
        public override async Task<List<Order>> UpdateOrdersAsync(Product product = null)
        {
            string json = null;
            try
            {
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
        #endregion

        #endregion
    }
}