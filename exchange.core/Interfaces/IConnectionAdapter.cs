using System.Net.Http;
using System.Net.WebSockets;
using exchange.core.models;
using System.Threading.Tasks;
using exchange.core.implementations;

namespace exchange.core.Interfaces
{
    public interface IConnectionAdapter
    {
        Authentication Authentication { get; set; } 
        HttpClient HttpClient { get; }
        ClientWebSocket ClientWebSocket { get; }

        Task ConnectAsync(string uriString, ClientWebSocket ClientWebSocket);
        Task<string> WebSocketSendAsync(string message, ClientWebSocket ClientWebSocket); 
        Task<string> WebSocketReceiveAsync(ClientWebSocket ClientWebSocket); 
        Task<bool> WebSocketCloseAsync(ClientWebSocket ClientWebSocket);
        Task<string> RequestAsync(IRequest request);
        Task<string> RequestUnsignedAsync(IRequest request);

        bool IsWebSocketConnected(ClientWebSocket ClientWebSocket);
        void Dispose();
        bool Validate();
    }
}
