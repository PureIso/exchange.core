using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using exchange.core.connectivity;
using exchange.core.utilities;

namespace exchange.core.implementations.connectivity
{
    public abstract class AbstractConnection: IConnection
    {
        #region Fields
        private SemaphoreSlim _ioSemaphoreSlim;
        private SemaphoreSlim _ioRequestSemaphoreSlim;
        private IAuthentication _authentication;
        private ClientWebSocket _webSocketClient;
        #endregion

        #region Events
        public virtual event Action<string> ParseOutput;
        public virtual event Action<string> Log;
        #endregion

        #region Methods
        public virtual void Initialise(IAuthentication authentication)
        {
            _ioSemaphoreSlim = new SemaphoreSlim(1, 1);
            _ioRequestSemaphoreSlim = new SemaphoreSlim(1, 1);
            _authentication = authentication;
        }
        public virtual void Connect()
        {
            _webSocketClient = new ClientWebSocket();
        }
        public virtual bool Disconnect()
        {
            try
            {
                _ioSemaphoreSlim.WaitAsync();
                if (_webSocketClient == null)
                    return true; 
                Task.Delay(1000); 
                _webSocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                return true;
            }
            catch (Exception ex)
            {
                Log?.Invoke(ex.StackTrace);
            }
            return false;
        }
        public virtual bool IsConnected()
        {
            return _webSocketClient != null && _webSocketClient.State == WebSocketState.Open;
        }
        public virtual async Task SendAsync(string message)
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
                    _webSocketClient.ConnectAsync(_authentication.Uri, CancellationToken.None).GetAwaiter().GetResult();
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
        public virtual async Task ReceiveAsync()
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
                if (!json.IsJson())
                    return;
                ParseOutput?.Invoke(json);
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
        public virtual async Task<string> RequestAsync(IRequest request)
        {
            //Limit the waiting time for a request
            if (!await _ioRequestSemaphoreSlim.WaitAsync(1000)) 
                return null;
            try
            {
                _authentication.ComputeSignature(request);
                request.Compose(_authentication.Signature);
                StringContent requestBody = null;
                if (!string.IsNullOrEmpty(request.Body))
                    requestBody = new StringContent(request.Body, Encoding.UTF8, request.ContentType);

                Console.WriteLine("{0} - {1}", DateTime.Now, request.AbsoluteUri);
                /**
                 *  int serverTimeDelay = await Endpoints.Misc.Others.TimeServerAsync(this);
                    if(serverTimeDelay > 0)
                        await Task.Delay(serverTimeDelay);
                 */
                using (HttpClient httpClient = request.ComposeDefaultRequestHeader(new HttpClient(), _authentication))
                {
                    HttpResponseMessage response = null;
                    switch (request.Method)
                    {
                        case "GET":
                            if (requestBody != null)
                                response = await httpClient.PostAsync(request.AbsoluteUri, requestBody);
                            else
                                response = await httpClient.GetAsync(request.AbsoluteUri);
                            break;
                        case "POST":
                            if (requestBody != null)
                                response = await httpClient.PostAsync(request.AbsoluteUri, requestBody);
                            break;
                        case "DELETE":
                            response = await httpClient.DeleteAsync(request.AbsoluteUri);
                            break;
                        default:
                            throw new NotImplementedException("The supplied HTTP method is not supported: " +
                                                              request.Method);
                    }
                    if (response != null)
                    {
                        string contentBody = await response.Content.ReadAsStringAsync();
                        HttpStatusCode statusCode = response.StatusCode;
                        bool isSuccess = response.IsSuccessStatusCode;
                        if (!isSuccess)
                        {
                            Log?.Invoke($"Exchange Request with not successful response: {request.Url} code:{statusCode}");
                            return null;
                        }
                        if (contentBody.IsJson())
                            return contentBody;
                        Log?.Invoke($"Exchange Request failed with invalid JSON response: {contentBody}");
                        return null;
                    }
                }
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
        #endregion
    }
}
