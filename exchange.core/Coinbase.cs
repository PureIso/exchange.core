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
        public bool IsWebSocketClosed { get; set; }
        public Product SelectedProduct { get; set; }
        #endregion

        public Coinbase(IConnectionFactory connectionFactory)
        {
            _authentication = connectionFactory.Authentication;
            _httpClient = connectionFactory.HttpClient;
            _ioRequestSemaphoreSlim = new SemaphoreSlim(1, 1);
            _ioSemaphoreSlim = new SemaphoreSlim(1, 1);
            _clientWebsocket = new ClientWebSocket();
            CurrentPrices = new Dictionary<string, decimal>();
            Tickers = new List<Ticker>();
            IsWebSocketClosed = true;
        }

        #region Public Methods
        public async Task<List<Account>> UpdateAccountsAsync()
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", "/accounts");
            string json = await RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Accounts;
            Accounts = JsonSerializer.Deserialize<List<Account>>(json);
            return Accounts;
        }
        public async Task<List<Product>> UpdateProductsAsync()
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/products");
            string json = await RequestAsync(request);
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
                string json = await RequestAsync(request);
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
            string json = await RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Fills;
            Fills = JsonSerializer.Deserialize<List<Fill>>(json);
            return Fills;
        }
        public async Task<List<Order>> UpdateOrdersAsync(Product product = null)
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/orders?status=open&status=pending&status=active&product_id={product?.ID ?? string.Empty}");
            string json = await RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return Orders;
            Orders = JsonSerializer.Deserialize<List<Order>>(json);
            return Orders;
        }
        public async Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 2)
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/products/{product.ID}/book?level={level}");
            string json = await RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return OrderBook;
            OrderBook = JsonSerializer.Deserialize<OrderBook>(json);
            return OrderBook;
        }
        public async Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(Product product, DateTime startingDateTime, DateTime endingDateTime, int granularity = 86400)
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/products/{product.ID}/candles?start={startingDateTime:o}&end={endingDateTime:o}&granularity={granularity}");
            string json = await RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return HistoricRates;
            ArrayList[] candles = JsonSerializer.Deserialize<ArrayList[]>(json);
            HistoricRates = candles.ToHistoricRateList();
            return HistoricRates;
        }
        public void WebSocketClose()
        {
            IsWebSocketClosed = WebSocketCloseAndDisposeAsync().GetAwaiter().GetResult();
        }
        public void WebSocketSubscribe(List<Product> products)
        {
            Task.Run(async () => {
                IsWebSocketClosed = false;
                while (!IsWebSocketClosed)
                {
                    try
                    {
                        if (products == null || !products.Any())
                        {
                            IsWebSocketClosed = true;
                            return;
                        }
                        string productIds = null;
                        foreach (Product product in products)
                        {
                            productIds += $@"""{product.ID}"",";
                        }

                        if (productIds == null) 
                            continue;
                        productIds = productIds.Remove(productIds.Length - 1, 1);
                        string message =
                            $@"{{""type"": ""subscribe"",""channels"": [{{""name"": ""ticker"",""product_ids"": [{productIds}]}}]}}";
                        Feed feed = await WebSocketSendAsync(message);
                        if (feed == null || feed.Type == "error")
                        {
                            IsWebSocketClosed = true;
                            return;
                        }
                        while (!IsWebSocketClosed)
                        {
                            string json = await WebSocketReceiveAsync().ConfigureAwait(false);
                            feed = JsonSerializer.Deserialize<Feed>(json);
                            if (feed == null || feed.Type == "error")
                            {
                                IsWebSocketClosed = true;
                                return;
                            }

                            FeedBroadCast?.Invoke(feed);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            });
            Task.Delay(1000).Wait();
        }
        #endregion

        #region Private Methods
        private async Task<string> RequestAsync(IRequest request)
        {
            try
            {
                await _ioRequestSemaphoreSlim.WaitAsync();
                IAuthenticationSignature authenticationSignature = _authentication.ComputeSignature(request);
                StringContent requestBody = request.GetRequestBody();
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("CB-ACCESS-KEY", _authentication.ApiKey);
                _httpClient.DefaultRequestHeaders.Add("CB-ACCESS-PASSPHRASE", _authentication.Passphrase);
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "sefbkn.github.io");
                _httpClient.DefaultRequestHeaders.Add("CB-ACCESS-SIGN", authenticationSignature.Signature);
                _httpClient.DefaultRequestHeaders.Add("CB-ACCESS-TIMESTAMP", authenticationSignature.Timestamp);
                HttpResponseMessage response = null;
                switch (request.Method)
                {
                    case "GET":
                        if (requestBody != null)
                            response = await _httpClient.PostAsync(request.AbsoluteUri, requestBody);
                        else
                            response = await _httpClient.GetAsync(request.AbsoluteUri);
                        break;
                    case "POST":
                        if (requestBody != null)
                            response = await _httpClient.PostAsync(request.AbsoluteUri, requestBody);
                        break;
                    case "DELETE":
                        response = await _httpClient.DeleteAsync(request.AbsoluteUri);
                        break;
                    default:
                        throw new NotImplementedException("The supplied HTTP method is not supported: " +
                                                            request.Method);
                }
                if (response == null)
                    throw new Exception("Create Category returned no response");
                string contentBody = await response.Content.ReadAsStringAsync();
                return !response.IsSuccessStatusCode ? null : contentBody;
            }
            catch (Exception)
            {
            }
            finally
            {
                await Task.Delay(500);
                _ioRequestSemaphoreSlim.Release();
            }

            return null;
        }
        private bool IsWebSocketConnected()
        {
            return _clientWebsocket != null && GetWebSocketState() == WebSocketState.Open;
        }
        #endregion

        #region Virtual Methods
        public virtual async Task<Feed> WebSocketSendAsync(string message)
        {
            Feed feed = new Feed { Type = "error" };
            try
            {
                await _ioSemaphoreSlim.WaitAsync();  
                if (_clientWebsocket == null)
                    return feed;
                byte[] requestBytes = Encoding.UTF8.GetBytes(message);
                if (GetWebSocketState() != WebSocketState.Open)
                {
                    await _clientWebsocket.ConnectAsync(_authentication.WebSocketUri, CancellationToken.None);
                }
                ArraySegment<byte> subscribeRequest = new ArraySegment<byte>(requestBytes);
                await _clientWebsocket.SendAsync(subscribeRequest, WebSocketMessageType.Text, true,
                    CancellationToken.None);
                if (GetWebSocketState() != WebSocketState.Open)
                    return null;
                ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(new byte[512 * 512 * 5]);
                WebSocketReceiveResult webSocketReceiveResult = await _clientWebsocket.ReceiveAsync(
                    receiveBuffer,
                    CancellationToken.None);
                if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                {
                    _clientWebsocket.Abort();
                    _clientWebsocket.Dispose();
                    return null;
                }
                if (webSocketReceiveResult.Count == 0 || !receiveBuffer.Any() || receiveBuffer.Array == null)
                    return null;
                string json = Encoding.UTF8.GetString(receiveBuffer.Array, 0, webSocketReceiveResult.Count);
                if (string.IsNullOrWhiteSpace(json))
                {
                    WebSocketClose();
                    return feed;
                }
                feed = JsonSerializer.Deserialize<Feed>(json);
            }
            catch (Exception)
            {
                await Task.Delay(5000);
            }
            finally
            {
                _ioSemaphoreSlim.Release();
            }
            return feed;
        }
        public virtual async Task<string> WebSocketReceiveAsync()
        {
            try
            {
                await _ioSemaphoreSlim.WaitAsync();
                if (_clientWebsocket == null)
                    return null;
                if (GetWebSocketState() != WebSocketState.Open)
                    return null;
                ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(new byte[512 * 512 * 5]);
                WebSocketReceiveResult webSocketReceiveResult = await _clientWebsocket.ReceiveAsync(
                    receiveBuffer,
                    CancellationToken.None);
                if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                {
                    _clientWebsocket.Abort();
                    _clientWebsocket.Dispose();
                    return null;
                }
                if (webSocketReceiveResult.Count == 0 || !receiveBuffer.Any() || receiveBuffer.Array == null)
                    return null;
                return Encoding.UTF8.GetString(receiveBuffer.Array, 0, webSocketReceiveResult.Count);
            }
            catch (Exception)
            {
                await Task.Delay(5000);
            }
            finally
            {
                _ioSemaphoreSlim.Release();
            }
            return null;
        }
        public virtual async Task<bool> WebSocketCloseAndDisposeAsync()
        {
            try
            {
                await _ioSemaphoreSlim.WaitAsync();
                if (_clientWebsocket == null || !IsWebSocketConnected())
                    return true;
                await _clientWebsocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty,CancellationToken.None);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                _ioSemaphoreSlim.Release();
            }
            return false;
        }
        #endregion
    }
}