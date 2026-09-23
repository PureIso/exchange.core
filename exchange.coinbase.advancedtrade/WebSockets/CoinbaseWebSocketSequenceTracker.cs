using System;
using System.Collections.Generic;
using System.Linq;

namespace exchange.coinbase.advancedtrade.websockets
{
    public sealed class CoinbaseWebSocketSequenceTracker
    {
        private const string UserOrdersStreamKey = "__user_orders__";
        private readonly Dictionary<(string Channel, string ProductId), long> _sequences =
            new Dictionary<(string Channel, string ProductId), long>();
        private readonly HashSet<(string Channel, string ProductId)> _desynchronized =
            new HashSet<(string Channel, string ProductId)>();

        public CoinbaseWebSocketParseResult Evaluate(CoinbaseWebSocketMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            SequenceObservation[] observations = GetObservations(message)
                .GroupBy(value => value.ProductId, StringComparer.Ordinal)
                .Select(group => new SequenceObservation(
                    group.Key,
                    group.Any(value => value.IsSnapshot)))
                .ToArray();
            if (observations.Length == 0)
                return CoinbaseWebSocketParseResult.Parsed(message);

            string[] gaps = observations
                .Where(value => IsGap(message, value))
                .Select(value => value.ProductId)
                .ToArray();
            if (gaps.Length > 0)
            {
                foreach (string productId in gaps)
                    _desynchronized.Add((message.Channel, productId));
                return CoinbaseWebSocketParseResult.ResynchronizationRequired(
                    message.Channel,
                    gaps);
            }

            string[] stale = observations
                .Where(value => IsOutOfOrder(message, value))
                .Select(value => value.ProductId)
                .ToArray();
            if (stale.Length > 0)
                return CoinbaseWebSocketParseResult.OutOfOrder(message.Channel, stale);

            foreach (SequenceObservation observation in observations)
            {
                _sequences[(message.Channel, observation.ProductId)] = message.SequenceNumber;
                if (observation.IsSnapshot)
                    _desynchronized.Remove((message.Channel, observation.ProductId));
            }
            return CoinbaseWebSocketParseResult.Parsed(message);
        }

        public void Reset()
        {
            _sequences.Clear();
            _desynchronized.Clear();
        }

        private bool IsGap(CoinbaseWebSocketMessage message, SequenceObservation observation)
        {
            if (observation.IsSnapshot)
                return false;
            if (_desynchronized.Contains((message.Channel, observation.ProductId)))
                return true;
            if (
                !_sequences.TryGetValue((message.Channel, observation.ProductId), out long previous))
                return false;
            return message.SequenceNumber > previous && message.SequenceNumber - previous > 1;
        }

        private bool IsOutOfOrder(
            CoinbaseWebSocketMessage message,
            SequenceObservation observation)
        {
            return !observation.IsSnapshot &&
                _sequences.TryGetValue(
                    (message.Channel, observation.ProductId),
                    out long previous) &&
                message.SequenceNumber <= previous;
        }

        private static IEnumerable<SequenceObservation> GetObservations(
            CoinbaseWebSocketMessage message)
        {
            return message switch
            {
                CoinbaseTickerWebSocketMessage ticker => ticker.Events.Select(value =>
                    new SequenceObservation(
                        value.ProductId,
                        value.EventType == "snapshot")),
                CoinbaseMarketTradesWebSocketMessage trades => trades.Events.Select(value =>
                    new SequenceObservation(
                        value.ProductId,
                        value.Type == "snapshot")),
                CoinbaseLevel2WebSocketMessage level2 => level2.Events.Select(value =>
                    new SequenceObservation(
                        value.ProductId,
                        value.Type == "snapshot")),
                CoinbaseUserOrdersWebSocketMessage user => new[]
                {
                    new SequenceObservation(
                        UserOrdersStreamKey,
                        user.Events.Any(value => value.Type == "snapshot"))
                },
                _ => Array.Empty<SequenceObservation>()
            };
        }

        private sealed class SequenceObservation
        {
            public SequenceObservation(string productId, bool isSnapshot)
            {
                ProductId = productId;
                IsSnapshot = isSnapshot;
            }

            public string ProductId { get; }

            public bool IsSnapshot { get; }
        }
    }
}
