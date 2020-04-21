using System;
using System.Linq;
using exchange.core.Interfaces;
using exchange.core.models;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.core
{
    public class ConnectionAdapter : IConnectionAdapter
    {
        #region Properties
        public Authentication Authentication { get; }
        public HttpClient HttpClient { get; }
        public ClientWebSocket ClientWebSocket { get; }
        #endregion

        #region Fields
        private readonly SemaphoreSlim _ioRequestSemaphoreSlim;
        private readonly SemaphoreSlim _ioSemaphoreSlim;
        #endregion

        #region Events
        public Action<Feed> FeedBroadCast { get; set; }
        #endregion

        public ConnectionAdapter(HttpClient httpClient, IExchangeSettings exchangeSettings)
        {
            Authentication = new Authentication(
                exchangeSettings.APIKey,
                exchangeSettings.PassPhrase,
                exchangeSettings.Secret,
                exchangeSettings.EndpointUrl,
                exchangeSettings.Uri);
            HttpClient = httpClient;
            ClientWebSocket = new ClientWebSocket();
            _ioSemaphoreSlim = new SemaphoreSlim(1,1);
            _ioRequestSemaphoreSlim = new SemaphoreSlim(1,1);
        }

        #region Web Socket
        public virtual async Task<string> WebSocketSendAsync(string message)
        {
            try
            {
                await _ioSemaphoreSlim.WaitAsync();
                if (string.IsNullOrEmpty(message))
                    return null;
                byte[] requestBytes = Encoding.UTF8.GetBytes(message);
                if (!IsWebSocketConnected())
                    await ClientWebSocket.ConnectAsync(Authentication.WebSocketUri, CancellationToken.None);
                ArraySegment<byte> subscribeRequest = new ArraySegment<byte>(requestBytes);
                await ClientWebSocket.SendAsync(subscribeRequest, WebSocketMessageType.Text, true,
                    CancellationToken.None);
                if (!IsWebSocketConnected())
                    return null;
                ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(new byte[512 * 512 * 5]);
                WebSocketReceiveResult webSocketReceiveResult = await ClientWebSocket.ReceiveAsync(
                    receiveBuffer,
                    CancellationToken.None);
                if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                {
                    ClientWebSocket.Abort();
                    ClientWebSocket.Dispose();
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
        public virtual async Task<string> WebSocketReceiveAsync()
        {
            try
            {
                await _ioSemaphoreSlim.WaitAsync();
                if (ClientWebSocket == null)
                    return null;
                if (!IsWebSocketConnected())
                    return null;
                ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(new byte[512 * 512 * 5]);
                WebSocketReceiveResult webSocketReceiveResult = await ClientWebSocket.ReceiveAsync(
                    receiveBuffer,
                    CancellationToken.None);
                if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                {
                    ClientWebSocket.Abort();
                    ClientWebSocket.Dispose();
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
        public virtual async Task<bool> WebSocketCloseAsync()
        {
            try
            {
                await _ioSemaphoreSlim.WaitAsync();
                if (ClientWebSocket == null || !IsWebSocketConnected())
                    return true;
                await ClientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                Dispose();
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
        public virtual bool IsWebSocketConnected()
        {
            return ClientWebSocket != null && ClientWebSocket.State == WebSocketState.Open;
        }
        #endregion

        public void Dispose()
        {
            HttpClient.Dispose();
            ClientWebSocket.Dispose();
        }

        #region Private Methods
        public async Task<string> RequestAsync(IRequest request)
        {
            try
            {
                await _ioRequestSemaphoreSlim.WaitAsync();
                IAuthenticationSignature authenticationSignature = Authentication.ComputeSignature(request);
                StringContent requestBody = request.GetRequestBody();
                HttpClient.DefaultRequestHeaders.Clear();
                //The api key as a string.
                HttpClient.DefaultRequestHeaders.Add("CB-ACCESS-KEY", Authentication.ApiKey);
                //The passphrase you specified when creating the API key.
                HttpClient.DefaultRequestHeaders.Add("CB-ACCESS-PASSPHRASE", Authentication.Passphrase);
                //The base64-encoded signature (see Signing a Message).
                HttpClient.DefaultRequestHeaders.Add("CB-ACCESS-SIGN", authenticationSignature.Signature);
                // A timestamp for your request.
                HttpClient.DefaultRequestHeaders.Add("CB-ACCESS-TIMESTAMP", authenticationSignature.Timestamp);
                //user-agent header
                HttpClient.DefaultRequestHeaders.Add("User-Agent", "sefbkn.github.io");
                HttpResponseMessage response = null;
                switch (request.Method)
                {
                    case "GET":
                        if (requestBody != null)
                            response = await HttpClient.PostAsync(request.AbsoluteUri, requestBody);
                        else
                            response = await HttpClient.GetAsync(request.AbsoluteUri);
                        break;
                    case "POST":
                        if (requestBody != null)
                            response = await HttpClient.PostAsync(request.AbsoluteUri, requestBody);
                        break;
                    case "DELETE":
                        response = await HttpClient.DeleteAsync(request.AbsoluteUri);
                        break;
                    default:
                        throw new NotImplementedException("The supplied HTTP method is not supported: " +
                                                            request.Method);
                }
                if (response == null)
                    throw new Exception($"null response from RequestAsync \r\n URI:{request.AbsoluteUri} \r\n:Request Body:{requestBody}");
                return await response.Content.ReadAsStringAsync();
            }
            finally
            {
                await Task.Delay(500);
                _ioRequestSemaphoreSlim.Release();
            }
        }
        #endregion
    }
}
