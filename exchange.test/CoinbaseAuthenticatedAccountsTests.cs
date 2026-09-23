using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase.advancedtrade;
using exchange.coinbase.advancedtrade.authentication;
using exchange.core.models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseAuthenticatedAccountsTests
    {
        [TestMethod]
        public async Task ListAccountsAsync_AuthenticatesEveryPageAndMapsBalances()
        {
            CapturingHandler innerHandler = new CapturingHandler(
                JsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "has_next": true,
                      "cursor": "next/cursor+=",
                      "accounts": [{
                        "uuid": "8bfc20d7-f7c6-4422-bf07-8243ca4169fe",
                        "currency": "BTC",
                        "available_balance": { "value": "1.20", "currency": "BTC" },
                        "hold": { "value": "0.30", "currency": "BTC" },
                        "active": true,
                        "ready": true
                      }]
                    }
                    """),
                JsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "has_next": false,
                      "accounts": [{
                        "uuid": "14a298f5-2640-4dd3-86cf-bbca240faf83",
                        "currency": "EUR",
                        "available_balance": { "value": "10", "currency": "EUR" },
                        "hold": { "value": "0", "currency": "EUR" },
                        "active": true,
                        "ready": false
                      }]
                    }
                    """));
            RecordingJwtProvider jwtProvider = new RecordingJwtProvider();
            CoinbaseAdvancedTradeAuthenticatedClient client = CreateClient(innerHandler, jwtProvider, 2);

            IReadOnlyList<Account> accounts = await client.ListAccountsAsync();

            Assert.HasCount(2, accounts);
            Assert.AreEqual("1.50", accounts[0].Balance);
            Assert.AreEqual("1.20", accounts[0].Available);
            Assert.AreEqual("0.30", accounts[0].Hold);
            Assert.IsTrue(accounts[0].TradingEnabled);
            Assert.IsFalse(accounts[1].TradingEnabled);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/accounts?limit=2",
                innerHandler.Requests[0].Uri.AbsoluteUri);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/accounts?limit=2&cursor=next%2Fcursor%2B%3D",
                innerHandler.Requests[1].Uri.AbsoluteUri);
            CollectionAssert.AreEqual(new[] { "Bearer jwt-1", "Bearer jwt-2" },
                innerHandler.Requests.Select(request => request.Authorization).ToArray());
            Assert.HasCount(2, jwtProvider.Requests);
        }

        [TestMethod]
        public async Task ListAccountsAsync_RejectsRepeatedCursor()
        {
            string repeatedPage =
                """
                { "has_next": true, "cursor": "same", "accounts": [] }
                """;
            CapturingHandler innerHandler = new CapturingHandler(
                JsonResponse(HttpStatusCode.OK, repeatedPage),
                JsonResponse(HttpStatusCode.OK, repeatedPage));
            CoinbaseAdvancedTradeAuthenticatedClient client = CreateClient(
                innerHandler,
                new RecordingJwtProvider());

            await Assert.ThrowsAsync<System.IO.InvalidDataException>(() => client.ListAccountsAsync());
            Assert.HasCount(2, innerHandler.Requests);
        }

        [TestMethod]
        public async Task GetAccountAsync_MapsWrappedAccountAndValidatesUuid()
        {
            const string accountUuid = "8bfc20d7-f7c6-4422-bf07-8243ca4169fe";
            CapturingHandler innerHandler = new CapturingHandler(
                JsonResponse(HttpStatusCode.OK,
                    """
                    { "account": {
                      "uuid": "8bfc20d7-f7c6-4422-bf07-8243ca4169fe",
                      "currency": "BTC",
                      "available_balance": { "value": "2", "currency": "BTC" },
                      "hold": { "value": "0.5", "currency": "BTC" },
                      "active": true,
                      "ready": true
                    } }
                    """));
            CoinbaseAdvancedTradeAuthenticatedClient client = CreateClient(
                innerHandler,
                new RecordingJwtProvider());

            Account account = await client.GetAccountAsync(accountUuid);

            Assert.AreEqual("2.5", account.Balance);
            Assert.AreEqual(
                $"https://api.coinbase.com/api/v3/brokerage/accounts/{accountUuid}",
                innerHandler.Requests[0].Uri.AbsoluteUri);
            await Assert.ThrowsAsync<ArgumentException>(() => client.GetAccountAsync("not-a-uuid"));
            Assert.HasCount(1, innerHandler.Requests);
        }

        [TestMethod]
        public async Task AuthenticationHandler_RedactsTokenFromTransportErrorsAndRequest()
        {
            const string token = "secret.jwt.token";
            ThrowingHandler innerHandler = new ThrowingHandler(token);
            CoinbaseJwtAuthenticationHandler authenticationHandler = new CoinbaseJwtAuthenticationHandler(
                new FixedJwtProvider(token))
            {
                InnerHandler = innerHandler
            };
            using HttpClient httpClient = new HttpClient(authenticationHandler);

            HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
                () => httpClient.GetAsync("https://api.coinbase.com/api/v3/brokerage/accounts"));

            Assert.IsFalse(exception.ToString().Contains(token, StringComparison.Ordinal));
            Assert.IsNull(exception.InnerException);
            Assert.IsNull(innerHandler.Request.Headers.Authorization);
        }

        [TestMethod]
        public async Task Plugin_UpdateAccountsAsync_StoresBalancesAndAwaitsNotifications()
        {
            CapturingHandler innerHandler = new CapturingHandler(
                JsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "has_next": false,
                      "accounts": [
                        {
                          "uuid": "8bfc20d7-f7c6-4422-bf07-8243ca4169fe",
                          "currency": "BTC",
                          "available_balance": { "value": "1", "currency": "BTC" },
                          "hold": { "value": "0.25", "currency": "BTC" },
                          "active": true,
                          "ready": true
                        },
                        {
                          "uuid": "14a298f5-2640-4dd3-86cf-bbca240faf83",
                          "currency": "EUR",
                          "available_balance": { "value": "0", "currency": "EUR" },
                          "hold": { "value": "0", "currency": "EUR" },
                          "active": true,
                          "ready": true
                        }
                      ]
                    }
                    """));
            CoinbaseAdvancedTradeAuthenticatedClient authenticatedClient = CreateClient(
                innerHandler,
                new RecordingJwtProvider());
            CoinbaseAdvancedTradeClient publicClient = new CoinbaseAdvancedTradeClient(
                new HttpClient(new CapturingHandler()));
            using CoinbaseAdvancedTrade plugin = new CoinbaseAdvancedTrade(publicClient, authenticatedClient)
            {
                MainCurrency = "EUR"
            };
            Dictionary<string, decimal> notifiedAccounts = null;
            string notifiedMainCurrency = null;
            plugin.NotifyAccountInfo = (_, accounts) =>
            {
                notifiedAccounts = new Dictionary<string, decimal>(accounts);
                return Task.CompletedTask;
            };
            plugin.NotifyMainCurrency = (_, currency) =>
            {
                notifiedMainCurrency = currency;
                return Task.CompletedTask;
            };

            List<Account> accounts = await plugin.UpdateAccountsAsync();

            Assert.HasCount(2, accounts);
            Assert.AreSame(accounts, plugin.Accounts);
            Assert.AreEqual(1.25m, plugin.AccountInfo["BTC"]);
            Assert.IsFalse(plugin.AccountInfo.ContainsKey("EUR"));
            Assert.AreEqual(1.25m, notifiedAccounts["BTC"]);
            Assert.AreEqual("EUR", notifiedMainCurrency);
        }

        [TestMethod]
        public async Task ListOrdersAsync_PaginatesFiltersAndFlattensConfigurations()
        {
            CapturingHandler innerHandler = new CapturingHandler(
                JsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "has_next": true,
                      "cursor": "orders/next+=",
                      "orders": [{
                        "order_id": "order-one",
                        "product_id": "BTC-USD",
                        "order_configuration": {
                          "limit_limit_gtc": {
                            "base_size": "0.25",
                            "quote_size": "25",
                            "limit_price": "100",
                            "post_only": true
                          }
                        },
                        "side": "BUY",
                        "status": "OPEN",
                        "created_time": "2026-08-15T10:00:00Z",
                        "total_fees": "0.10",
                        "time_in_force": "GOOD_UNTIL_CANCELLED",
                        "filled_size": "0.05",
                        "filled_value": "5",
                        "order_type": "LIMIT",
                        "settled": false,
                        "size_in_quote": false
                      }]
                    }
                    """),
                JsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "has_next": false,
                      "orders": [{
                        "order_id": "order-two",
                        "product_id": "BTC-USD",
                        "order_configuration": {
                          "market_market_ioc": { "quote_size": "50" }
                        },
                        "side": "SELL",
                        "status": "FILLED",
                        "average_filled_price": "101",
                        "filled_size": "0.49",
                        "filled_value": "50",
                        "order_type": "MARKET",
                        "settled": true,
                        "size_in_quote": true
                      }]
                    }
                    """));
            CoinbaseAdvancedTradeAuthenticatedClient client = CreateClient(
                innerHandler,
                new RecordingJwtProvider(),
                orderPageSize: 1);

            IReadOnlyList<Order> orders = await client.ListOrdersAsync("BTC-USD");

            Assert.HasCount(2, orders);
            Assert.AreEqual("order-one", orders[0].ID);
            Assert.AreEqual("buy", orders[0].Side);
            Assert.AreEqual("limit", orders[0].Type);
            Assert.AreEqual("0.25", orders[0].Size);
            Assert.AreEqual("100", orders[0].Price);
            Assert.IsTrue(orders[0].PostOnly);
            Assert.AreEqual(0.25m, orders[0].Quantity);
            Assert.AreEqual("50", orders[1].Size);
            Assert.AreEqual("101", orders[1].Price);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/orders/historical/batch?limit=1&product_ids=BTC-USD",
                innerHandler.Requests[0].Uri.AbsoluteUri);
            StringAssert.Contains(innerHandler.Requests[1].Uri.Query, "cursor=orders%2Fnext%2B%3D");
            CollectionAssert.AreEqual(new[] { "Bearer jwt-1", "Bearer jwt-2" },
                innerHandler.Requests.Select(request => request.Authorization).ToArray());
        }

        [TestMethod]
        public async Task ListFillsAsync_PaginatesAndPreservesExternalTradeIds()
        {
            CapturingHandler innerHandler = new CapturingHandler(
                JsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "cursor": "fills-next",
                      "fills": [{
                        "trade_id": "34b080bf-fcfd-445a-832b-46b5ddc65601",
                        "order_id": "order-one",
                        "trade_time": "2026-08-15T10:01:00Z",
                        "price": "100",
                        "size": "0.25",
                        "commission": "0.10",
                        "product_id": "BTC-USD",
                        "side": "BUY"
                      }]
                    }
                    """),
                JsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "fills": [{
                        "trade_id": "123",
                        "order_id": "order-two",
                        "trade_time": "2026-08-15T10:02:00Z",
                        "price": "101",
                        "size": "0.10",
                        "commission": "0.05",
                        "product_id": "BTC-USD",
                        "side": "SELL"
                      }]
                    }
                    """));
            CoinbaseAdvancedTradeAuthenticatedClient client = CreateClient(
                innerHandler,
                new RecordingJwtProvider(),
                fillPageSize: 1);

            IReadOnlyList<Fill> fills = await client.ListFillsAsync("BTC-USD");

            Assert.HasCount(2, fills);
            Assert.AreEqual(0, fills[0].TradeID);
            Assert.AreEqual("34b080bf-fcfd-445a-832b-46b5ddc65601", fills[0].ExternalTradeID);
            Assert.AreEqual("buy", fills[0].Side);
            Assert.AreEqual(0.10m, decimal.Parse(fills[0].Fee, System.Globalization.CultureInfo.InvariantCulture));
            Assert.AreEqual(123, fills[1].TradeID);
            Assert.AreEqual("123", fills[1].ExternalTradeID);
            Assert.IsTrue(fills.All(fill => fill.Settled));
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/orders/historical/fills?limit=1&product_ids=BTC-USD",
                innerHandler.Requests[0].Uri.AbsoluteUri);
            StringAssert.Contains(innerHandler.Requests[1].Uri.Query, "cursor=fills-next");
        }

        [TestMethod]
        public async Task HistoryRequests_FailClosedWhenProofTokenIsRequired()
        {
            CapturingHandler innerHandler = new CapturingHandler(
                JsonResponse(HttpStatusCode.OK,
                    """
                    { "orders": [], "has_next": false, "proof_token_required": true }
                    """));
            CoinbaseAdvancedTradeAuthenticatedClient client = CreateClient(
                innerHandler,
                new RecordingJwtProvider());

            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(
                () => client.ListOrdersAsync());

            StringAssert.Contains(exception.Message, "EU SCA proof token");
        }

        [TestMethod]
        public async Task Plugin_OrderAndFillUpdatesStoreStateAndAwaitNotifications()
        {
            CapturingHandler innerHandler = new CapturingHandler(
                JsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "has_next": false,
                      "orders": [{
                        "order_id": "order-one",
                        "product_id": "BTC-USD",
                        "order_configuration": { "market_market_ioc": { "base_size": "0.1" } },
                        "side": "BUY",
                        "status": "OPEN",
                        "order_type": "MARKET"
                      }]
                    }
                    """),
                JsonResponse(HttpStatusCode.OK,
                    """
                    {
                      "fills": [{
                        "trade_id": "trade-one",
                        "order_id": "order-one",
                        "trade_time": "2026-08-15T10:01:00Z",
                        "price": "100",
                        "size": "0.1",
                        "commission": "0.01",
                        "product_id": "BTC-USD",
                        "side": "BUY"
                      }]
                    }
                    """));
            CoinbaseAdvancedTradeAuthenticatedClient authenticatedClient = CreateClient(
                innerHandler,
                new RecordingJwtProvider());
            CoinbaseAdvancedTradeClient publicClient = new CoinbaseAdvancedTradeClient(
                new HttpClient(new CapturingHandler()));
            using CoinbaseAdvancedTrade plugin = new CoinbaseAdvancedTrade(publicClient, authenticatedClient);
            List<Order> notifiedOrders = null;
            List<Fill> notifiedFills = null;
            plugin.NotifyOrders = (_, orders) =>
            {
                notifiedOrders = orders;
                return Task.CompletedTask;
            };
            plugin.NotifyFills = (_, fills) =>
            {
                notifiedFills = fills;
                return Task.CompletedTask;
            };
            Product product = new Product { ID = "BTC-USD" };

            List<Order> orders = await plugin.UpdateOrdersAsync(product);
            List<Fill> fills = await plugin.UpdateFillsAsync(product);

            Assert.AreSame(orders, plugin.Orders);
            Assert.AreSame(fills, plugin.Fills);
            Assert.AreSame(orders, notifiedOrders);
            Assert.AreSame(fills, notifiedFills);
            Assert.HasCount(2, innerHandler.Requests);
            Assert.IsTrue(innerHandler.Requests.All(request =>
                request.Uri.Query.Contains("product_ids=BTC-USD", StringComparison.Ordinal)));
        }

        private static CoinbaseAdvancedTradeAuthenticatedClient CreateClient(
            HttpMessageHandler innerHandler,
            ICoinbaseJwtProvider jwtProvider,
            int accountPageSize = 250,
            int orderPageSize = 100,
            int fillPageSize = 100)
        {
            CoinbaseJwtAuthenticationHandler authenticationHandler = new CoinbaseJwtAuthenticationHandler(jwtProvider)
            {
                InnerHandler = innerHandler
            };
            HttpClient httpClient = new HttpClient(authenticationHandler);
            return new CoinbaseAdvancedTradeAuthenticatedClient(
                httpClient,
                new CoinbaseAdvancedTradeOptions
                {
                    AccountPageSize = accountPageSize,
                    OrderPageSize = orderPageSize,
                    FillPageSize = fillPageSize
                });
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private sealed class RequestSnapshot
        {
            public Uri Uri { get; init; }
            public string Authorization { get; init; }
        }

        private sealed class CapturingHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;

            public CapturingHandler(params HttpResponseMessage[] responses)
            {
                _responses = new Queue<HttpResponseMessage>(responses);
            }

            public List<RequestSnapshot> Requests { get; } = new List<RequestSnapshot>();

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(new RequestSnapshot
                {
                    Uri = request.RequestUri,
                    Authorization = request.Headers.Authorization?.ToString()
                });
                if (_responses.Count == 0)
                    throw new InvalidOperationException("No response was configured.");
                return Task.FromResult(_responses.Dequeue());
            }
        }

        private sealed class ThrowingHandler : HttpMessageHandler
        {
            private readonly string _token;

            public ThrowingHandler(string token)
            {
                _token = token;
            }

            public HttpRequestMessage Request { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Request = request;
                throw new HttpRequestException($"Transport exposed {_token}.");
            }
        }

        private sealed class RecordingJwtProvider : ICoinbaseJwtProvider
        {
            public List<(HttpMethod Method, Uri Uri)> Requests { get; } = new List<(HttpMethod, Uri)>();

            public string CreateRestToken(HttpMethod method, Uri requestUri)
            {
                Requests.Add((method, requestUri));
                return $"jwt-{Requests.Count}";
            }

            public string CreateWebSocketToken()
            {
                throw new NotSupportedException();
            }
        }

        private sealed class FixedJwtProvider : ICoinbaseJwtProvider
        {
            private readonly string _token;

            public FixedJwtProvider(string token)
            {
                _token = token;
            }

            public string CreateRestToken(HttpMethod method, Uri requestUri)
            {
                return _token;
            }

            public string CreateWebSocketToken()
            {
                return _token;
            }
        }
    }
}
