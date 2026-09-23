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
    public class CoinbaseMarketDataWebSocketTests
    {
        [TestMethod]
        public async Task RunAsync_SubscribesTickerAndHeartbeatsThenClosesCleanly()
        {
            RecordingConnection connection = new RecordingConnection();
            connection.QueueTextFrame("{\"channel\":\"ticker\"}", false);
            RecordingConnectionFactory factory = new RecordingConnectionFactory(connection);
            RecordingDelay delay = new RecordingDelay();
            MutableClock clock = new MutableClock();
            CoinbaseAdvancedTradeMarketDataWebSocket client = CreateClient(factory, delay, clock);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            CoinbaseWebSocketFrame receivedFrame = null;

            await client.RunAsync(
                new[] { " ETH-USD ", "BTC-USD", "BTC-USD", " " },
                (frame, _) =>
                {
                    receivedFrame = frame;
                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token);

            Assert.AreEqual(new Uri("wss://advanced-trade-ws.coinbase.com"), connection.ConnectedUri);
            Assert.IsNotNull(receivedFrame);
            Assert.AreEqual("{\"channel\":\"ticker\"}", Encoding.UTF8.GetString(receivedFrame.Payload));
            Assert.IsFalse(receivedFrame.EndOfMessage);
            Assert.AreEqual(WebSocketMessageType.Text, receivedFrame.MessageType);
            Assert.HasCount(4, connection.SentMessages);
            AssertSubscription(
                connection.SentMessages[0],
                "subscribe",
                "ticker",
                "BTC-USD",
                "ETH-USD");
            AssertSubscription(connection.SentMessages[1], "subscribe", "heartbeats");
            AssertSubscription(
                connection.SentMessages[2],
                "unsubscribe",
                "ticker",
                "BTC-USD",
                "ETH-USD");
            AssertSubscription(connection.SentMessages[3], "unsubscribe", "heartbeats");
            Assert.AreEqual(WebSocketState.Closed, connection.State);
            Assert.IsTrue(connection.Disposed);
            Assert.IsEmpty(delay.Delays);
        }

        [TestMethod]
        public async Task RunAsync_DefaultChannelsUseSeparateSubscriptionsPlusHeartbeats()
        {
            RecordingConnection connection = new RecordingConnection();
            connection.QueueTextFrame("{}", true);
            RecordingConnectionFactory factory = new RecordingConnectionFactory(connection);
            CoinbaseAdvancedTradeOptions options = new CoinbaseAdvancedTradeOptions
            {
                WebSocketReceiveBufferSize = 256
            };
            CoinbaseAdvancedTradeMarketDataWebSocket client =
                new CoinbaseAdvancedTradeMarketDataWebSocket(
                    options,
                    new CoinbaseWebSocketDependencies(
                        factory,
                        new MutableClock(),
                        new RecordingDelay()));
            using CancellationTokenSource cancellation = new CancellationTokenSource();

            await client.RunAsync(
                new[] { "BTC-USD" },
                (_, _) =>
                {
                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token);

            Assert.HasCount(8, connection.SentMessages);
            AssertSubscription(connection.SentMessages[0], "subscribe", "ticker", "BTC-USD");
            AssertSubscription(
                connection.SentMessages[1],
                "subscribe",
                "market_trades",
                "BTC-USD");
            AssertSubscription(connection.SentMessages[2], "subscribe", "level2", "BTC-USD");
            AssertSubscription(connection.SentMessages[3], "subscribe", "heartbeats");
            AssertSubscription(connection.SentMessages[4], "unsubscribe", "ticker", "BTC-USD");
            AssertSubscription(
                connection.SentMessages[5],
                "unsubscribe",
                "market_trades",
                "BTC-USD");
            AssertSubscription(connection.SentMessages[6], "unsubscribe", "level2", "BTC-USD");
            AssertSubscription(connection.SentMessages[7], "unsubscribe", "heartbeats");
        }

        [TestMethod]
        public async Task RunAsync_ReconnectsAfterServerCloseUsingConfiguredDelay()
        {
            RecordingConnection first = new RecordingConnection();
            first.QueueCloseFrame();
            RecordingConnection second = new RecordingConnection();
            second.QueueTextFrame("{}", true);
            RecordingConnectionFactory factory = new RecordingConnectionFactory(first, second);
            RecordingDelay delay = new RecordingDelay();
            MutableClock clock = new MutableClock();
            CoinbaseAdvancedTradeMarketDataWebSocket client = CreateClient(
                factory,
                delay,
                clock,
                TimeSpan.FromMilliseconds(25));
            using CancellationTokenSource cancellation = new CancellationTokenSource();

            await client.RunAsync(
                new[] { "BTC-USD" },
                (_, _) =>
                {
                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token);

            CollectionAssert.AreEqual(
                new[] { TimeSpan.FromMilliseconds(25) },
                delay.Delays.ToArray());
            Assert.AreEqual(2, factory.CreateCount);
            Assert.IsFalse(first.Aborted);
            Assert.AreEqual(WebSocketState.Closed, first.State);
            Assert.IsTrue(first.Disposed);
            Assert.IsTrue(second.Disposed);
        }

        [TestMethod]
        public async Task RunAsync_ThrowsAfterBoundedReconnectAttemptsAreExhausted()
        {
            RecordingConnectionFactory factory = new RecordingConnectionFactory(
                ClosedConnection(),
                ClosedConnection(),
                ClosedConnection());
            RecordingDelay delay = new RecordingDelay();
            CoinbaseAdvancedTradeMarketDataWebSocket client = CreateClient(
                factory,
                delay,
                new MutableClock(),
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2));

            CoinbaseWebSocketReconnectException exception =
                await Assert.ThrowsAsync<CoinbaseWebSocketReconnectException>(() =>
                    client.RunAsync(
                        new[] { "BTC-USD" },
                        (_, _) => Task.CompletedTask,
                        CancellationToken.None));

            StringAssert.Contains(exception.Message, "attempts were exhausted");
            CollectionAssert.AreEqual(
                new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2) },
                delay.Delays.ToArray());
            Assert.AreEqual(3, factory.CreateCount);
        }

        [TestMethod]
        public async Task RunAsync_DoesNotExposeTransportOrFutureJwtFieldsOnExhaustion()
        {
            CoinbaseAdvancedTradeOptions options = new CoinbaseAdvancedTradeOptions
            {
                WebSocketReconnectDelays = new[] { TimeSpan.Zero },
                WebSocketReceiveBufferSize = 256,
                WebSocketMarketDataChannels = new[] { "ticker" }
            };
            CoinbaseAdvancedTradeMarketDataWebSocket client =
                new CoinbaseAdvancedTradeMarketDataWebSocket(
                    options,
                    new CoinbaseWebSocketDependencies(
                        new ThrowingConnectionFactory(),
                        new MutableClock(),
                        new RecordingDelay()));

            CoinbaseWebSocketReconnectException exception =
                await Assert.ThrowsAsync<CoinbaseWebSocketReconnectException>(() =>
                    client.RunAsync(
                        new[] { "BTC-USD" },
                        (_, _) => Task.CompletedTask,
                        CancellationToken.None));

            Assert.IsFalse(exception.ToString().Contains("secret-token", StringComparison.Ordinal));
            Assert.IsFalse(exception.ToString().Contains("product_ids", StringComparison.Ordinal));
            Assert.IsNull(exception.InnerException);
        }

        [TestMethod]
        public async Task RunAsync_RejectsMissingProductsAndHandler()
        {
            CoinbaseAdvancedTradeMarketDataWebSocket client = CreateClient(
                new RecordingConnectionFactory(),
                new RecordingDelay(),
                new MutableClock());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                client.RunAsync(
                    new[] { "", " " },
                    (_, _) => Task.CompletedTask,
                    CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                client.RunAsync(
                    new[] { "BTC-USD" },
                    null,
                    CancellationToken.None));
        }

        [TestMethod]
        public void Constructor_RejectsInsecureEndpointAndInvalidReconnectConfiguration()
        {
            Assert.Throws<ArgumentException>(() =>
                new CoinbaseAdvancedTradeMarketDataWebSocket(
                    new CoinbaseAdvancedTradeOptions
                    {
                        MarketDataWebSocketUri = new Uri("ws://advanced-trade-ws.coinbase.com")
                    }));
            Assert.Throws<ArgumentException>(() =>
                new CoinbaseAdvancedTradeMarketDataWebSocket(
                    new CoinbaseAdvancedTradeOptions
                    {
                        WebSocketReconnectDelays = Array.Empty<TimeSpan>()
                    }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CoinbaseAdvancedTradeMarketDataWebSocket(
                    new CoinbaseAdvancedTradeOptions
                    {
                        WebSocketReceiveBufferSize = 0
                    }));
        }

        [TestMethod]
        public async Task RunMessagesAsync_SurfacesMalformedMessageAndContinuesReceiving()
        {
            RecordingConnection connection = new RecordingConnection();
            connection.QueueTextFrame("{not-json", true);
            connection.QueueTextFrame(
                """
                {"channel":"heartbeats","timestamp":"now","sequence_num":1,"events":[{"heartbeat_counter":7}]}
                """,
                true);
            RecordingConnectionFactory factory = new RecordingConnectionFactory(connection);
            CoinbaseAdvancedTradeMarketDataWebSocket client = CreateClient(
                factory,
                new RecordingDelay(),
                new MutableClock());
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            List<CoinbaseWebSocketParseStatus> statuses =
                new List<CoinbaseWebSocketParseStatus>();

            await client.RunMessagesAsync(
                new[] { "BTC-USD" },
                (result, _) =>
                {
                    statuses.Add(result.Status);
                    if (result.Status == CoinbaseWebSocketParseStatus.Parsed)
                        cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token);

            CollectionAssert.AreEqual(
                new[]
                {
                    CoinbaseWebSocketParseStatus.Malformed,
                    CoinbaseWebSocketParseStatus.Parsed
                },
                statuses.ToArray());
        }

        [TestMethod]
        public async Task RunMessagesAsync_AssemblesFragmentsBeforeDispatchingParsedMessage()
        {
            const string json =
                "{\"channel\":\"heartbeats\",\"timestamp\":\"now\",\"sequence_num\":1," +
                "\"events\":[{\"heartbeat_counter\":7}]}";
            RecordingConnection connection = new RecordingConnection();
            connection.QueueTextFrame(json.Substring(0, 37), false);
            connection.QueueTextFrame(json.Substring(37), true);
            CoinbaseAdvancedTradeMarketDataWebSocket client = CreateClient(
                new RecordingConnectionFactory(connection),
                new RecordingDelay(),
                new MutableClock());
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            List<CoinbaseWebSocketParseStatus> statuses =
                new List<CoinbaseWebSocketParseStatus>();

            await client.RunMessagesAsync(
                new[] { "BTC-USD" },
                (result, _) =>
                {
                    statuses.Add(result.Status);
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
        }

        [TestMethod]
        public async Task RunMessagesAsync_SurfacesUnknownEventAndContinuesReceiving()
        {
            RecordingConnection connection = new RecordingConnection();
            connection.QueueTextFrame(
                """
                {"channel":"ticker","timestamp":"now","sequence_num":1,"events":[{"type":"future_event","tickers":[]}]}
                """,
                true);
            connection.QueueTextFrame(
                """
                {"channel":"heartbeats","timestamp":"now","sequence_num":2,"events":[{"heartbeat_counter":8}]}
                """,
                true);
            CoinbaseAdvancedTradeMarketDataWebSocket client = CreateClient(
                new RecordingConnectionFactory(connection),
                new RecordingDelay(),
                new MutableClock());
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            List<CoinbaseWebSocketParseStatus> statuses =
                new List<CoinbaseWebSocketParseStatus>();

            await client.RunMessagesAsync(
                new[] { "BTC-USD" },
                (result, _) =>
                {
                    statuses.Add(result.Status);
                    if (result.Status == CoinbaseWebSocketParseStatus.Parsed)
                        cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token);

            CollectionAssert.AreEqual(
                new[]
                {
                    CoinbaseWebSocketParseStatus.Unknown,
                    CoinbaseWebSocketParseStatus.Parsed
                },
                statuses.ToArray());
        }

        [TestMethod]
        public async Task RunMessagesAsync_ReconnectsToObtainSnapshotAfterSequenceGap()
        {
            RecordingConnection first = new RecordingConnection();
            first.QueueTextFrame(Ticker("snapshot", 1), true);
            first.QueueTextFrame(Ticker("update", 3), true);
            RecordingConnection second = new RecordingConnection();
            second.QueueTextFrame(Ticker("snapshot", 50), true);
            RecordingConnectionFactory factory = new RecordingConnectionFactory(first, second);
            RecordingDelay delay = new RecordingDelay();
            CoinbaseAdvancedTradeMarketDataWebSocket client = CreateClient(
                factory,
                delay,
                new MutableClock(),
                TimeSpan.Zero);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            List<CoinbaseWebSocketParseStatus> statuses =
                new List<CoinbaseWebSocketParseStatus>();

            await client.RunMessagesAsync(
                new[] { "BTC-USD" },
                (result, _) =>
                {
                    statuses.Add(result.Status);
                    if (statuses.Count == 3)
                        cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token);

            CollectionAssert.AreEqual(
                new[]
                {
                    CoinbaseWebSocketParseStatus.Parsed,
                    CoinbaseWebSocketParseStatus.ResynchronizationRequired,
                    CoinbaseWebSocketParseStatus.Parsed
                },
                statuses.ToArray());
            Assert.AreEqual(2, factory.CreateCount);
            CollectionAssert.AreEqual(new[] { TimeSpan.Zero }, delay.Delays.ToArray());
        }

        [TestMethod]
        public async Task RunAsync_ResetsBackoffAfterStableConnection()
        {
            RecordingConnection first = ClosedConnection();
            RecordingConnection second = ClosedConnection();
            RecordingConnection third = new RecordingConnection();
            third.QueueTextFrame("{}", true);
            RecordingConnectionFactory factory = new RecordingConnectionFactory(first, second, third);
            RecordingDelay delay = new RecordingDelay();
            MutableClock clock = new MutableClock();
            second.BeforeReceive = () => clock.UtcNow += TimeSpan.FromMinutes(2);
            CoinbaseAdvancedTradeMarketDataWebSocket client = CreateClient(
                factory,
                delay,
                clock,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5));
            using CancellationTokenSource cancellation = new CancellationTokenSource();

            await client.RunAsync(
                new[] { "BTC-USD" },
                (_, _) =>
                {
                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token);

            CollectionAssert.AreEqual(
                new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1) },
                delay.Delays.ToArray());
        }

        [TestMethod]
        public async Task RunAsync_ReturnsCleanlyWhenReconnectDelayIsCancelled()
        {
            RecordingConnection first = ClosedConnection();
            RecordingConnectionFactory factory = new RecordingConnectionFactory(first);
            RecordingDelay delay = new RecordingDelay();
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            delay.AfterWait = _ => cancellation.Cancel();
            CoinbaseAdvancedTradeMarketDataWebSocket client = CreateClient(
                factory,
                delay,
                new MutableClock(),
                TimeSpan.FromSeconds(5));

            await client.RunAsync(
                new[] { "BTC-USD" },
                (_, _) => Task.CompletedTask,
                cancellation.Token);

            Assert.AreEqual(1, factory.CreateCount);
            CollectionAssert.AreEqual(
                new[] { TimeSpan.FromSeconds(5) },
                delay.Delays.ToArray());
        }

        private static CoinbaseAdvancedTradeMarketDataWebSocket CreateClient(
            RecordingConnectionFactory factory,
            RecordingDelay delay,
            MutableClock clock,
            params TimeSpan[] reconnectDelays)
        {
            CoinbaseAdvancedTradeOptions options = new CoinbaseAdvancedTradeOptions
            {
                WebSocketReconnectDelays = reconnectDelays.Length == 0
                    ? new[] { TimeSpan.Zero }
                    : reconnectDelays,
                WebSocketReconnectResetAfter = TimeSpan.FromMinutes(1),
                WebSocketReceiveBufferSize = 256,
                WebSocketMarketDataChannels = new[] { "ticker" }
            };
            return new CoinbaseAdvancedTradeMarketDataWebSocket(
                options,
                new CoinbaseWebSocketDependencies(factory, clock, delay));
        }

        private static string Ticker(string eventType, long sequence)
        {
            return $$"""
                {"channel":"ticker","timestamp":"now","sequence_num":{{sequence}},"events":[{"type":"{{eventType}}","tickers":[{"type":"ticker","product_id":"BTC-USD","price":"1"}]}]}
                """;
        }

        private static RecordingConnection ClosedConnection()
        {
            RecordingConnection connection = new RecordingConnection();
            connection.QueueCloseFrame();
            return connection;
        }

        private static void AssertSubscription(
            string json,
            string expectedType,
            string expectedChannel,
            params string[] expectedProducts)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            Assert.AreEqual(expectedType, root.GetProperty("type").GetString());
            Assert.AreEqual(expectedChannel, root.GetProperty("channel").GetString());
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

        private sealed class MutableClock : ICoinbaseClock
        {
            public DateTimeOffset UtcNow { get; set; } =
                new DateTimeOffset(2026, 8, 15, 10, 0, 0, TimeSpan.Zero);
        }

        private sealed class RecordingDelay : ICoinbaseDelay
        {
            public List<TimeSpan> Delays { get; } = new List<TimeSpan>();

            public Action<TimeSpan> AfterWait { get; set; }

            public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Delays.Add(delay);
                AfterWait?.Invoke(delay);
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }
        }

        private sealed class RecordingConnectionFactory : ICoinbaseWebSocketConnectionFactory
        {
            private readonly Queue<RecordingConnection> _connections;

            public RecordingConnectionFactory(params RecordingConnection[] connections)
            {
                _connections = new Queue<RecordingConnection>(connections);
            }

            public int CreateCount { get; private set; }

            public ICoinbaseWebSocketConnection Create()
            {
                CreateCount++;
                if (_connections.Count == 0)
                    throw new InvalidOperationException("No recorded connection is available.");
                return _connections.Dequeue();
            }
        }

        private sealed class ThrowingConnectionFactory : ICoinbaseWebSocketConnectionFactory
        {
            public ICoinbaseWebSocketConnection Create()
            {
                throw new InvalidOperationException(
                    "jwt=secret-token; product_ids=[BTC-USD]");
            }
        }

        private sealed class RecordingConnection : ICoinbaseWebSocketConnection
        {
            private readonly Queue<Func<ArraySegment<byte>, WebSocketReceiveResult>> _receives =
                new Queue<Func<ArraySegment<byte>, WebSocketReceiveResult>>();

            public WebSocketState State { get; private set; } = WebSocketState.None;

            public Uri ConnectedUri { get; private set; }

            public List<string> SentMessages { get; } = new List<string>();

            public bool Aborted { get; private set; }

            public bool Disposed { get; private set; }

            public Action BeforeReceive { get; set; }

            public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ConnectedUri = uri;
                State = WebSocketState.Open;
                return Task.CompletedTask;
            }

            public Task SendAsync(
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
                BeforeReceive?.Invoke();
                if (_receives.Count == 0)
                    return Task.Delay(Timeout.Infinite, cancellationToken)
                        .ContinueWith<WebSocketReceiveResult>(
                            _ => throw new InvalidOperationException("The receive wait ended unexpectedly."),
                            CancellationToken.None,
                            TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default);
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
                Aborted = true;
                State = WebSocketState.Aborted;
            }

            public void Dispose()
            {
                Disposed = true;
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
    }
}
