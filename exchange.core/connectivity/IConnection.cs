using System;
using System.Threading.Tasks;

namespace exchange.core.connectivity
{
    public interface IConnection
    {
        event Action<string> ParseOutput;
        event Action<string> Log;

        void Initialise(IAuthentication authentication);
        bool IsConnected();
        void Connect();
        bool Disconnect();

        Task SendAsync(string message);
        Task ReceiveAsync();
        Task<string> RequestAsync(IRequest request);
    }
}
