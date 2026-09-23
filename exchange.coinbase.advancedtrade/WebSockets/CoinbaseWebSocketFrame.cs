using System;
using System.Net.WebSockets;

namespace exchange.coinbase.advancedtrade.websockets
{
    public sealed class CoinbaseWebSocketFrame
    {
        public CoinbaseWebSocketFrame(
            Guid connectionId,
            byte[] payload,
            WebSocketMessageType messageType,
            bool endOfMessage)
        {
            if (connectionId == Guid.Empty)
                throw new ArgumentException("A WebSocket connection ID is required.", nameof(connectionId));
            ConnectionId = connectionId;
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            MessageType = messageType;
            EndOfMessage = endOfMessage;
        }

        public Guid ConnectionId { get; }

        public byte[] Payload { get; }

        public WebSocketMessageType MessageType { get; }

        public bool EndOfMessage { get; }
    }
}
