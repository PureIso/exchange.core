using System;
using System.IO;

namespace exchange.coinbase.advancedtrade.websockets
{
    public sealed class CoinbaseWebSocketMessageProcessor : IDisposable
    {
        private readonly CoinbaseWebSocketMessageAssembler _assembler;
        private readonly CoinbaseWebSocketMessageParser _parser;
        private readonly CoinbaseWebSocketSequenceTracker _sequenceTracker =
            new CoinbaseWebSocketSequenceTracker();
        private Guid? _connectionId;

        public CoinbaseWebSocketMessageProcessor(
            int maximumMessageSize,
            CoinbaseWebSocketMessageParser parser = null)
        {
            _assembler = new CoinbaseWebSocketMessageAssembler(maximumMessageSize);
            _parser = parser ?? new CoinbaseWebSocketMessageParser();
        }

        public CoinbaseWebSocketParseResult Process(CoinbaseWebSocketFrame frame)
        {
            try
            {
                if (_connectionId != frame.ConnectionId)
                {
                    _sequenceTracker.Reset();
                    _connectionId = frame.ConnectionId;
                }
                string message = _assembler.Append(frame);
                CoinbaseWebSocketParseResult result = message == null
                    ? CoinbaseWebSocketParseResult.Incomplete()
                    : _parser.Parse(message);
                return result.Status == CoinbaseWebSocketParseStatus.Parsed
                    ? _sequenceTracker.Evaluate(result.Message)
                    : result;
            }
            catch (InvalidDataException exception)
            {
                return CoinbaseWebSocketParseResult.Malformed(exception.Message);
            }
        }

        public void Dispose()
        {
            _assembler.Dispose();
        }
    }
}
