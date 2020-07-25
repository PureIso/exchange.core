using System;
using System.Linq;
using System.Net;
using exchange.core.Interfaces;
using exchange.core.models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using exchange.core.Models;
using exchange.core.implementations;
using exchange.core.Enums;

namespace exchange.core
{
    public class ConnectionAdapter : IConnectionAdapter
    {
        #region Properties
        public Authentication Authentication { get; set; }
        public HttpClient HttpClient { get; set; }
        public ClientWebSocket ClientWebSocket { get; set; }
        #endregion

        #region Fields
        private readonly SemaphoreSlim _ioRequestSemaphoreSlim;
        private readonly SemaphoreSlim _ioSemaphoreSlim;
        #endregion

        public Action<MessageType, string> ProcessLogBroadcast { get; set; }

        public ConnectionAdapter()
        {
            HttpClient = new HttpClient();
            _ioSemaphoreSlim = new SemaphoreSlim(1, 1);
            _ioRequestSemaphoreSlim = new SemaphoreSlim(1, 1);
        }
        public ConnectionAdapter(HttpClient httpClient)
        {
            HttpClient = httpClient;
            _ioSemaphoreSlim = new SemaphoreSlim(1,1);
            _ioRequestSemaphoreSlim = new SemaphoreSlim(1,1);
        }

        #region Web Socket
        public virtual async Task ConnectAsync(string uriString)
        {
            try
            {
                await _ioSemaphoreSlim.WaitAsync();
                if (ClientWebSocket == null)
                    return;
                if (ClientWebSocket.State != WebSocketState.Open)
                {
                    await ClientWebSocket.ConnectAsync(new Uri(uriString), CancellationToken.None);
                }
            }
            catch (WebSocketException)
            {
                await Task.Delay(5000);
            }
            catch (WebException)
            {
                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: ConnectAsync\r\nException Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                _ioSemaphoreSlim.Release();
            }
        }
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
            catch (Exception ex)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: WebSocketSendAsync\r\nException Stack Trace: {ex.StackTrace}");
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
            catch (Exception ex)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: WebSocketReceiveAsync\r\nException Stack Trace: {ex.StackTrace}");
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
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: WebSocketReceiveAsync\r\nException Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                _ioSemaphoreSlim.Release();
            }
            return false;
        }
        public virtual bool IsWebSocketConnected()
        {
            if (ClientWebSocket == null)
                ClientWebSocket = new ClientWebSocket();
            return ClientWebSocket.State == WebSocketState.Open;
        }
        #endregion

        public void Dispose()
        {
            HttpClient?.Dispose();
            ClientWebSocket?.Dispose();
        }

        #region Private Methods
        public virtual async Task<string> RequestAsync(IRequest request)
        {
            try
            {
                await _ioRequestSemaphoreSlim.WaitAsync();
                StringContent requestBody = new StringContent("");
                HttpResponseMessage response;
                Uri absoluteUri;
                if (Authentication == null)
                    throw new Exception("Invalid Authentication.");
                if (string.IsNullOrEmpty(Authentication.Passphrase))
                {
                    request.RequestSignature = Authentication.ComputeSignature(request.RequestQuery);
                    request.RequestQuery = null;
                    absoluteUri = request.ComposeRequestUriAbsolute(Authentication.EndpointUrl);
                    HttpClient.DefaultRequestHeaders.Clear();
                    HttpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", Authentication.ApiKey);
                    HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
                }
                else
                {
                    AuthenticationSignature authenticationSignature = Authentication.ComputeSignature(request);
                    requestBody = request.GetRequestBody();
                    absoluteUri = request.AbsoluteUri;
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
                }

                switch (request.Method)
                {
                    case "GET":
                        response = await HttpClient.GetAsync(absoluteUri);
                        break;
                    case "POST":
                        response = await HttpClient.PostAsync(absoluteUri, requestBody);
                        break;
                    case "DELETE":
                        response = await HttpClient.DeleteAsync(absoluteUri);
                        break;
                    default:
                        throw new NotImplementedException("The supplied HTTP method is not supported: " +
                                                          request.Method);
                }

                if (response == null)
                    throw new Exception(
                        $"null response from RequestAsync \r\n URI:{request.AbsoluteUri} \r\n:Request Body:{requestBody}");
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                await Task.Delay(500);
                _ioRequestSemaphoreSlim.Release();
            }

            return null;
        }

        public virtual async Task<string> RequestUnsignedAsync(IRequest request)
        {
            try
            {
                HttpResponseMessage response = null; 
                HttpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", Authentication.ApiKey);
                switch (request.Method)
                {
                    case "GET":
                        response = await HttpClient.GetAsync(request.ComposeRequestUriAbsolute(Authentication.EndpointUrl));
                        break;
                    case "POST":
                        response = await HttpClient.PostAsync(request.ComposeRequestUriAbsolute(Authentication.EndpointUrl), new StringContent(""));
                        break;
                    case "DELETE":
                        response = await HttpClient.DeleteAsync(request.ComposeRequestUriAbsolute(Authentication.EndpointUrl));
                        break;
                    default:
                        response = null;
                        break;
                }
                if (response == null)
                    throw new Exception($"null response from RequestUnsignedAsync \r\n URI:{request.ComposeRequestUriAbsolute(Authentication.EndpointUrl)}");
                return await response.Content.ReadAsStringAsync();
            }
            finally
            {
                await Task.Delay(500);
            }
        }
        #endregion
    }
}
