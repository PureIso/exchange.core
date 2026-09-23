using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using exchange.coinbase.advancedtrade;
using exchange.coinbase.advancedtrade.websockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseWebSocketParsingTests
    {
        [TestMethod]
        public void Process_AssemblesFragmentedTickerFixture()
        {
            const string json =
                """
                {
                  "channel": "ticker",
                  "timestamp": "2026-08-15T10:00:00.123456789Z",
                  "sequence_num": 42,
                  "events": [{
                    "type": "snapshot",
                    "tickers": [{
                      "type": "ticker",
                      "product_id": "BTC-USD",
                      "price": "65000.25",
                      "volume_24_h": "12.5",
                      "low_24_h": "62000",
                      "high_24_h": "66000",
                      "price_percent_chg_24_h": "1.25",
                      "best_bid": "65000",
                      "best_bid_quantity": "0.4",
                      "best_ask": "65001",
                      "best_ask_quantity": "0.3"
                    }]
                  }]
                }
                """;
            Guid connectionId = Guid.NewGuid();
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(4096);

            CoinbaseWebSocketParseResult first = processor.Process(
                Frame(connectionId, bytes.Take(37).ToArray(), false));
            CoinbaseWebSocketParseResult second = processor.Process(
                Frame(connectionId, bytes.Skip(37).ToArray(), true));

            Assert.AreEqual(CoinbaseWebSocketParseStatus.Incomplete, first.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, second.Status);
            CoinbaseTickerWebSocketMessage message =
                Assert.IsInstanceOfType<CoinbaseTickerWebSocketMessage>(second.Message);
            Assert.AreEqual(42, message.SequenceNumber);
            Assert.AreEqual("2026-08-15T10:00:00.123456789Z", message.Timestamp);
            Assert.HasCount(1, message.Events);
            Assert.AreEqual("BTC-USD", message.Events[0].ProductId);
            Assert.AreEqual("65000.25", message.Events[0].Price);
            Assert.AreEqual("65000", message.Events[0].BestBid);
            Assert.AreEqual("65001", message.Events[0].BestAsk);
        }

        [TestMethod]
        public void Process_ParsesMarketTradesFixture()
        {
            const string json =
                """
                {
                  "channel": "market_trades",
                  "timestamp": "2026-08-15T10:00:01Z",
                  "sequence_num": 43,
                  "events": [{
                    "type": "update",
                    "trades": [{
                      "trade_id": "0198b6cf-58a8-76f0-9a7c-2b1ef39fdb74",
                      "product_id": "ETH-USD",
                      "price": "4200.10",
                      "size": "0.25",
                      "side": "BUY",
                      "time": "2026-08-15T10:00:00.900Z"
                    }]
                  }]
                }
                """;
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(4096);

            CoinbaseWebSocketParseResult result = processor.Process(TextFrame(json));

            CoinbaseMarketTradesWebSocketMessage message =
                Assert.IsInstanceOfType<CoinbaseMarketTradesWebSocketMessage>(result.Message);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, result.Status);
            Assert.AreEqual("0198b6cf-58a8-76f0-9a7c-2b1ef39fdb74", message.Events[0].TradeId);
            Assert.AreEqual("update", message.Events[0].Type);
            Assert.AreEqual("BUY", message.Events[0].Side);
        }

        [TestMethod]
        public void Process_ParsesLevel2Fixture()
        {
            const string json =
                """
                {
                  "channel": "l2_data",
                  "timestamp": "2026-08-15T10:00:02Z",
                  "sequence_num": 44,
                  "events": [{
                    "type": "snapshot",
                    "product_id": "BTC-USD",
                    "updates": [{
                      "side": "bid",
                      "event_time": "2026-08-15T10:00:01Z",
                      "price_level": "64999.50",
                      "new_quantity": "0.75"
                    }]
                  }]
                }
                """;
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(4096);

            CoinbaseWebSocketParseResult result = processor.Process(TextFrame(json));

            CoinbaseLevel2WebSocketMessage message =
                Assert.IsInstanceOfType<CoinbaseLevel2WebSocketMessage>(result.Message);
            Assert.AreEqual("BTC-USD", message.Events[0].ProductId);
            Assert.AreEqual("bid", message.Events[0].Updates[0].Side);
            Assert.AreEqual("64999.50", message.Events[0].Updates[0].PriceLevel);
            Assert.AreEqual("0.75", message.Events[0].Updates[0].NewQuantity);
        }

        [TestMethod]
        public void Process_ParsesNumericAndStringHeartbeatCounters()
        {
            const string json =
                """
                {
                  "channel": "heartbeats",
                  "timestamp": "2026-08-15T10:00:03Z",
                  "sequence_num": 45,
                  "events": [
                    { "current_time": "now", "heartbeat_counter": 3049 },
                    { "current_time": "later", "heartbeat_counter": "3050" }
                  ]
                }
                """;
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(4096);

            CoinbaseWebSocketParseResult result = processor.Process(TextFrame(json));

            CoinbaseHeartbeatWebSocketMessage message =
                Assert.IsInstanceOfType<CoinbaseHeartbeatWebSocketMessage>(result.Message);
            CollectionAssert.AreEqual(
                new[] { "3049", "3050" },
                message.Events.Select(value => value.HeartbeatCounter).ToArray());
        }

        [TestMethod]
        public void Process_IgnoresUnknownChannelAndFutureEventType()
        {
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(4096);
            CoinbaseWebSocketParseResult channel = processor.Process(TextFrame(
                """
                {"channel":"future_channel","timestamp":"now","sequence_num":1,"events":[]}
                """));
            CoinbaseWebSocketParseResult eventType = processor.Process(TextFrame(
                """
                {"channel":"ticker","timestamp":"now","sequence_num":2,"events":[{"type":"future_event","tickers":[]}]}
                """));
            CoinbaseWebSocketParseResult protocolMessage = processor.Process(TextFrame(
                """
                {"type":"subscriptions","channels":[]}
                """));

            Assert.AreEqual(CoinbaseWebSocketParseStatus.Unknown, channel.Status);
            Assert.AreEqual("future_channel", channel.Channel);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Unknown, eventType.Status);
            Assert.AreEqual("ticker", eventType.Channel);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Unknown, protocolMessage.Status);
        }

        [TestMethod]
        public void Process_SurfacesMalformedPayloadAndContinuesWithNextMessage()
        {
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(4096);

            CoinbaseWebSocketParseResult malformed = processor.Process(TextFrame("{not-json"));
            CoinbaseWebSocketParseResult incompleteEnvelope = processor.Process(TextFrame(
                """
                {"channel":"ticker","timestamp":"now","events":[]}
                """));
            CoinbaseWebSocketParseResult next = processor.Process(TextFrame(
                """
                {"channel":"heartbeats","timestamp":"now","sequence_num":3,"events":[{"heartbeat_counter":1}]}
                """));

            Assert.AreEqual(CoinbaseWebSocketParseStatus.Malformed, malformed.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Malformed, incompleteEnvelope.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, next.Status);
            Assert.IsFalse(malformed.Error.Contains("{not-json", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Process_RejectsBinaryOversizedAndInvalidUtf8MessagesThenResets()
        {
            Guid connectionId = Guid.NewGuid();
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(128);

            CoinbaseWebSocketParseResult binary = processor.Process(
                new CoinbaseWebSocketFrame(
                    connectionId,
                    new byte[] { 1, 2 },
                    WebSocketMessageType.Binary,
                    true));
            CoinbaseWebSocketParseResult oversized = processor.Process(
                Frame(connectionId, Enumerable.Repeat((byte)'x', 129).ToArray(), true));
            CoinbaseWebSocketParseResult invalidUtf8 = processor.Process(
                Frame(connectionId, new byte[] { 0xc3, 0x28 }, true));
            CoinbaseWebSocketParseResult next = processor.Process(TextFrame(
                """
                {"type":"subscriptions"}
                """,
                connectionId));

            Assert.AreEqual(CoinbaseWebSocketParseStatus.Malformed, binary.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Malformed, oversized.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Malformed, invalidUtf8.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Unknown, next.Status);
        }

        [TestMethod]
        public void Process_DiscardsPartialMessageWhenConnectionChanges()
        {
            const string complete =
                """
                {"channel":"heartbeats","timestamp":"now","sequence_num":5,"events":[{"heartbeat_counter":9}]}
                """;
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(4096);

            CoinbaseWebSocketParseResult partial = processor.Process(
                Frame(Guid.NewGuid(), Encoding.UTF8.GetBytes("{\"channel\":\"ticker\""), false));
            CoinbaseWebSocketParseResult nextConnection = processor.Process(
                TextFrame(complete, Guid.NewGuid()));

            Assert.AreEqual(CoinbaseWebSocketParseStatus.Incomplete, partial.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, nextConnection.Status);
        }

        [TestMethod]
        public void Process_DetectsPerProductGapAndRequiresSnapshotBeforeRecovery()
        {
            Guid connectionId = Guid.NewGuid();
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(4096);

            CoinbaseWebSocketParseResult snapshot = processor.Process(TextFrame(
                Ticker("snapshot", 10),
                connectionId));
            CoinbaseWebSocketParseResult gap = processor.Process(TextFrame(
                Ticker("update", 12),
                connectionId));
            CoinbaseWebSocketParseResult stillMissingSnapshot = processor.Process(TextFrame(
                Ticker("update", 13),
                connectionId));
            CoinbaseWebSocketParseResult recovered = processor.Process(TextFrame(
                Ticker("snapshot", 20),
                connectionId));
            CoinbaseWebSocketParseResult next = processor.Process(TextFrame(
                Ticker("update", 21),
                connectionId));

            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, snapshot.Status);
            Assert.AreEqual(
                CoinbaseWebSocketParseStatus.ResynchronizationRequired,
                gap.Status);
            CollectionAssert.AreEqual(
                new[] { "BTC-USD" },
                gap.AffectedProductIds.ToArray());
            Assert.AreEqual(
                CoinbaseWebSocketParseStatus.ResynchronizationRequired,
                stillMissingSnapshot.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, recovered.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, next.Status);
        }

        [TestMethod]
        public void Process_IgnoresDuplicateOrOutOfOrderSequenceAndResetsForNewConnection()
        {
            Guid connectionId = Guid.NewGuid();
            using CoinbaseWebSocketMessageProcessor processor =
                new CoinbaseWebSocketMessageProcessor(4096);

            processor.Process(TextFrame(Ticker("snapshot", 10), connectionId));
            CoinbaseWebSocketParseResult stale = processor.Process(TextFrame(
                Ticker("update", 9),
                connectionId));
            CoinbaseWebSocketParseResult newConnection = processor.Process(TextFrame(
                Ticker("update", 50),
                Guid.NewGuid()));

            Assert.AreEqual(CoinbaseWebSocketParseStatus.OutOfOrder, stale.Status);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, newConnection.Status);
        }

        [TestMethod]
        public void Options_RejectMaximumMessageSizeBelowReceiveBuffer()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CoinbaseAdvancedTradeMarketDataWebSocket(
                    new CoinbaseAdvancedTradeOptions
                    {
                        WebSocketReceiveBufferSize = 1024,
                        WebSocketMaximumMessageSize = 512
                    }));
        }

        private static CoinbaseWebSocketFrame TextFrame(string json, Guid? connectionId = null)
        {
            return Frame(
                connectionId ?? Guid.NewGuid(),
                Encoding.UTF8.GetBytes(json),
                true);
        }

        private static string Ticker(string eventType, long sequence)
        {
            return $$"""
                {"channel":"ticker","timestamp":"now","sequence_num":{{sequence}},"events":[{"type":"{{eventType}}","tickers":[{"type":"ticker","product_id":"BTC-USD","price":"1"}]}]}
                """;
        }

        private static CoinbaseWebSocketFrame Frame(
            Guid connectionId,
            byte[] payload,
            bool endOfMessage)
        {
            return new CoinbaseWebSocketFrame(
                connectionId,
                payload,
                WebSocketMessageType.Text,
                endOfMessage);
        }
    }
}
