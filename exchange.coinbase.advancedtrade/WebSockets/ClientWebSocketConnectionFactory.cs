using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.coinbase.advancedtrade.websockets
{
    public sealed class ClientWebSocketConnectionFactory : ICoinbaseWebSocketConnectionFactory
    {
        private readonly Func<ICoinbaseWebSocketConnection> _connectionFactory;

        public static ClientWebSocketConnectionFactory Instance { get; } =
            new ClientWebSocketConnectionFactory();

        public ClientWebSocketConnectionFactory()
            : this(() => new ClientWebSocketConnection())
        {
        }

        public ClientWebSocketConnectionFactory(
            Func<ICoinbaseWebSocketConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory
                ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public ICoinbaseWebSocketConnection Create()
        {
            return _connectionFactory()
                ?? throw new InvalidOperationException("The Coinbase WebSocket factory returned no connection.");
        }

        private sealed class ClientWebSocketConnection : ICoinbaseWebSocketConnection
        {
            private readonly ClientWebSocket _socket = new ClientWebSocket();

            public WebSocketState State => _socket.State;

            public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
            {
                return _socket.ConnectAsync(uri, cancellationToken);
            }

            public Task SendAsync(
                ArraySegment<byte> payload,
                WebSocketMessageType messageType,
                bool endOfMessage,
                CancellationToken cancellationToken)
            {
                return _socket.SendAsync(payload, messageType, endOfMessage, cancellationToken);
            }

            public Task<WebSocketReceiveResult> ReceiveAsync(
                ArraySegment<byte> buffer,
                CancellationToken cancellationToken)
            {
                return _socket.ReceiveAsync(buffer, cancellationToken);
            }

            public Task CloseOutputAsync(
                WebSocketCloseStatus closeStatus,
                string statusDescription,
                CancellationToken cancellationToken)
            {
                return _socket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
            }

            public void Abort()
            {
                _socket.Abort();
            }

            public void Dispose()
            {
                _socket.Dispose();
            }
        }
    }
}
