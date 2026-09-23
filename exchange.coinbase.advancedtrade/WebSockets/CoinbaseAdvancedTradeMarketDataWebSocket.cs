using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.coinbase.advancedtrade.websockets
{
    public sealed class CoinbaseAdvancedTradeMarketDataWebSocket : ICoinbaseMarketDataWebSocket
    {
        private const string HeartbeatsChannel = "heartbeats";
        private readonly CoinbaseWebSocketDependencies _dependencies;
        private readonly Uri _endpoint;
        private readonly TimeSpan[] _reconnectDelays;
        private readonly TimeSpan _reconnectResetAfter;
        private readonly int _receiveBufferSize;
        private readonly int _maximumMessageSize;
        private readonly string[] _marketDataChannels;

        public CoinbaseAdvancedTradeMarketDataWebSocket(
            CoinbaseAdvancedTradeOptions options = null,
            CoinbaseWebSocketDependencies dependencies = null)
        {
            options ??= new CoinbaseAdvancedTradeOptions();
            options.ValidateWebSocket();
            _dependencies = dependencies ?? new CoinbaseWebSocketDependencies();
            _endpoint = options.MarketDataWebSocketUri;
            _reconnectDelays = options.WebSocketReconnectDelays.ToArray();
            _reconnectResetAfter = options.WebSocketReconnectResetAfter;
            _receiveBufferSize = options.WebSocketReceiveBufferSize;
            _maximumMessageSize = options.WebSocketMaximumMessageSize;
            _marketDataChannels = options.WebSocketMarketDataChannels.ToArray();
        }

        public async Task RunMessagesAsync(
            IEnumerable<string> productIds,
            Func<CoinbaseWebSocketParseResult, CancellationToken, Task> messageHandler,
            CancellationToken cancellationToken)
        {
            if (messageHandler == null)
                throw new ArgumentNullException(nameof(messageHandler));
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(_maximumMessageSize);
            await RunAsync(
                productIds,
                async (frame, token) =>
                {
                    CoinbaseWebSocketParseResult result = processor.Process(frame);
                    await messageHandler(result, token);
                    if (result.Status ==
                        CoinbaseWebSocketParseStatus.ResynchronizationRequired)
                        throw new CoinbaseWebSocketResynchronizationException();
                },
                cancellationToken);
        }

        public async Task RunAsync(
            IEnumerable<string> productIds,
            Func<CoinbaseWebSocketFrame, CancellationToken, Task> frameHandler,
            CancellationToken cancellationToken)
        {
            string[] products = NormalizeProducts(productIds);
            if (frameHandler == null)
                throw new ArgumentNullException(nameof(frameHandler));

            int reconnectIndex = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                DateTimeOffset? connectedAt = null;
                try
                {
                    await RunConnectionAsync(
                        products,
                        frameHandler,
                        () => connectedAt = _dependencies.Clock.UtcNow,
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception)
                {
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (connectedAt.HasValue &&
                    _dependencies.Clock.UtcNow - connectedAt.Value >= _reconnectResetAfter)
                    reconnectIndex = 0;

                if (reconnectIndex >= _reconnectDelays.Length)
                    throw new CoinbaseWebSocketReconnectException(
                        "Coinbase market-data WebSocket reconnect attempts were exhausted.");

                try
                {
                    await _dependencies.Delay.WaitAsync(
                        _reconnectDelays[reconnectIndex],
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                reconnectIndex++;
            }
        }

        private async Task RunConnectionAsync(
            string[] productIds,
            Func<CoinbaseWebSocketFrame, CancellationToken, Task> frameHandler,
            Action onConnected,
            CancellationToken cancellationToken)
        {
            using ICoinbaseWebSocketConnection connection = _dependencies.ConnectionFactory.Create();
            Guid connectionId = Guid.NewGuid();
            await connection.ConnectAsync(_endpoint, cancellationToken);
            onConnected();
            foreach (string channel in _marketDataChannels)
                await SendSubscriptionAsync(
                    connection,
                    "subscribe",
                    channel,
                    productIds,
                    cancellationToken);
            await SendSubscriptionAsync(
                connection,
                "subscribe",
                HeartbeatsChannel,
                null,
                cancellationToken);

            byte[] buffer = new byte[_receiveBufferSize];
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await connection.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                        return;

                    byte[] payload = new byte[result.Count];
                    Buffer.BlockCopy(buffer, 0, payload, 0, result.Count);
                    await frameHandler(
                        new CoinbaseWebSocketFrame(
                            connectionId,
                            payload,
                            result.MessageType,
                            result.EndOfMessage),
                        cancellationToken);
                }
            }
            finally
            {
                await CloseGracefullyAsync(connection, productIds);
            }
        }

        private async Task CloseGracefullyAsync(
            ICoinbaseWebSocketConnection connection,
            string[] productIds)
        {
            if (connection.State == WebSocketState.Open)
            {
                foreach (string channel in _marketDataChannels)
                    await SendSubscriptionAsync(
                        connection,
                        "unsubscribe",
                        channel,
                        productIds,
                        CancellationToken.None);
                await SendSubscriptionAsync(
                    connection,
                    "unsubscribe",
                    HeartbeatsChannel,
                    null,
                    CancellationToken.None);
                await connection.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Feed stopped.",
                    CancellationToken.None);
            }
            else if (connection.State == WebSocketState.CloseReceived)
            {
                await connection.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Server closed the feed.",
                    CancellationToken.None);
            }
            else if (connection.State != WebSocketState.Closed)
            {
                connection.Abort();
            }
        }

        private static Task SendSubscriptionAsync(
            ICoinbaseWebSocketConnection connection,
            string type,
            string channel,
            string[] productIds,
            CancellationToken cancellationToken)
        {
            string json = JsonSerializer.Serialize(new SubscriptionMessage
            {
                Type = type,
                ProductIds = productIds,
                Channel = channel
            });
            byte[] payload = Encoding.UTF8.GetBytes(json);
            return connection.SendAsync(
                new ArraySegment<byte>(payload),
                WebSocketMessageType.Text,
                true,
                cancellationToken);
        }

        private static string[] NormalizeProducts(IEnumerable<string> productIds)
        {
            if (productIds == null)
                throw new ArgumentNullException(nameof(productIds));
            string[] products = productIds
                .Where(productId => !string.IsNullOrWhiteSpace(productId))
                .Select(productId => productId.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(productId => productId, StringComparer.Ordinal)
                .ToArray();
            if (products.Length == 0)
                throw new ArgumentException(
                    "At least one Coinbase product ID is required.",
                    nameof(productIds));
            return products;
        }

        private sealed class SubscriptionMessage
        {
            [JsonPropertyName("type")]
            public string Type { get; init; }

            [JsonPropertyName("product_ids")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string[] ProductIds { get; init; }

            [JsonPropertyName("channel")]
            public string Channel { get; init; }
        }
    }
}
