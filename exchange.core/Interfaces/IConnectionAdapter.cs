using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using exchange.core.Enums;
using exchange.core.implementations;

namespace exchange.core.interfaces
{
    public interface IConnectionAdapter
    {
        Action<MessageType, string> ProcessLogBroadcast { get; set; }

        Authentication Authentication { get; set; }
        HttpClient HttpClient { get; set; }
        ClientWebSocket ClientWebSocket { get; set; }

        Task ConnectAsync(string uriString);
        Task<string> WebSocketSendAsync(string message);
        Task<string> WebSocketReceiveAsync();
        Task<bool> WebSocketCloseAsync();
        Task<string> RequestAsync(IRequest request);
        Task<string> RequestUnsignedAsync(IRequest request);

        bool IsWebSocketConnected();
        void Dispose();
    }
}