using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.coinbase.advancedtrade.websockets
{
    public interface ICoinbaseWebSocketConnection : IDisposable
    {
        WebSocketState State { get; }

        Task ConnectAsync(Uri uri, CancellationToken cancellationToken);

        Task SendAsync(
            ArraySegment<byte> payload,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken);

        Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken);

        Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken);

        void Abort();
    }
}
