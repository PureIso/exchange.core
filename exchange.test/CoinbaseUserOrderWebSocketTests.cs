using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase.advancedtrade;
using exchange.coinbase.advancedtrade.authentication;
using exchange.coinbase.advancedtrade.websockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseUserOrderWebSocketTests
    {
        [TestMethod]
        public async Task RunAsync_UsesUserEndpointAndFreshJwtForEveryMessage()
        {
            RecordingConnection connection = new RecordingConnection();
            connection.QueueTextFrame("{}", true);
            SequenceJwtProvider jwtProvider = new SequenceJwtProvider();
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            CoinbaseAdvancedTradeUserOrderWebSocket client = CreateClient(
                jwtProvider,
                new RecordingConnectionFactory(connection));

            await client.RunAsync(
                new[] { " ETH-USD ", "BTC-USD", "BTC-USD" },
                (_, _) =>
                {
                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token);

            Assert.AreEqual(
                new Uri("wss://advanced-trade-ws-user.coinbase.com"),
                connection.ConnectedUri);
            Assert.HasCount(4, connection.SentMessages);
            AssertSubscription(
                connection.SentMessages[0],
                "subscribe",
                "user",
                "token-1",
                "BTC-USD",
                "ETH-USD");
            AssertSubscription(
                connection.SentMessages[1],
                "subscribe",
                "heartbeats",
                "token-2");
            AssertSubscription(
                connection.SentMessages[2],
                "unsubscribe",
                "user",
                "token-3",
                "BTC-USD",
                "ETH-USD");
            AssertSubscription(
                connection.SentMessages[3],
                "unsubscribe",
                "heartbeats",
                "token-4");
            Assert.AreEqual(4, jwtProvider.CreateCount);
            Assert.AreEqual(WebSocketState.Closed, connection.State);
        }

        [TestMethod]
        public async Task RunAsync_OmitsProductsToSubscribeToAllUserOrders()
        {
            RecordingConnection connection = new RecordingConnection();
            connection.QueueTextFrame("{}", true);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            CoinbaseAdvancedTradeUserOrderWebSocket client = CreateClient(
                new SequenceJwtProvider(),
                new RecordingConnectionFactory(connection));

            await client.RunAsync(
                null,
                (_, _) =>
                {
                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token);

            AssertSubscription(
                connection.SentMessages[0],
                "subscribe",
                "user",
                "token-1");
        }

        [TestMethod]
        public async Task RunAsync_GeneratesNewTokensAfterReconnect()
        {
            RecordingConnection first = new RecordingConnection();
            first.QueueCloseFrame();
            RecordingConnection second = new RecordingConnection();
            second.QueueTextFrame("{}", true);
            SequenceJwtProvider jwtProvider = new SequenceJwtProvider();
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            CoinbaseAdvancedTradeUserOrderWebSocket client = CreateClient(
                jwtProvider,
                new RecordingConnectionFactory(first, second));

            await client.RunAsync(
                new[] { "BTC-USD" },
                (_, _) =>
                {
                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token);

            Assert.AreEqual(6, jwtProvider.CreateCount);
            string[] tokens = first.SentMessages
                .Concat(second.SentMessages)
                .Select(ReadJwt)
                .ToArray();
            Assert.HasCount(6, tokens.Distinct(StringComparer.Ordinal).ToArray());
        }

        [TestMethod]
        public async Task RunMessagesAsync_AssemblesAndParsesFragmentedUserSnapshot()
        {
            const string json =
                "{\"channel\":\"user\",\"timestamp\":\"now\",\"sequence_num\":1,\"events\":[{\"type\":\"snapshot\",\"orders\":[{\"order_id\":\"order-1\",\"product_id\":\"BTC-USD\",\"status\":\"OPEN\",\"post_only\":\"false\"}]}]}";
            RecordingConnection connection = new RecordingConnection();
            connection.QueueTextFrame(json[..73], false);
            connection.QueueTextFrame(json[73..], true);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            CoinbaseAdvancedTradeUserOrderWebSocket client = CreateClient(
                new SequenceJwtProvider(),
                new RecordingConnectionFactory(connection));
            List<CoinbaseWebSocketParseStatus> statuses =
                new List<CoinbaseWebSocketParseStatus>();
            CoinbaseWebSocketMessage message = null;

            await client.RunMessagesAsync(
                new[] { "BTC-USD" },
                (result, _) =>
                {
                    statuses.Add(result.Status);
                    message = result.Message ?? message;
                    if (result.Status == CoinbaseWebSocketParseStatus.Parsed)
                        cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token);

            CollectionAssert.AreEqual(
                new[]
                {
                    CoinbaseWebSocketParseStatus.Incomplete,
                    CoinbaseWebSocketParseStatus.Parsed
                },
                statuses.ToArray());
            Assert.IsInstanceOfType<CoinbaseUserOrdersWebSocketMessage>(message);
        }

        [TestMethod]
        public async Task RunAsync_RedactsSignedPayloadWhenReconnectsAreExhausted()
        {
            const string secretToken = "SECRET-JWT-TOKEN";
            CoinbaseAdvancedTradeUserOrderWebSocket client = CreateClient(
                new FixedJwtProvider(secretToken),
                new RecordingConnectionFactory(
                    new ThrowingSendConnection(),
                    new ThrowingSendConnection()));

            CoinbaseWebSocketReconnectException exception =
                await Assert.ThrowsAsync<CoinbaseWebSocketReconnectException>(() =>
                    client.RunAsync(
                        new[] { "PRIVATE-PRODUCT" },
                        (_, _) => Task.CompletedTask,
                        CancellationToken.None));

            Assert.AreEqual(
                "Coinbase user-order WebSocket reconnect attempts were exhausted.",
                exception.Message);
            Assert.IsFalse(exception.ToString().Contains(secretToken, StringComparison.Ordinal));
            Assert.IsFalse(exception.ToString().Contains("PRIVATE-PRODUCT", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Constructor_RequiresSignerAndSecureEndpoint()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CoinbaseAdvancedTradeUserOrderWebSocket(null));
            Assert.Throws<ArgumentException>(() =>
                new CoinbaseAdvancedTradeUserOrderWebSocket(
                    new SequenceJwtProvider(),
                    new CoinbaseAdvancedTradeOptions
                    {
                        UserOrderWebSocketUri = new Uri("ws://localhost/user")
                    }));
        }

        private static CoinbaseAdvancedTradeUserOrderWebSocket CreateClient(
            ICoinbaseJwtProvider jwtProvider,
            ICoinbaseWebSocketConnectionFactory factory)
        {
            return new CoinbaseAdvancedTradeUserOrderWebSocket(
                jwtProvider,
                new CoinbaseAdvancedTradeOptions
                {
                    WebSocketReconnectDelays = new[] { TimeSpan.Zero },
                    WebSocketReconnectResetAfter = TimeSpan.FromMinutes(1),
                    WebSocketReceiveBufferSize = 256
                },
                new CoinbaseWebSocketDependencies(
                    factory,
                    new FixedClock(),
                    new ImmediateDelay()));
        }

        private static void AssertSubscription(
            string json,
            string expectedType,
            string expectedChannel,
            string expectedJwt,
            params string[] expectedProducts)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            Assert.AreEqual(expectedType, root.GetProperty("type").GetString());
            Assert.AreEqual(expectedChannel, root.GetProperty("channel").GetString());
            Assert.AreEqual(expectedJwt, root.GetProperty("jwt").GetString());
            if (expectedProducts.Length == 0)
            {
                Assert.IsFalse(root.TryGetProperty("product_ids", out _));
                return;
            }
            CollectionAssert.AreEqual(
                expectedProducts,
                root.GetProperty("product_ids")
                    .EnumerateArray()
                    .Select(value => value.GetString())
                    .ToArray());
        }

        private static string ReadJwt(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.GetProperty("jwt").GetString();
        }

        private sealed class SequenceJwtProvider : ICoinbaseJwtProvider
        {
            public int CreateCount { get; private set; }

            public string CreateRestToken(System.Net.Http.HttpMethod method, Uri requestUri)
            {
                throw new NotSupportedException();
            }

            public string CreateWebSocketToken()
            {
                CreateCount++;
                return $"token-{CreateCount}";
            }
        }

        private sealed class FixedJwtProvider : ICoinbaseJwtProvider
        {
            private readonly string _token;

            public FixedJwtProvider(string token)
            {
                _token = token;
            }

            public string CreateRestToken(System.Net.Http.HttpMethod method, Uri requestUri)
            {
                throw new NotSupportedException();
            }

            public string CreateWebSocketToken()
            {
                return _token;
            }
        }

        private sealed class FixedClock : ICoinbaseClock
        {
            public DateTimeOffset UtcNow { get; } =
                new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
        }

        private sealed class ImmediateDelay : ICoinbaseDelay
        {
            public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }
        }

        private sealed class RecordingConnectionFactory : ICoinbaseWebSocketConnectionFactory
        {
            private readonly Queue<ICoinbaseWebSocketConnection> _connections;

            public RecordingConnectionFactory(params ICoinbaseWebSocketConnection[] connections)
            {
                _connections = new Queue<ICoinbaseWebSocketConnection>(connections);
            }

            public ICoinbaseWebSocketConnection Create()
            {
                return _connections.Dequeue();
            }
        }

        private class RecordingConnection : ICoinbaseWebSocketConnection
        {
            private readonly Queue<Func<ArraySegment<byte>, WebSocketReceiveResult>> _receives =
                new Queue<Func<ArraySegment<byte>, WebSocketReceiveResult>>();

            public WebSocketState State { get; protected set; } = WebSocketState.None;

            public Uri ConnectedUri { get; private set; }

            public List<string> SentMessages { get; } = new List<string>();

            public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ConnectedUri = uri;
                State = WebSocketState.Open;
                return Task.CompletedTask;
            }

            public virtual Task SendAsync(
                ArraySegment<byte> payload,
                WebSocketMessageType messageType,
                bool endOfMessage,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SentMessages.Add(Encoding.UTF8.GetString(payload.Array, payload.Offset, payload.Count));
                return Task.CompletedTask;
            }

            public Task<WebSocketReceiveResult> ReceiveAsync(
                ArraySegment<byte> buffer,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(_receives.Dequeue()(buffer));
            }

            public Task CloseOutputAsync(
                WebSocketCloseStatus closeStatus,
                string statusDescription,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                State = WebSocketState.Closed;
                return Task.CompletedTask;
            }

            public void Abort()
            {
                State = WebSocketState.Aborted;
            }

            public void Dispose()
            {
            }

            public void QueueTextFrame(string json, bool endOfMessage)
            {
                _receives.Enqueue(buffer =>
                {
                    byte[] payload = Encoding.UTF8.GetBytes(json);
                    Buffer.BlockCopy(payload, 0, buffer.Array, buffer.Offset, payload.Length);
                    return new WebSocketReceiveResult(
                        payload.Length,
                        WebSocketMessageType.Text,
                        endOfMessage);
                });
            }

            public void QueueCloseFrame()
            {
                _receives.Enqueue(_ =>
                {
                    State = WebSocketState.CloseReceived;
                    return new WebSocketReceiveResult(
                        0,
                        WebSocketMessageType.Close,
                        true,
                        WebSocketCloseStatus.NormalClosure,
                        "server close");
                });
            }
        }

        private sealed class ThrowingSendConnection : RecordingConnection
        {
            public override Task SendAsync(
                ArraySegment<byte> payload,
                WebSocketMessageType messageType,
                bool endOfMessage,
                CancellationToken cancellationToken)
            {
                string signedPayload = Encoding.UTF8.GetString(
                    payload.Array,
                    payload.Offset,
                    payload.Count);
                throw new InvalidOperationException($"Transport rejected {signedPayload}");
            }
        }
    }
}
