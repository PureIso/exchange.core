using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase.advancedtrade;
using exchange.core.helpers;
using exchange.core.implementations;
using exchange.core.interfaces;
using exchange.core.models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseAdvancedTradeClientTests
    {
        [TestMethod]
        public async Task ListSpotProductsAsync_MapsProductsAndBuildsPublicRequest()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "products": [
                        {
                          "product_id": "BTC-USD",
                          "base_currency_id": "BTC",
                          "quote_currency_id": "USD",
                          "base_min_size": "0.00000001",
                          "base_max_size": "1000",
                          "quote_increment": "0.01"
                        }
                      ],
                      "num_products": 1,
                      "pagination": { "has_next": false }
                    }
                    """));
            CoinbaseAdvancedTradeClient client = CreateClient(handler);

            IReadOnlyList<Product> products = await client.ListSpotProductsAsync();

            Assert.HasCount(1, products);
            Assert.AreEqual("BTC-USD", products[0].ID);
            Assert.AreEqual("BTC", products[0].BaseCurrency);
            Assert.AreEqual("USD", products[0].QuoteCurrency);
            Assert.AreEqual("0.00000001", products[0].BaseMinSize);
            Assert.AreEqual("1000", products[0].BaseMaxSize);
            Assert.AreEqual("0.01", products[0].QuoteIncrement);
            Assert.HasCount(1, handler.Requests);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/market/products?product_type=SPOT&limit=100",
                handler.Requests[0].RequestUri.AbsoluteUri);
            Assert.IsFalse(handler.Requests[0].Headers.Contains("Authorization"));
        }

        [TestMethod]
        public async Task ListSpotProductsAsync_FollowsCursorPagination()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "products": [{ "product_id": "BTC-USD" }],
                      "pagination": {
                        "next_cursor": "BTC/USD+=",
                        "has_next": true
                      }
                    }
                    """),
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "products": [{ "product_id": "ETH-EUR" }],
                      "pagination": { "has_next": false }
                    }
                    """));
            CoinbaseAdvancedTradeClient client = CreateClient(handler, 25);

            IReadOnlyList<Product> products = await client.ListSpotProductsAsync();

            CollectionAssert.AreEqual(
                new[] { "BTC-USD", "ETH-EUR" },
                products.Select(product => product.ID).ToArray());
            Assert.HasCount(2, handler.Requests);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/market/products?product_type=SPOT&limit=25&cursor=BTC%2FUSD%2B%3D",
                handler.Requests[1].RequestUri.AbsoluteUri);
        }

        [TestMethod]
        public async Task ListSpotProductsAsync_ThrowsForApiError()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.TooManyRequests, "{\"error\":\"rate_limited\"}"));
            CoinbaseAdvancedTradeClient client = CreateClient(handler);

            await Assert.ThrowsAsync<HttpRequestException>(() => client.ListSpotProductsAsync());
        }

        [TestMethod]
        public async Task ListSpotProductsAsync_ThrowsForRepeatedCursor()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "products": [],
                      "pagination": { "next_cursor": "same", "has_next": true }
                    }
                    """),
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "products": [],
                      "pagination": { "next_cursor": "same", "has_next": true }
                    }
                    """));
            CoinbaseAdvancedTradeClient client = CreateClient(handler);

            await Assert.ThrowsAsync<InvalidDataException>(() => client.ListSpotProductsAsync());
        }

        [TestMethod]
        public async Task GetProductAsync_MapsSinglePublicProduct()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "product_id": "ETH-EUR",
                      "base_currency_id": "ETH",
                      "quote_currency_id": "EUR",
                      "base_min_size": "0.001",
                      "base_max_size": "5000",
                      "quote_increment": "0.01"
                    }
                    """));
            CoinbaseAdvancedTradeClient client = CreateClient(handler);

            Product product = await client.GetProductAsync("ETH-EUR");

            Assert.AreEqual("ETH-EUR", product.ID);
            Assert.AreEqual("ETH", product.BaseCurrency);
            Assert.AreEqual("EUR", product.QuoteCurrency);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/market/products/ETH-EUR",
                handler.Requests[0].RequestUri.AbsoluteUri);
            AssertPublicRequests(handler);
        }

        [TestMethod]
        public async Task GetTwentyFourHourStatisticsAsync_MapsProductFieldsAndDerivesOpen()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "product_id": "BTC-USD",
                      "price": "105.00",
                      "price_percentage_change_24h": "5%",
                      "volume_24h": "1234.50",
                      "high_24h": "108.25",
                      "low_24h": "96.75"
                    }
                    """));
            CoinbaseAdvancedTradeClient client = CreateClient(handler);

            Statistics statistics = await client.GetTwentyFourHourStatisticsAsync("BTC-USD");

            Assert.AreEqual("100", statistics.Open);
            Assert.AreEqual("108.25", statistics.High);
            Assert.AreEqual("96.75", statistics.Low);
            Assert.AreEqual("1234.50", statistics.Volume);
            Assert.AreEqual("105.00", statistics.Last);
            Assert.IsNull(statistics.Volume30Day);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/market/products/BTC-USD",
                handler.Requests[0].RequestUri.AbsoluteUri);
            AssertPublicRequests(handler);
        }

        [TestMethod]
        public async Task GetTwentyFourHourStatisticsAsync_RejectsImpossiblePercentageChange()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "product_id": "BTC-USD",
                      "price": "1",
                      "price_percentage_change_24h": "-100%",
                      "volume_24h": "10",
                      "high_24h": "2",
                      "low_24h": "1"
                    }
                    """));
            CoinbaseAdvancedTradeClient client = CreateClient(handler);

            await Assert.ThrowsAsync<System.Text.Json.JsonException>(
                () => client.GetTwentyFourHourStatisticsAsync("BTC-USD"));
        }

        [TestMethod]
        public async Task GetMarketTradesAsync_PreservesUuidAndMapsSnapshot()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "trades": [{
                        "trade_id": "34b080bf-fcfd-445a-832b-46b5ddc65601",
                        "product_id": "BTC-USD",
                        "price": "140.91",
                        "size": "4",
                        "time": "2021-05-31T09:59:59Z",
                        "side": "BUY",
                        "exchange": "coinbase"
                      }],
                      "best_bid": "140.90",
                      "best_ask": "140.92"
                    }
                    """));
            CoinbaseAdvancedTradeClient client = CreateClient(handler);

            CoinbaseMarketTradesSnapshot snapshot = await client.GetMarketTradesAsync("BTC-USD", 25);

            Assert.AreEqual("140.90", snapshot.BestBid);
            Assert.AreEqual("140.92", snapshot.BestAsk);
            Assert.HasCount(1, snapshot.Trades);
            Assert.AreEqual("34b080bf-fcfd-445a-832b-46b5ddc65601", snapshot.Trades[0].TradeId);
            Assert.AreEqual(new DateTimeOffset(2021, 5, 31, 9, 59, 59, TimeSpan.Zero), snapshot.Trades[0].Time);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/market/products/BTC-USD/ticker?limit=25",
                handler.Requests[0].RequestUri.AbsoluteUri);
            AssertPublicRequests(handler);
        }

        [TestMethod]
        public async Task GetProductBookAsync_MapsLevelsToCoreOrderBook()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "pricebook": {
                        "product_id": "BTC-USD",
                        "bids": [{ "price": "100.10", "size": "1.25" }],
                        "asks": [{ "price": "100.20", "size": "2.50" }],
                        "time": "2026-08-15T06:00:00Z"
                      }
                    }
                    """));
            CoinbaseAdvancedTradeClient client = CreateClient(handler);

            OrderBook book = await client.GetProductBookAsync("BTC-USD", 10);

            Assert.AreEqual("100.10", book.Bids.ToOrderList()[0].Price);
            Assert.AreEqual("1.25", book.Bids.ToOrderList()[0].Size);
            Assert.AreEqual("100.20", book.Asks.ToOrderList()[0].Price);
            Assert.AreEqual("2.50", book.Asks.ToOrderList()[0].Size);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/market/product_book?product_id=BTC-USD&limit=10",
                handler.Requests[0].RequestUri.AbsoluteUri);
            AssertPublicRequests(handler);
        }

        [TestMethod]
        public async Task GetCandlesAsync_UsesUnixRangeAndReturnsAscendingCoreRates()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "candles": [
                        { "start": "1639508400", "low": "141.1", "high": "142.2", "open": "141.3", "close": "142.0", "volume": "11.5" },
                        { "start": "1639508100", "low": "140.1", "high": "141.2", "open": "140.3", "close": "141.0", "volume": "10.5" }
                      ]
                    }
                    """));
            CoinbaseAdvancedTradeClient client = CreateClient(handler);
            HistoricCandlesSearch search = new HistoricCandlesSearch
            {
                Symbol = "BTC-USD",
                StartingDateTime = DateTimeOffset.FromUnixTimeSeconds(1639508100).UtcDateTime,
                EndingDateTime = DateTimeOffset.FromUnixTimeSeconds(1639508700).UtcDateTime,
                Granularity = Granularity.FiveMinutes
            };

            IReadOnlyList<HistoricRate> rates = await client.GetCandlesAsync(search);

            Assert.HasCount(2, rates);
            Assert.AreEqual(DateTimeOffset.FromUnixTimeSeconds(1639508100).UtcDateTime, rates[0].DateAndTime);
            Assert.AreEqual(140.1m, rates[0].Low);
            Assert.AreEqual(141.0m, rates[0].Close);
            Assert.AreEqual(11.5m, rates[1].Volume);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/market/products/BTC-USD/candles?start=1639508100&end=1639508700&granularity=FIVE_MINUTE&limit=2",
                handler.Requests[0].RequestUri.AbsoluteUri);
            AssertPublicRequests(handler);
        }

        [TestMethod]
        public async Task GetCandlesAsync_SplitsRangesAtCoinbaseMaximumAndDeduplicatesBoundaries()
        {
            const long start = 1700000000;
            const long boundary = start + 350 * 300;
            const long end = boundary + 300;
            string boundaryCandle =
                $"{{\"start\":\"{boundary}\",\"low\":\"1\",\"high\":\"2\",\"open\":\"1\",\"close\":\"2\",\"volume\":\"3\"}}";
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK, $"{{\"candles\":[{boundaryCandle}]}}"),
                CreateJsonResponse(HttpStatusCode.OK, $"{{\"candles\":[{boundaryCandle}]}}"));
            CoinbaseAdvancedTradeClient client = CreateClient(handler);
            HistoricCandlesSearch search = new HistoricCandlesSearch
            {
                Symbol = "BTC-USD",
                StartingDateTime = DateTimeOffset.FromUnixTimeSeconds(start).UtcDateTime,
                EndingDateTime = DateTimeOffset.FromUnixTimeSeconds(end).UtcDateTime,
                Granularity = Granularity.FiveMinutes
            };

            IReadOnlyList<HistoricRate> rates = await client.GetCandlesAsync(search);

            Assert.HasCount(1, rates);
            Assert.HasCount(2, handler.Requests);
            StringAssert.Contains(handler.Requests[0].RequestUri.Query, "limit=350");
            StringAssert.Contains(handler.Requests[1].RequestUri.Query, "limit=1");
        }

        [TestMethod]
        public async Task PublicMethods_RejectInvalidInputBeforeSending()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler();
            CoinbaseAdvancedTradeClient client = CreateClient(handler);

            await Assert.ThrowsAsync<ArgumentException>(() => client.GetProductAsync(" "));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetMarketTradesAsync("BTC-USD", 0));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetProductBookAsync("BTC-USD", 0));
            await Assert.ThrowsAsync<ArgumentException>(() => client.GetCandlesAsync(new HistoricCandlesSearch
            {
                Symbol = "BTC-USD",
                StartingDateTime = DateTime.UtcNow,
                EndingDateTime = DateTime.UtcNow.AddMinutes(-5),
                Granularity = Granularity.FiveMinutes
            }));
            Assert.HasCount(0, handler.Requests);
        }

        [TestMethod]
        public void Constructor_RejectsInsecureBaseUri()
        {
            using HttpClient httpClient = new HttpClient(new RecordingHttpMessageHandler());
            CoinbaseAdvancedTradeOptions options = new CoinbaseAdvancedTradeOptions
            {
                RestBaseUri = new Uri("http://api.coinbase.test/api/v3/brokerage/")
            };

            Assert.Throws<ArgumentException>(() => new CoinbaseAdvancedTradeClient(httpClient, options));
        }

        [TestMethod]
        public void Plugin_HasLoaderCompatibleConstructorAndMetadata()
        {
            using CoinbaseAdvancedTrade plugin = new CoinbaseAdvancedTrade();

            Assert.IsInstanceOfType<AbstractExchangePlugin>(plugin);
            Assert.IsNotNull(typeof(CoinbaseAdvancedTrade).GetConstructor(Type.EmptyTypes));
            Assert.AreEqual("Coinbase Advanced Trade", plugin.ApplicationName);
            StringAssert.Contains(plugin.Description, "read-only accounts");
        }

        [TestMethod]
        public async Task Plugin_InitAsync_LoadsPublicProductsAndInitializesState()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "products": [{ "product_id": "BTC-USD" }],
                      "pagination": { "has_next": false }
                    }
                    """));
            using CoinbaseAdvancedTrade plugin = CreatePlugin(handler);
            TestExchangeSettings settings = new TestExchangeSettings
            {
                TestMode = true,
                IndicatorDirectoryPath = "indicators",
                INIDirectoryPath = "configuration",
                MainCurrency = "EUR"
            };

            bool initialized = await plugin.InitAsync(settings);

            Assert.IsTrue(initialized);
            Assert.HasCount(1, plugin.Products);
            Assert.AreEqual("BTC-USD", plugin.Products[0].ID);
            Assert.AreEqual("EUR", plugin.MainCurrency);
            Assert.IsTrue(plugin.TestMode);
            Assert.IsNotNull(plugin.CurrentPrices);
            Assert.IsNotNull(plugin.Statistics);
            Assert.IsNotNull(plugin.MarketTrades);
            AssertPublicRequests(handler);
        }

        [TestMethod]
        public async Task Plugin_UpdateMarketTradesAsync_StoresSnapshotPriceAndNotifies()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "trades": [{
                        "trade_id": "6ad5ed31-45a2-4e5f-a4f1-a21caa23615c",
                        "product_id": "BTC-USD",
                        "price": "123.45",
                        "size": "0.5",
                        "time": "2026-08-15T10:00:00Z"
                      }],
                      "best_bid": "123.44",
                      "best_ask": "123.46"
                    }
                    """));
            using CoinbaseAdvancedTrade plugin = CreatePlugin(handler);
            Dictionary<string, decimal> notifiedPrices = null;
            plugin.NotifyCurrentPrices = (_, prices) =>
            {
                notifiedPrices = new Dictionary<string, decimal>(prices);
                return Task.CompletedTask;
            };

            CoinbaseMarketTradesSnapshot snapshot = await plugin.UpdateMarketTradesAsync(
                new Product { ID = "BTC-USD" },
                1);

            Assert.HasCount(1, snapshot.Trades);
            Assert.AreEqual(123.45m, plugin.CurrentPrices["BTC-USD"]);
            Assert.AreEqual(123.45m, notifiedPrices["BTC-USD"]);
            Assert.AreSame(snapshot, plugin.MarketTrades["BTC-USD"]);
        }

        [TestMethod]
        public async Task Plugin_PublicMethods_UpdateCoreState()
        {
            RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler(
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "product_id": "BTC-USD",
                      "base_currency_id": "BTC",
                      "quote_currency_id": "USD"
                    }
                    """),
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    { "pricebook": {
                        "bids": [{ "price": "100", "size": "1" }],
                        "asks": [{ "price": "101", "size": "2" }]
                    } }
                    """),
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    { "candles": [
                        { "start": "1700000000", "low": "99", "high": "102", "open": "100", "close": "101", "volume": "3" }
                    ] }
                    """),
                CreateJsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "product_id": "BTC-USD",
                      "price": "101",
                      "price_percentage_change_24h": "1%",
                      "volume_24h": "3",
                      "high_24h": "102",
                      "low_24h": "99"
                    }
                    """));
            using CoinbaseAdvancedTrade plugin = CreatePlugin(handler);
            Product product = new Product { ID = "BTC-USD" };

            Product updatedProduct = await plugin.UpdateProductAsync(product);
            OrderBook book = await plugin.UpdateProductOrderBookAsync(product, 5);
            IReadOnlyList<HistoricRate> rates = await plugin.UpdateProductHistoricCandlesAsync(
                new HistoricCandlesSearch
                {
                    Symbol = "BTC-USD",
                    StartingDateTime = DateTimeOffset.FromUnixTimeSeconds(1700000000).UtcDateTime,
                    EndingDateTime = DateTimeOffset.FromUnixTimeSeconds(1700000300).UtcDateTime,
                    Granularity = Granularity.FiveMinutes
                });
            Statistics statistics = await plugin.TwentyFourHoursRollingStatsAsync(product);

            Assert.AreEqual("BTC", updatedProduct.BaseCurrency);
            Assert.AreSame(updatedProduct, plugin.Products[0]);
            Assert.AreSame(book, plugin.OrderBook);
            Assert.AreSame(plugin.HistoricRates, rates);
            Assert.AreSame(statistics, plugin.Statistics["BTC-USD"]);
            Assert.AreSame(product, plugin.SelectedProduct);
            Assert.HasCount(4, handler.Requests);
        }

        [TestMethod]
        public async Task Plugin_UnsupportedOperationsFailExplicitly()
        {
            using CoinbaseAdvancedTrade plugin = CreatePlugin(new RecordingHttpMessageHandler());

            Assert.Throws<NotSupportedException>(() =>
                plugin.InitIndicatorsAsync(new List<Product>()));
            await Assert.ThrowsAsync<NotSupportedException>(() => plugin.UpdateOrdersAsync());
            await Assert.ThrowsAsync<NotSupportedException>(() => plugin.PostOrdersAsync(new Order()));
        }

        private static CoinbaseAdvancedTradeClient CreateClient(
            RecordingHttpMessageHandler handler,
            int pageSize = 100)
        {
            HttpClient httpClient = new HttpClient(handler);
            CoinbaseAdvancedTradeOptions options = new CoinbaseAdvancedTradeOptions
            {
                ProductPageSize = pageSize
            };
            return new CoinbaseAdvancedTradeClient(httpClient, options);
        }

        private static CoinbaseAdvancedTrade CreatePlugin(RecordingHttpMessageHandler handler)
        {
            return new CoinbaseAdvancedTrade(CreateClient(handler));
        }

        private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private static void AssertPublicRequests(RecordingHttpMessageHandler handler)
        {
            Assert.IsTrue(handler.Requests.All(request => !request.Headers.Contains("Authorization")));
        }

        private sealed class RecordingHttpMessageHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;

            public RecordingHttpMessageHandler(params HttpResponseMessage[] responses)
            {
                _responses = new Queue<HttpResponseMessage>(responses);
            }

            public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(request);
                if (_responses.Count == 0)
                    throw new InvalidOperationException("No HTTP response was configured for the request.");
                return Task.FromResult(_responses.Dequeue());
            }
        }

        private sealed class TestExchangeSettings : IExchangeSettings
        {
            public bool TestMode { get; set; }
            public string IndicatorDirectoryPath { get; set; }
            public string INIDirectoryPath { get; set; }
            public string MainCurrency { get; set; }
        }
    }
}
