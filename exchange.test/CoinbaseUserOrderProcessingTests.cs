using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase.advancedtrade;
using exchange.coinbase.advancedtrade.websockets;
using exchange.core.models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseUserOrderProcessingTests
    {
        [TestMethod]
        public void Parser_ParsesSanitizedSnapshotAndStringBoolean()
        {
            CoinbaseWebSocketParseResult result = Parse(
                UserMessage("snapshot", 1,
                    """
                    {"order_id":"order-1","product_id":"BTC-USD","status":"OPEN","order_side":"BUY","order_type":"LIMIT","post_only":"true","cumulative_quantity":"0.25","leaves_quantity":"0.75","limit_price":"65000","filled_value":"16250","total_fees":"3.25","time_in_force":"GOOD_UNTIL_CANCELLED","creation_time":"2026-08-15T10:00:00Z"}
                    """));

            CoinbaseUserOrdersWebSocketMessage message =
                Assert.IsInstanceOfType<CoinbaseUserOrdersWebSocketMessage>(result.Message);
            CoinbaseUserOrderWebSocketUpdate order = message.Events.Single().Orders.Single();
            Assert.AreEqual("snapshot", message.Events[0].Type);
            Assert.AreEqual("order-1", order.OrderId);
            Assert.AreEqual("BTC-USD", order.ProductId);
            Assert.IsTrue(order.PostOnly);
            Assert.AreEqual("0.25", order.CumulativeQuantity);
        }

        [TestMethod]
        public void Parser_HandlesUnknownRateLimitAndMalformedUserFixturesSafely()
        {
            CoinbaseWebSocketMessageParser parser = new CoinbaseWebSocketMessageParser();
            CoinbaseWebSocketParseResult rateLimit = parser.Parse(
                """
                {"type":"error","message":"rate limit exceeded","jwt":"SANITIZED"}
                """);
            CoinbaseWebSocketParseResult future = parser.Parse(
                """
                {"channel":"user","timestamp":"now","sequence_num":2,"events":[{"type":"future_type","orders":[]}]}
                """);
            CoinbaseWebSocketParseResult malformed = parser.Parse(
                """
                {"channel":"user","timestamp":"now","sequence_num":3,"events":[{"type":"update","orders":[{"order_id":"secret-order","status":"OPEN","post_only":"not-bool"}]}]}
                """);

            Assert.AreEqual(CoinbaseWebSocketParseStatus.Unknown, rateLimit.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Unknown, future.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Malformed, malformed.Status);
            Assert.IsFalse(malformed.Error.Contains("secret-order", StringComparison.Ordinal));
            Assert.IsFalse(malformed.Error.Contains("not-bool", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Mapper_NormalizesUserOrderAndTerminalStatus()
        {
            CoinbaseUserOrdersWebSocketMessage message =
                Assert.IsInstanceOfType<CoinbaseUserOrdersWebSocketMessage>(Parse(
                    UserMessage("update", 2,
                        """
                        {"order_id":"order-2","product_id":"ETH-USD","status":"FILLED","order_side":"SELL","order_type":"MARKET","post_only":false,"cumulative_quantity":"1.5","leaves_quantity":"0","limit_price":"0","avg_price":"4200.25","filled_value":"6300.375","total_fees":"6.3","time_in_force":"IMMEDIATE_OR_CANCEL","creation_time":"2026-08-15T10:01:00Z"}
                        """)).Message);

            CoinbaseUserOrderUpdate update =
                AdvancedTradeWebSocketMapper.MapUserOrders(message).Single();

            Assert.AreEqual("update", update.EventType);
            Assert.AreEqual("order-2", update.Order.ID);
            Assert.AreEqual("sell", update.Order.Side);
            Assert.AreEqual("market", update.Order.Type);
            Assert.AreEqual("1.5", update.Order.Size);
            Assert.AreEqual(1.5m, update.Order.Quantity);
            Assert.AreEqual("4200.25", update.Order.Price);
            Assert.AreEqual("1.5", update.Order.FilledSize);
            Assert.AreEqual("6300.375", update.Order.ExecutedValue);
            Assert.AreEqual("filled", update.Order.Status);
            Assert.IsTrue(update.Order.Settled);
        }

        [TestMethod]
        public void Mapper_RejectsInvalidNumbersWithoutEchoingPayload()
        {
            const string secret = "SECRET-NUMERIC-PAYLOAD";
            CoinbaseUserOrdersWebSocketMessage message =
                Assert.IsInstanceOfType<CoinbaseUserOrdersWebSocketMessage>(Parse(
                    UserMessage("update", 3,
                        $$"""
                        {"order_id":"order-3","product_id":"BTC-USD","status":"OPEN","post_only":false,"cumulative_quantity":"{{secret}}","leaves_quantity":"1"}
                        """)).Message);

            InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
                AdvancedTradeWebSocketMapper.MapUserOrders(message));

            Assert.IsFalse(exception.ToString().Contains(secret, StringComparison.Ordinal));
        }

        [TestMethod]
        public void Processor_DetectsUserOrderSequenceGapAndRecoversOnSnapshot()
        {
            Guid connectionId = Guid.NewGuid();
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(4096);

            CoinbaseWebSocketParseResult snapshot = processor.Process(Frame(
                connectionId,
                UserMessage(
                    "snapshot",
                    10,
                    BasicOrder("order-a", "OPEN") + "," +
                    BasicOrder("order-b", "OPEN").Replace("BTC-USD", "ETH-USD"))));
            CoinbaseWebSocketParseResult sparseBitcoinUpdate = processor.Process(Frame(
                connectionId,
                UserMessage("update", 11, BasicOrder("order-a", "OPEN"))));
            CoinbaseWebSocketParseResult sparseEthereumUpdate = processor.Process(Frame(
                connectionId,
                UserMessage(
                    "update",
                    12,
                    BasicOrder("order-b", "OPEN").Replace("BTC-USD", "ETH-USD"))));
            CoinbaseWebSocketParseResult gap = processor.Process(Frame(
                connectionId,
                UserMessage("update", 14, BasicOrder("order-a", "FILLED"))));
            CoinbaseWebSocketParseResult recovery = processor.Process(Frame(
                connectionId,
                UserMessage("snapshot", 20, BasicOrder("order-a", "OPEN"))));

            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, snapshot.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, sparseBitcoinUpdate.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, sparseEthereumUpdate.Status);
            Assert.AreEqual(
                CoinbaseWebSocketParseStatus.ResynchronizationRequired,
                gap.Status);
            CollectionAssert.AreEqual(
                new[] { "__user_orders__" },
                gap.AffectedProductIds.ToArray());
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, recovery.Status);
        }

        [TestMethod]
        public async Task Plugin_MergesSnapshotAndPatchAndAwaitsOrderNotifications()
        {
            ControlledMarketDataWebSocket market = new ControlledMarketDataWebSocket();
            ControlledUserOrderWebSocket user = new ControlledUserOrderWebSocket();
            using HttpClient httpClient = new HttpClient(new NoopHandler());
            using CoinbaseAdvancedTrade plugin = new CoinbaseAdvancedTrade(
                new CoinbaseAdvancedTradeClient(httpClient),
                null,
                market,
                user);
            List<List<Order>> notifications = new List<List<Order>>();
            List<string> logs = new List<string>();
            plugin.NotifyOrders = (_, orders) =>
            {
                notifications.Add(orders.Select(Clone).ToList());
                return Task.CompletedTask;
            };
            plugin.ProcessLogBroadcast = (_, _, message) => logs.Add(message);

            await plugin.ChangeFeed(new List<Product> { new Product { ID = "BTC-USD" } });
            await Task.WhenAll(market.Started.Task, user.Started.Task);
            await user.EmitAsync(Parse(UserMessage(
                "snapshot",
                1,
                BasicOrder("order-a", "OPEN") + "," + BasicOrder("order-b", "OPEN"))));
            await user.EmitAsync(CoinbaseWebSocketParseResult.Malformed(
                "jwt=SECRET; order_id=secret-order"));
            await user.EmitAsync(Parse(UserMessage(
                "patch",
                2,
                BasicOrder("order-a", "FILLED"))));
            await plugin.CloseFeed();

            Assert.HasCount(2, notifications);
            Assert.HasCount(2, notifications[0]);
            Assert.HasCount(2, plugin.Orders);
            Assert.AreEqual("filled", plugin.Orders.Single(value => value.ID == "order-a").Status);
            Assert.IsTrue(plugin.Orders.Single(value => value.ID == "order-a").Settled);
            Assert.AreEqual("open", plugin.Orders.Single(value => value.ID == "order-b").Status);
            CollectionAssert.AreEqual(new[] { "BTC-USD" }, user.ProductIds);
            Assert.IsTrue(user.CancellationObserved);
            Assert.IsTrue(market.CancellationObserved);
            Assert.HasCount(1, logs);
            Assert.IsFalse(logs[0].Contains("SECRET", StringComparison.Ordinal));
            Assert.IsFalse(logs[0].Contains("secret-order", StringComparison.Ordinal));
        }

        private static CoinbaseWebSocketParseResult Parse(string json)
        {
            CoinbaseWebSocketParseResult result = new CoinbaseWebSocketMessageParser().Parse(json);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, result.Status);
            return result;
        }

        private static string UserMessage(string type, long sequence, string orders)
        {
            return $$"""
                {"channel":"user","timestamp":"2026-08-15T10:00:00Z","sequence_num":{{sequence}},"events":[{"type":"{{type}}","orders":[{{orders}}]}]}
                """;
        }

        private static string BasicOrder(string id, string status)
        {
            return $$"""
                {"order_id":"{{id}}","product_id":"BTC-USD","status":"{{status}}","order_side":"BUY","order_type":"LIMIT","post_only":"false","cumulative_quantity":"0.5","leaves_quantity":"0.5","limit_price":"65000","filled_value":"32500","total_fees":"2","time_in_force":"GOOD_UNTIL_CANCELLED","creation_time":"2026-08-15T10:00:00Z"}
                """;
        }

        private static CoinbaseWebSocketFrame Frame(Guid connectionId, string json)
        {
            return new CoinbaseWebSocketFrame(
                connectionId,
                System.Text.Encoding.UTF8.GetBytes(json),
                System.Net.WebSockets.WebSocketMessageType.Text,
                true);
        }

        private static Order Clone(Order source)
        {
            return new Order { ID = source.ID, Status = source.Status, Settled = source.Settled };
        }

        private sealed class ControlledMarketDataWebSocket : ICoinbaseMarketDataWebSocket
        {
            public TaskCompletionSource<bool> Started { get; } =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool CancellationObserved { get; private set; }

            public async Task RunMessagesAsync(
                IEnumerable<string> productIds,
                Func<CoinbaseWebSocketParseResult, CancellationToken, Task> messageHandler,
                CancellationToken cancellationToken)
            {
                Started.SetResult(true);
                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    CancellationObserved = true;
                }
            }
        }

        private sealed class ControlledUserOrderWebSocket : ICoinbaseUserOrderWebSocket
        {
            private Func<CoinbaseWebSocketParseResult, CancellationToken, Task> _handler;
            private CancellationToken _cancellationToken;

            public TaskCompletionSource<bool> Started { get; } =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public string[] ProductIds { get; private set; }

            public bool CancellationObserved { get; private set; }

            public async Task RunMessagesAsync(
                IEnumerable<string> productIds,
                Func<CoinbaseWebSocketParseResult, CancellationToken, Task> messageHandler,
                CancellationToken cancellationToken)
            {
                ProductIds = productIds.ToArray();
                _handler = messageHandler;
                _cancellationToken = cancellationToken;
                Started.SetResult(true);
                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    CancellationObserved = true;
                }
            }

            public Task RunAsync(
                IEnumerable<string> productIds,
                Func<CoinbaseWebSocketFrame, CancellationToken, Task> frameHandler,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task EmitAsync(CoinbaseWebSocketParseResult result)
            {
                return _handler(result, _cancellationToken);
            }
        }

        private sealed class NoopHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
            }
        }
    }
}
