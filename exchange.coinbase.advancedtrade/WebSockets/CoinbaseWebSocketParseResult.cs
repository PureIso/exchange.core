using System.Collections.Generic;

namespace exchange.coinbase.advancedtrade.websockets
{
    public enum CoinbaseWebSocketParseStatus
    {
        Incomplete,
        Parsed,
        Unknown,
        Malformed,
        OutOfOrder,
        ResynchronizationRequired
    }

    public sealed class CoinbaseWebSocketParseResult
    {
        private CoinbaseWebSocketParseResult(
            CoinbaseWebSocketParseStatus status,
            CoinbaseWebSocketMessage message,
            string channel,
            string error,
            IReadOnlyList<string> affectedProductIds = null)
        {
            Status = status;
            Message = message;
            Channel = channel;
            Error = error;
            AffectedProductIds = affectedProductIds ?? new string[0];
        }

        public CoinbaseWebSocketParseStatus Status { get; }

        public CoinbaseWebSocketMessage Message { get; }

        public string Channel { get; }

        public string Error { get; }

        public IReadOnlyList<string> AffectedProductIds { get; }

        public static CoinbaseWebSocketParseResult Incomplete()
        {
            return new CoinbaseWebSocketParseResult(
                CoinbaseWebSocketParseStatus.Incomplete,
                null,
                null,
                null);
        }

        public static CoinbaseWebSocketParseResult Parsed(CoinbaseWebSocketMessage message)
        {
            return new CoinbaseWebSocketParseResult(
                CoinbaseWebSocketParseStatus.Parsed,
                message,
                message.Channel,
                null);
        }

        public static CoinbaseWebSocketParseResult Unknown(string channel)
        {
            return new CoinbaseWebSocketParseResult(
                CoinbaseWebSocketParseStatus.Unknown,
                null,
                channel,
                null);
        }

        public static CoinbaseWebSocketParseResult Malformed(string error)
        {
            return new CoinbaseWebSocketParseResult(
                CoinbaseWebSocketParseStatus.Malformed,
                null,
                null,
                error);
        }

        public static CoinbaseWebSocketParseResult OutOfOrder(
            string channel,
            IReadOnlyList<string> affectedProductIds)
        {
            return new CoinbaseWebSocketParseResult(
                CoinbaseWebSocketParseStatus.OutOfOrder,
                null,
                channel,
                null,
                affectedProductIds);
        }

        public static CoinbaseWebSocketParseResult ResynchronizationRequired(
            string channel,
            IReadOnlyList<string> affectedProductIds)
        {
            return new CoinbaseWebSocketParseResult(
                CoinbaseWebSocketParseStatus.ResynchronizationRequired,
                null,
                channel,
                null,
                affectedProductIds);
        }
    }
}
