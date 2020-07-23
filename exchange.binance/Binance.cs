using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using exchange.core;
using exchange.core.Enums;
using exchange.core.helpers;
using exchange.core.implementations;
using exchange.core.Indicators;
using exchange.core.interfaces;
using exchange.core.Interfaces;
using exchange.core.models;
using exchange.core.Models;

namespace exchange.binance
{
    
    public class Binance : AbstractExchangePlugin, IDisposable
    {
        #region Fields

        private IConnectionAdapter _connectionAdapter;
        private object _ioLock;
        private ClientWebSocket _clientWebSocket;
        #endregion

        #region Public Properties
        public Authentication _authentication;
        public string FileName { get; set; }
        public ServerTime ServerTime { get; set; }
        public List<Account> Accounts { get; set; }
        public List<Ticker> Tickers { get; set; }
        public BinanceAccount BinanceAccount { get; set; }
        public ExchangeInfo ExchangeInfo { get; set; }
        public List<HistoricRate> HistoricRates { get; set; }
        public List<Fill> Fills { get; set; }
        public List<BinanceFill> BinanceFill { get; set; }
        public List<Order> Orders { get; set; }
        public OrderBook OrderBook { get; set; }
        public Product SelectedProduct { get; set; }
        public List<AccountHistory> AccountHistories { get; set; }
        public List<AccountHold> AccountHolds { get; set; }
        #endregion

