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
using exchange.core.enums;
using exchange.core.models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseAdvancedTradeWebSocketMappingTests
    {
        [TestMethod]
        public void MapTicker_PreservesCoreFeedTickerAndAssetContract()
        {
            CoinbaseTickerWebSocketMessage message = new CoinbaseTickerWebSocketMessage(
                "ticker",
                "2026-08-15T10:00:00Z",
                12,
                new[]
                {
                    new CoinbaseTickerWebSocketEvent
                    {
                        Type = "ticker",
                        ProductId = "BTC-USD",
                        Price = "105",
                        Volume24Hour = "20",
                        PricePercentageChange24Hour = "5",
                        BestBid = "104.5",
                        BestAsk = "105.5"
                    }
                });

            CoinbaseTickerUpdate update = AdvancedTradeWebSocketMapper.MapTicker(message).Single();

            Assert.AreEqual("BTC-USD", update.Ticker.ProductID);
            Assert.AreEqual("105", update.Ticker.Price);
            Assert.AreEqual("104.5", update.Ticker.Bid);
            Assert.AreEqual("105.5", update.Ticker.Ask);
            Assert.AreEqual(12, update.Feed.Sequence);
            Assert.AreEqual(105m, update.AssetInformation.CurrentPrice);
            Assert.AreEqual(5m, update.AssetInformation.TwentyFourHourPriceChange);
            Assert.AreEqual(5m, update.AssetInformation.TwentyFourHourPricePercentageChange);
        }

        [TestMethod]
        public void ApplyLevel2_AppliesSnapshotAndAbsoluteQuantityUpdatesInPriceOrder()
        {
            CoinbaseLevel2WebSocketEvent snapshot = new CoinbaseLevel2WebSocketEvent
            {
                Type = "snapshot",
                ProductId = "BTC-USD",
                Updates = new[]
                {
                    Level("bid", "100", "2"),
                    Level("bid", "101", "1"),
                    Level("offer", "103", "4"),
                    Level("offer", "102", "3")
                }
            };
            OrderBook initial = AdvancedTradeWebSocketMapper.ApplyLevel2(null, snapshot, 5);
            CoinbaseLevel2WebSocketEvent update = new CoinbaseLevel2WebSocketEvent
            {
                Type = "update",
                ProductId = "BTC-USD",
                Updates = new[]
                {
                    Level("bid", "101", "0"),
                    Level("bid", "99", "7"),
                    Level("offer", "102", "8")
                }
            };

            OrderBook result = AdvancedTradeWebSocketMapper.ApplyLevel2(initial, update, 6);

            Assert.AreEqual(6, result.Sequence);
            Assert.AreEqual("100", result.Bids[0][0]);
            Assert.AreEqual("99", result.Bids[1][0]);
            Assert.AreEqual("102", result.Asks[0][0]);
            Assert.AreEqual("8", result.Asks[0][1]);
            Assert.AreEqual("103", result.Asks[1][0]);
        }

        [TestMethod]
        public void MapMarketTrades_PreservesUuidAndRejectsInvalidNumbers()
        {
            CoinbaseMarketTradesWebSocketMessage valid = new CoinbaseMarketTradesWebSocketMessage(
                "market_trades",
                "2026-08-15T10:00:00Z",
                8,
                new[]
                {
                    new CoinbaseMarketTradeWebSocketEvent
                    {
                        Type = "update",
                        TradeId = "0198b6cf-58a8-76f0-9a7c-2b1ef39fdb74",
                        ProductId = "ETH-USD",
                        Price = "4200.10",
                        Size = "0.25",
                        Side = "BUY",
                        Time = "2026-08-15T10:00:00Z"
                    }
                });

            CoinbaseTradeUpdate update = AdvancedTradeWebSocketMapper.MapMarketTrades(valid).Single();

            Assert.AreEqual("0198b6cf-58a8-76f0-9a7c-2b1ef39fdb74", update.Trade.TradeId);
            Assert.AreEqual("BUY", update.Feed.Side);
            Assert.AreEqual("0.25", update.Feed.LastSize);

            CoinbaseMarketTradesWebSocketMessage invalid = new CoinbaseMarketTradesWebSocketMessage(
                "market_trades",
                "now",
                9,
                new[]
                {
                    new CoinbaseMarketTradeWebSocketEvent
                    {
                        Type = "update",
                        TradeId = "trade",
                        ProductId = "ETH-USD",
                        Price = "secret-payload",
                        Size = "1",
                        Time = "now"
                    }
                });
            Assert.Throws<InvalidDataException>(() =>
                AdvancedTradeWebSocketMapper.MapMarketTrades(invalid));
        }

        [TestMethod]
        public async Task ChangeFeed_MapsMessagesAndAwaitsExistingNotifications()
        {
            ControlledMarketDataWebSocket webSocket = new ControlledMarketDataWebSocket();
            using HttpClient httpClient = new HttpClient(new NoopHandler());
            using CoinbaseAdvancedTrade plugin = new CoinbaseAdvancedTrade(
                new CoinbaseAdvancedTradeClient(httpClient),
                null,
                webSocket);
            List<bool> subscriptionStates = new List<bool>();
            int priceNotifications = 0;
            int assetNotifications = 0;
            plugin.DelegateNotifySubscriptionStatus = (_, running) =>
            {
                subscriptionStates.Add(running);
                return Task.CompletedTask;
            };
            plugin.NotifyCurrentPrices = (_, _) =>
            {
                priceNotifications++;
                return Task.CompletedTask;
            };
            plugin.NotifyAssetInformation = (_, _) =>
            {
                assetNotifications++;
                return Task.CompletedTask;
            };

            await plugin.ChangeFeed(new List<Product>
            {
                new Product { ID = "ETH-USD" },
                new Product { ID = "BTC-USD" },
                new Product { ID = "BTC-USD" }
            });
            await webSocket.Started.Task;
            await webSocket.EmitAsync(Parse(
                """
                {"channel":"ticker","timestamp":"2026-08-15T10:00:00Z","sequence_num":1,"events":[{"type":"snapshot","tickers":[{"type":"ticker","product_id":"BTC-USD","price":"65000","price_percent_chg_24_h":"4","best_bid":"64999","best_ask":"65001"}]}]}
                """));
            await webSocket.EmitAsync(Parse(
                """
                {"channel":"market_trades","timestamp":"2026-08-15T10:00:01Z","sequence_num":2,"events":[{"type":"update","trades":[{"trade_id":"0198b6cf-58a8-76f0-9a7c-2b1ef39fdb74","product_id":"BTC-USD","price":"65001","size":"0.1","side":"BUY","time":"2026-08-15T10:00:01Z"}]}]}
                """));
            await webSocket.EmitAsync(Parse(
                """
                {"channel":"l2_data","timestamp":"2026-08-15T10:00:02Z","sequence_num":3,"events":[{"type":"snapshot","product_id":"BTC-USD","updates":[{"side":"bid","price_level":"64998","new_quantity":"2"},{"side":"offer","price_level":"65002","new_quantity":"3"}]}]}
                """));
            await plugin.CloseFeed();

            CollectionAssert.AreEqual(new[] { "BTC-USD", "ETH-USD" }, webSocket.ProductIds);
            CollectionAssert.AreEqual(new[] { true, false }, subscriptionStates);
            Assert.AreEqual(2, priceNotifications);
            Assert.AreEqual(3, assetNotifications);
            Assert.AreEqual(65001m, plugin.CurrentPrices["BTC-USD"]);
            Assert.AreEqual(65001m, plugin.SubscribedPrices["BTC-USD"]);
            Assert.AreEqual(OrderSide.Buy, plugin.AssetInformation["BTC-USD"].OrderSide);
            Assert.AreEqual("64998", plugin.AssetInformation["BTC-USD"].BestBid);
            Assert.AreEqual("65002", plugin.AssetInformation["BTC-USD"].BestAsk);
            Assert.AreEqual("0198b6cf-58a8-76f0-9a7c-2b1ef39fdb74",
                plugin.MarketTrades["BTC-USD"].Trades[0].TradeId);
            Assert.AreEqual(3, plugin.ProductOrderBooks["BTC-USD"].Sequence);
            Assert.IsTrue(webSocket.CancellationObserved);
            Assert.IsEmpty(plugin.SubscribeProducts);
        }

        [TestMethod]
        public async Task ChangeFeed_ReportsMalformedMessageWithoutPayloadAndContinues()
        {
            ControlledMarketDataWebSocket webSocket = new ControlledMarketDataWebSocket();
            using HttpClient httpClient = new HttpClient(new NoopHandler());
            using CoinbaseAdvancedTrade plugin = new CoinbaseAdvancedTrade(
                new CoinbaseAdvancedTradeClient(httpClient),
                null,
                webSocket);
            List<string> logs = new List<string>();
            plugin.ProcessLogBroadcast = (_, _, message) => logs.Add(message);

            await plugin.ChangeFeed(new List<Product> { new Product { ID = "BTC-USD" } });
            await webSocket.Started.Task;
            await webSocket.EmitAsync(CoinbaseWebSocketParseResult.Malformed(
                "jwt=secret-token; product_ids=[BTC-USD]"));
            await webSocket.EmitAsync(Parse(
                """
                {"channel":"heartbeats","timestamp":"now","sequence_num":1,"events":[{"heartbeat_counter":1}]}
                """));
            await plugin.CloseFeed();

            Assert.HasCount(1, logs);
            StringAssert.Contains(logs[0], "message was malformed");
            Assert.IsFalse(logs[0].Contains("secret-token", StringComparison.Ordinal));
            Assert.IsFalse(logs[0].Contains("product_ids", StringComparison.Ordinal));
            Assert.IsFalse(logs[0].Contains("BTC-USD", StringComparison.Ordinal));
            Assert.AreEqual(2, webSocket.EmitCount);
        }

        [TestMethod]
        public async Task ChangeFeed_InvalidatesBookAndUsesPayloadFreeGapDiagnostic()
        {
            ControlledMarketDataWebSocket webSocket = new ControlledMarketDataWebSocket();
            using HttpClient httpClient = new HttpClient(new NoopHandler());
            using CoinbaseAdvancedTrade plugin = new CoinbaseAdvancedTrade(
                new CoinbaseAdvancedTradeClient(httpClient),
                null,
                webSocket);
            List<string> logs = new List<string>();
            plugin.ProcessLogBroadcast = (_, _, message) => logs.Add(message);

            await plugin.ChangeFeed(new List<Product> { new Product { ID = "BTC-USD" } });
            await webSocket.Started.Task;
            await webSocket.EmitAsync(Parse(
                """
                {"channel":"l2_data","timestamp":"now","sequence_num":1,"events":[{"type":"snapshot","product_id":"BTC-USD","updates":[{"side":"bid","price_level":"10","new_quantity":"2"}]}]}
                """));
            await webSocket.EmitAsync(CoinbaseWebSocketParseResult.ResynchronizationRequired(
                "l2_data",
                new[] { "BTC-USD" }));
            await plugin.CloseFeed();

            Assert.IsFalse(plugin.ProductOrderBooks.ContainsKey("BTC-USD"));
            Assert.IsNull(plugin.OrderBook);
            Assert.IsNull(plugin.AssetInformation["BTC-USD"].BestBid);
            Assert.HasCount(1, logs);
            StringAssert.Contains(logs[0], "resynchronization requested");
            Assert.IsFalse(logs[0].Contains("BTC-USD", StringComparison.Ordinal));
            Assert.IsFalse(logs[0].Contains("l2_data", StringComparison.Ordinal));
        }

        private static CoinbaseLevel2WebSocketUpdate Level(
            string side,
            string price,
            string quantity)
        {
            return new CoinbaseLevel2WebSocketUpdate
            {
                Side = side,
                PriceLevel = price,
                NewQuantity = quantity
            };
        }

        private static CoinbaseWebSocketParseResult Parse(string json)
        {
            CoinbaseWebSocketParseResult result = new CoinbaseWebSocketMessageParser().Parse(json);
            Assert.AreEqual(CoinbaseWebSocketParseStatus.Parsed, result.Status);
            return result;
        }

        private sealed class ControlledMarketDataWebSocket : ICoinbaseMarketDataWebSocket
        {
            private Func<CoinbaseWebSocketParseResult, CancellationToken, Task> _handler;
            private CancellationToken _cancellationToken;

            public TaskCompletionSource<bool> Started { get; } =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public string[] ProductIds { get; private set; }

            public bool CancellationObserved { get; private set; }

            public int EmitCount { get; private set; }

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

            public async Task EmitAsync(CoinbaseWebSocketParseResult result)
            {
                EmitCount++;
                await _handler(result, _cancellationToken);
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
