using System.Net.Http;
using System.Net.WebSockets;
using exchange.core.models;
using System.Threading.Tasks;

namespace exchange.core.Interfaces
{
    public interface IConnectionAdapter
    {
        Authentication Authentication { get; set; } 
        HttpClient HttpClient { get; }
        ClientWebSocket ClientWebSocket { get; }

        Task<string> WebSocketSendAsync(string message); 
        Task<string> WebSocketReceiveAsync(); 
        Task<bool> WebSocketCloseAsync();
        Task<string> RequestAsync(IRequest request);
        Task<string> RequestUnsignedAsync(IRequest request);

        bool IsWebSocketConnected();
        void Dispose();
        bool Validate();
    }
}