        public void LoadINI(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    if (_authentication == null)
                        _authentication = new Authentication();
                    string line;
                    StreamReader streamReader = new StreamReader(filePath);
                    while((line = streamReader.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line))
                            continue;
                        if (line.StartsWith("uri="))
                        {
                            line = line.Replace("uri=", "").Trim();
                            if (string.IsNullOrEmpty(line))
                                continue;
                            _authentication.WebSocketUri = new Uri(line);
                        }
                        else if (line.StartsWith("key="))
                        {
                            line = line.Replace("key=", "").Trim();
                            if (string.IsNullOrEmpty(line))
                                continue;
                            _authentication.ApiKey = line;
                        }
                        else if (line.StartsWith("secret="))
                        {
                            line = line.Replace("secret=", "").Trim();
                            if (string.IsNullOrEmpty(line))
                                continue;
                            _authentication.Secret = line;
                        }
                        else if (line.StartsWith("endpoint="))
                        {
                            line = line.Replace("endpoint=", "").Trim();
                            if (string.IsNullOrEmpty(line))
                                continue;
                            _authentication.EndpointUrl = line;
                        }
                    }
                    _connectionAdapter.Authentication = _authentication;
                }
            }
            catch
            {
            }
        }
        private void Save()
        {
            try
            {
                lock (_ioLock)
                {
                    if (string.IsNullOrEmpty(FileName))
                    {
                        string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                        FileName = Path.Combine(directoryName, "binance.json");
                        if (!File.Exists(FileName))
                            File.Create(FileName).Close();
                    }
                    BinanceSettings binanceSettings = new BinanceSettings();
                    binanceSettings.Accounts = new List<Account>();
                    binanceSettings.BinanceAccount = BinanceAccount;
                    binanceSettings.CurrentPrices = CurrentPrices;
                    binanceSettings.ExchangeInfo = ExchangeInfo;
                    binanceSettings.ServerTime = ServerTime;
                    binanceSettings.Tickers = Tickers;
                    string json = JsonSerializer.Serialize(binanceSettings, new JsonSerializerOptions() { WriteIndented = true });
                    File.WriteAllText(FileName, json);
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: Save\r\nException Stack Trace: {e.StackTrace}");
            }
        }
        public static BinanceSettings Load(string fileName)
        {
            try
            {
                string json = File.ReadAllText(fileName);
                return JsonSerializer.Deserialize<BinanceSettings>(json);
            }
            catch
            {

            }
            return default;
        }

        public Binance()
        {
            base.ApplicationName = "Binance Exchange";
            CurrentPrices = new Dictionary<string, decimal>();
            Tickers = new List<Ticker>();
            BinanceAccount = new BinanceAccount();
            AccountHistories = new List<AccountHistory>();
            AccountHolds = new List<AccountHold>();
            Products = new List<Product>();
            HistoricRates = new List<HistoricRate>();
            Fills = new List<Fill>();
            Orders = new List<Order>();
            OrderBook = new OrderBook();
            SelectedProduct = new Product();
            ServerTime = new ServerTime(0);
            _ioLock = new object();
            _clientWebSocket = new ClientWebSocket();
        }
        public async Task<ServerTime> UpdateTimeServerAsync()
        {
            Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                $"/api/v1/time");
            string json = await _connectionAdapter.RequestUnsignedAsync(request);
            ServerTime = JsonSerializer.Deserialize<ServerTime>(json);
            return ServerTime;
        }
        public async Task<ExchangeInfo> UpdateExchangeInfoAsync()
        {
            try
            {
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET",
                    $"/api/v1/exchangeInfo");
                string json = await _connectionAdapter.RequestUnsignedAsync(request);
                if (!string.IsNullOrEmpty(json))
                {
                    ExchangeInfo = JsonSerializer.Deserialize<ExchangeInfo>(json);
                    Save();
                }           
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateExchangeInfoAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return ExchangeInfo;
        }
        public async Task<BinanceAccount> UpdateBinanceAccountAsync()
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"[Binance] Updating Account Information.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/api/v3/account?")
                {
                    RequestQuery = $"timestamp={serverTime.ServerTimeLong}"
                };
                string json = await _connectionAdapter.RequestAsync(request);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateAccountsAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                BinanceAccount = JsonSerializer.Deserialize<BinanceAccount>(json);
                List<Asset> assets = BinanceAccount.Balances.Where(x => x.Free.ToDecimal() > 0 || (x.ID == "BTC")).ToList();
                Accounts = new List<Account>();              
                foreach (Asset asset in assets)
                {
                    Accounts.Add(new Account() { Hold = asset.Free, Balance = asset.Free, ID = asset.ID, Currency = asset.ID });
                }
                if (ExchangeInfo == null || ExchangeInfo.Symbols == null || !ExchangeInfo.Symbols.Any())
                    return BinanceAccount;
                foreach (Symbol symbol in ExchangeInfo.Symbols)
                {
                    if (BinanceAccount.Balances.Any(x => x.Free.ToDecimal() > 0 && (x.ID == symbol.QuoteAsset)))
                    {
                        Filter filter = symbol.Filters.FirstOrDefault(x => x.FilterType == "PRICE_FILTER");
                        Products.Add(new Product()
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
                return BinanceAccount;
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateAccountsAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return BinanceAccount;
        }
        public Task<List<Account>> UpdateAccountsAsync(string accountId = "")
        {
            throw new NotImplementedException();
        }
        public async Task<BinanceOrder> BinancePostOrdersAsync(BinanceOrder order)
        {
            BinanceOrder binanceOrder = null;
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"[Binance] Post Order Information.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "POST",
                    $"/api/v3/order?");
                if (order.OrderType == OrderType.Market)
                    request.RequestQuery = $"timestamp={serverTime.ServerTimeLong}&symbol={order.Symbol.ToUpper()}" +
                                           $"&side={order.OrderSide.ToString().ToUpper()}" +
                                           $"&type={order.OrderType.ToString().ToUpper()}&quantity={order.OrderSize}";
                else
                    request.RequestQuery = $"timestamp={serverTime.ServerTimeLong}&symbol={order.Symbol.ToUpper()}" +
                                           $"&side={order.OrderSide.ToString().ToUpper()}&type={order.OrderType.ToString().ToUpper()}" +
                                           $"&quantity={order.OrderSize}&price={ order.LimitPrice}&timeInForce=GTC";
                string json = await _connectionAdapter.RequestAsync(request);
                binanceOrder = JsonSerializer.Deserialize<BinanceOrder>(json);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateAccountsAsync JSON:\r\n{json}");
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: BinancePostOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return binanceOrder;
        }
        public async Task<BinanceOrder> BinanceCancelOrdersAsync(BinanceOrder binanceOrder)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Cancelling order.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl,
                    "DELETE",
                    $"/api/v3/order?")
                {
                    RequestQuery =
                        $"symbol={binanceOrder.Symbol}&orderId={binanceOrder.ID}&timestamp={serverTime.ServerTimeLong}"
                };
                string json = await _connectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    binanceOrder = JsonSerializer.Deserialize<BinanceOrder>(json);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"BinanceCancelOrdersAsync JSON:\r\n{json}");
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: BinanceCancelOrdersAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return binanceOrder;
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
                        $"/api/v3/ticker/price") {RequestQuery = $"?symbol={product.ID}"};
                    string json = await _connectionAdapter.RequestUnsignedAsync(request);
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
        public async Task<List<BinanceFill>> UpdateBinanceFillsAsync(Product product)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Updating Fills Information.");
                ServerTime serverTime = await UpdateTimeServerAsync();
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl,
                    "GET",
                    $"/api/v3/myTrades?")
                {
                    RequestQuery =
                        $"symbol={product.ID}&recvWindow=5000&timestamp={serverTime.ServerTimeLong}&limit=10"
                };
                string json = await _connectionAdapter.RequestAsync(request);
                if (!string.IsNullOrEmpty(json))
                    BinanceFill = JsonSerializer.Deserialize<List<BinanceFill>>(json);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateAccountsAsync JSON:\r\n{json}");
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateFillsAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return BinanceFill;
        }
        public async Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"[Binance] Updating Product Order Book.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/api/v3/depth?")
                {
                    RequestQuery = $"symbol={product.ID}&limit={level}"
                };
                string json = await _connectionAdapter.RequestUnsignedAsync(request);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateProductOrderBookAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                OrderBook = JsonSerializer.Deserialize<OrderBook>(json);
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateAccountsAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return OrderBook;
        }
        public override async Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(HistoricCandlesSearch historicCandlesSearch)
        {
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"[Binance] Updating Product Historic Candles.");
                Request request = new Request(_connectionAdapter.Authentication.EndpointUrl, "GET", $"/api/v1/klines?")
                {
                    RequestQuery =
                        $"symbol={historicCandlesSearch.Symbol}&interval=5m&startTime={historicCandlesSearch.StartingDateTime.GenerateDateTimeOffsetToUnixTimeMilliseconds()}&endTime={historicCandlesSearch.EndingDateTime.GenerateDateTimeOffsetToUnixTimeMilliseconds()}"
                };
                string json = await _connectionAdapter.RequestUnsignedAsync(request);
                ProcessLogBroadcast?.Invoke(MessageType.JsonOutput, $"UpdateProductOrderBookAsync JSON:\r\n{json}");
                //check if we do not have any error messages
                if(json.StartsWith("[") && json.EndsWith("]"))
                {
                    ArrayList[] arrayListOfHistory = JsonSerializer.Deserialize<ArrayList[]>(json);
                    HistoricRates = arrayListOfHistory.ToHistoricCandleList();
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: UpdateProductHistoricCandlesAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return HistoricRates;
        }
        
        public void StartProcessingFeed()
        {
            Task.Run(async () =>
            {
                string json = null;
                try
                {
                    ProcessLogBroadcast?.Invoke(MessageType.General, $"Started Processing Feed Information.");                
                    string message = "stream?streams=";              
                    List<Product> products = new List<Product>
                    {
                        Products.FirstOrDefault(x => x.BaseCurrency == "BNB" && x.QuoteCurrency == "BUSD"),
                        Products.FirstOrDefault(x => x.BaseCurrency == "ETH" && x.QuoteCurrency == "BTC")
                    };
                    foreach (Product product in products)
                    {
                        message += product.ID.ToLower() + "@trade/";
                    }

                    if (string.IsNullOrWhiteSpace(message) || !message.Contains("@trade"))
                        return;
                    int lastIndexOfSlash = message.LastIndexOf("/", StringComparison.Ordinal);
                    if (lastIndexOfSlash != -1)
                        message = message.Remove(lastIndexOfSlash, 1);
                    while (Accounts.Any())
                    {
                        string uriString = _connectionAdapter.Authentication.WebSocketUri + message;
                        await _connectionAdapter.ConnectAsync(uriString, _clientWebSocket);
                        while (_connectionAdapter.IsWebSocketConnected(_clientWebSocket))
                        {
                            json = await _connectionAdapter.WebSocketReceiveAsync(_clientWebSocket).ConfigureAwait(false);
                            if (string.IsNullOrEmpty(json))
                                continue;
                            Feed feed = JsonSerializer.Deserialize<Feed>(json);
                            if (feed == null || feed.Type == "error")
                                return;
                            CurrentPrices[feed.ProductID] = feed.Price.ToDecimal();
                            feed.CurrentPrices = CurrentPrices;

                            FeedBroadcast?.Invoke(feed);
                        }
                    }
                    
                }
                catch (Exception e)
                {
                    ProcessLogBroadcast?.Invoke(MessageType.Error,
                        $"Method: StartProcessingFeed\r\nException Stack Trace: {e.StackTrace}\r\nJSON: {json}");
                }
            });

           
        }
        public override void Dispose()
        {
            _connectionAdapter?.Dispose();
            CurrentPrices = null;
            Tickers = null;
            BinanceAccount = null;
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
        public override async Task<bool> InitAsync()
        {
            try
            {
                if (!_connectionAdapter.Validate())
                    return false;
                await UpdateExchangeInfoAsync();
                await UpdateBinanceAccountAsync();
                List<Product> products = new List<Product>
                {
                    Products.FirstOrDefault(x => x.BaseCurrency == "BNB" && x.QuoteCurrency == "BUSD"),
                    Products.FirstOrDefault(x => x.BaseCurrency == "ETH" && x.QuoteCurrency == "BTC")
                };
                products.RemoveAll(x => x == null);
                if (products.Any())
                {
                    await UpdateProductOrderBookAsync(products[0], 20);
                    HistoricCandlesSearch historicCandlesSearch = new HistoricCandlesSearch();
                    historicCandlesSearch.Symbol = products[0].ID;
                    historicCandlesSearch.StartingDateTime = DateTime.Now.AddHours(-2).ToUniversalTime();
                    historicCandlesSearch.EndingDateTime = DateTime.Now.ToUniversalTime();
                    historicCandlesSearch.Granularity = (Granularity)900;
                    await UpdateProductHistoricCandlesAsync(historicCandlesSearch);
                    await UpdateTickersAsync(products);
                    StartProcessingFeed();
                }         
                return true;
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: InitAsync\r\nException Stack Trace: {e.StackTrace}");
            }
            return false;
        }
        public override bool InitIndicatorsAsync()
        {
            RelativeStrengthIndex r = new RelativeStrengthIndex();
            return true;
        }
        public override bool InitConnectionAdapter(IConnectionAdapter connectionAdapter)
        {
            _connectionAdapter = connectionAdapter;
            string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            string fileName = Path.Combine(directoryName, "binance.config.ini");
            LoadINI(fileName);
            return true;
        }
    }
}