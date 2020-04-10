using exchange.coinbase.models;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.coinbase
{
    public class Coinbase
    {
        private Authentication _authentication;
        private ClientWebSocket _webSocketClient;
        private readonly SemaphoreSlim _ioSemaphoreSlim;
        private readonly SemaphoreSlim _ioRequestSemaphoreSlim;
        private HttpClient _httpClient;

        public event Action<string> Log;
        public Dictionary<string, decimal> CurrentPrices { get; set; }

        public List<Ticker> Tickers { get; set; }
        public List<Account> Accounts { get; set; }
        public List<Product> Products { get; set; }
        public List<HistoricRate> HistoricRates { get; set; }
        public List<Fill> Fills { get; set; }
        public OrderBook OrderBook { get; set; }

        public Product SelectedProduct { get; set; }

        public Coinbase(Authentication authentication, HttpClient httpClient)
        {
            _webSocketClient = new ClientWebSocket();
            _ioSemaphoreSlim = new SemaphoreSlim(1, 1);
            _ioRequestSemaphoreSlim = new SemaphoreSlim(1, 1);
            _authentication = authentication;
            _httpClient = httpClient;

            CurrentPrices = new Dictionary<string, decimal>();
            Tickers = new List<Ticker>();
        }

        public async Task<string> RequestAsync(Request request)
        {
            try
            {
                await _ioRequestSemaphoreSlim.WaitAsync();
                AuthenticationSignature authenticationSignature = _authentication.ComputeSignature(request);
                StringContent requestBody = request.GetRequestBody();  
                Console.WriteLine("{0} - {1}", DateTime.Now, request.AbsoluteUri);

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
                if (!response.IsSuccessStatusCode)
                {
                    Log?.Invoke(string.Format("Exchange Request with not successful response: {0} code:{1}",
                            request.RequestUrl,
                            response.StatusCode));
                    return null;
                }
                return contentBody;               
            }
            catch (Exception ex)
            {
                Log?.Invoke(ex.StackTrace);
            }
            finally
            {
                await Task.Delay(500);
                _ioRequestSemaphoreSlim.Release();
            }

            return null;
        }
        public async Task UpdateAccountsAsync()
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", "/accounts");
            string json = await RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return;
            Accounts = JsonConvert.DeserializeObject<List<Account>>(json);
        }
        public async Task UpdateProductsAsync()
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/products");
            string json = await RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return;
            Products = JsonConvert.DeserializeObject<List<Product>>(json);
            Product p = Products[0];
        }
        public async Task UpdateTickersAsync(List<Product> products)
        {
            if (products == null || !products.Any())
                return;
            //Get price of all products
            foreach (Product product in products)
            {
                Request request = new Request(_authentication.EndpointUrl, "GET", $"/products/{product.ID}/ticker");
                string json = await RequestAsync(request);
                if (string.IsNullOrWhiteSpace(json))
                    return;
                Ticker ticker = JsonConvert.DeserializeObject<Ticker>(json);
                if (Tickers != null)
                    Tickers.RemoveAll(x => x.ProductID == product.ID);
                if (ticker != null)
                {
                    ticker.ProductID = product.ID;
                    Tickers.Add(ticker);
                }
            }
            foreach (Ticker ticker in Tickers)
            {
                if(decimal.TryParse(ticker.Price, out decimal decimalPrice))
                    CurrentPrices[ticker.ProductID] = decimalPrice;
            }                  
        }
        public async Task UpdateFillsAsync(Product product)
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/fills?product_id={product.ID ?? string.Empty}");
            string json = await RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return;
            Fills = JsonConvert.DeserializeObject<List<Fill>>(json);
        }
        public async Task UpdateProductOrderBookAsync(Product product, int level = 2)
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/products/{product.ID}/book?level={level}");
            string json = await RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return;
            OrderBook = JsonConvert.DeserializeObject<OrderBook>(json);           
        }
        public async Task UpdateProductHistoricCandlesAsync(Product product, DateTime startingDateTime, DateTime endingDateTime, int granularity = 86400)
        {
            Request request = new Request(_authentication.EndpointUrl, "GET", $"/products/{product.ID}/candles?start={startingDateTime:o}&end={endingDateTime:o}&granularity={granularity}");
            string json = await RequestAsync(request);
            if (string.IsNullOrWhiteSpace(json))
                return;
            ArrayList[] candles = JsonConvert.DeserializeObject<ArrayList[]>(json);
            HistoricRates = candles.ToHistoricRateList();
        }






        private readonly bool _close;
        #region wss

        public async Task SendAsync(string message)
        {
            try
            {
                await _ioSemaphoreSlim.WaitAsync();
                if (_webSocketClient == null)
                    return;
                byte[] requestBytes = Encoding.UTF8.GetBytes(message);
                if (_webSocketClient.State != WebSocketState.Open)
                {
                    Log?.Invoke("Web Socket connection not opened: " + _webSocketClient.State);
                    _webSocketClient.ConnectAsync(_authentication.ExchangeUri, CancellationToken.None).GetAwaiter().GetResult();
                }

                ArraySegment<byte> subscribeRequest = new ArraySegment<byte>(requestBytes);
                await _webSocketClient.SendAsync(subscribeRequest, WebSocketMessageType.Text, true,
                    CancellationToken.None);
            }
            catch (Exception)
            {
                await Task.Delay(5000);
            }
            finally
            {
                _ioSemaphoreSlim.Release();
            }
        }

        public async Task ReceiveAsync()
        {
            try
            {
                await _ioSemaphoreSlim.WaitAsync();
                if (_webSocketClient == null)
                    return;
                if (_webSocketClient.State != WebSocketState.Open)
                    return;
                ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(new byte[512 * 512 * 5]);
                WebSocketReceiveResult webSocketReceiveResult = await _webSocketClient.ReceiveAsync(
                    receiveBuffer,
                    CancellationToken.None);
                if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                {
                    _webSocketClient.Abort();
                    _webSocketClient.Dispose();
                    _webSocketClient = null;
                    return;
                }

                if (webSocketReceiveResult.Count == 0 || !receiveBuffer.Any() || receiveBuffer.Array == null)
                    return;
                string json = Encoding.UTF8.GetString(receiveBuffer.Array, 0, webSocketReceiveResult.Count);
                ParseMessage(json);
            }
            catch (Exception)
            {
                await Task.Delay(5000);
            }
            finally
            {
                _ioSemaphoreSlim.Release();
            }
        }

        private void ParseMessage(string message)
        {
            Console.WriteLine(message);
        }

        public async Task<bool> CloseAsync()
        {
            try
            {
                await _ioSemaphoreSlim.WaitAsync();
                if (_webSocketClient == null)
                    return true;
                await Task.Delay(1000);
                await _webSocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                    CancellationToken.None);
                return true;
            }
            catch (Exception ex)
            {
                Log?.Invoke(ex.StackTrace);
            }

            return false;
        }

        #endregion

        public bool IsConnected()
        {
            return _webSocketClient != null && _webSocketClient.State == WebSocketState.Open;
        }
        public async Task Subscribe()
        {
            //Log?.Invoke(string.Format("Initialising subscription to Product:{0}", SelectedProduct.ID));
            //_orderBookClient.OnMatch += OnMatch;
            //_orderBookClient.OnDone += OnOrderCompleted;
            //Log?.Invoke(string.Format("Subscribed to Product:{0}", SelectedProduct.ID));
            while (!_close)
            {
                if (string.IsNullOrEmpty(SelectedProduct?.ID))
                    throw new ArgumentNullException(nameof(SelectedProduct));
                string message = $@"{{""type"": ""subscribe"",""channels"": [{{""name"": ""ticker"",""product_ids"": [""{SelectedProduct.ID}""]}}]}}";
                await SendAsync(message);
                // while (IsConnected()) 
                while (IsConnected()) await ReceiveAsync();
            }
        }
    }
}
