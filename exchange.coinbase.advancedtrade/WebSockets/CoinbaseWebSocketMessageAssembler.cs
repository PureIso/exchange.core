using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;

namespace exchange.coinbase.advancedtrade.websockets
{
    public sealed class CoinbaseWebSocketMessageAssembler : IDisposable
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private readonly int _maximumMessageSize;
        private readonly MemoryStream _buffer = new MemoryStream();
        private Guid _connectionId;

        public CoinbaseWebSocketMessageAssembler(int maximumMessageSize)
        {
            if (maximumMessageSize <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(maximumMessageSize),
                    "The maximum WebSocket message size must be positive.");
            _maximumMessageSize = maximumMessageSize;
        }

        public string Append(CoinbaseWebSocketFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            if (frame.MessageType != WebSocketMessageType.Text)
            {
                Reset();
                throw new InvalidDataException("Only Coinbase text WebSocket frames are supported.");
            }
            if (_connectionId != frame.ConnectionId)
            {
                Reset();
                _connectionId = frame.ConnectionId;
            }
            if (_buffer.Length + frame.Payload.Length > _maximumMessageSize)
            {
                Reset();
                throw new InvalidDataException("The Coinbase WebSocket message exceeded the configured limit.");
            }

            _buffer.Write(frame.Payload, 0, frame.Payload.Length);
            if (!frame.EndOfMessage)
                return null;

            byte[] payload = _buffer.ToArray();
            ResetBuffer();
            try
            {
                return StrictUtf8.GetString(payload);
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException("The Coinbase WebSocket message was not valid UTF-8.", exception);
            }
        }

        public void Reset()
        {
            _connectionId = Guid.Empty;
            ResetBuffer();
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }

        private void ResetBuffer()
        {
            _buffer.SetLength(0);
            _buffer.Position = 0;
        }
    }
}
