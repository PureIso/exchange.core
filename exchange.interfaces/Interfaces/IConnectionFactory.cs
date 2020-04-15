using exchange.core.models;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace exchange.core.Interfaces
{
    public interface IConnectionFactory
    {
        Authentication Authentication { get; set; }
        HttpClient HttpClient { get; set; } 
        ClientWebSocket ClientWebSocket { get; set; }

        Task<string> WebSocketSendAsync(string message); 
        Task<string> WebSocketReceiveAsync(); 
        Task<bool> WebSocketCloseAsync();
        Task<string> RequestAsync(IRequest request);

        bool IsWebSocketConnected();
    }
}
