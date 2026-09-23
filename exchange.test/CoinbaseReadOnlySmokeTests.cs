using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase.advancedtrade;
using exchange.coinbase.advancedtrade.authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseReadOnlySmokeTests
    {
        [TestMethod]
        public void SmokeGate_RequiresExplicitTrueAndUsesPublicProductDefault()
        {
            Dictionary<string, string> environment = new Dictionary<string, string>();
            CoinbaseReadOnlySmokeGate gate = new CoinbaseReadOnlySmokeGate(
                name => environment.GetValueOrDefault(name));

            Assert.IsFalse(gate.IsEnabled());
            environment[CoinbaseReadOnlySmokeGate.EnabledVariable] = "1";
            Assert.IsFalse(gate.IsEnabled());
            environment[CoinbaseReadOnlySmokeGate.EnabledVariable] = "true";
            Assert.IsTrue(gate.IsEnabled());
            Assert.AreEqual(CoinbaseReadOnlySmokeGate.DefaultProductId, gate.GetProductId());
            environment[CoinbaseReadOnlySmokeGate.ProductIdVariable] = " ETH-EUR ";
            Assert.AreEqual("ETH-EUR", gate.GetProductId());
        }

        [TestMethod]
        public async Task Runner_PerformsOnlyReadOperationsAndReturnsPayloadFreeCounts()
        {
            RoutingHandler publicHandler = new RoutingHandler(request =>
            {
                Assert.AreEqual(
                    "/api/v3/brokerage/market/products/BTC-USD",
                    request.RequestUri.AbsolutePath);
                return Json("""{ "product_id": "BTC-USD" }""");
            });
            RoutingHandler authenticatedHandler = new RoutingHandler(request =>
            {
                string path = request.RequestUri.AbsolutePath;
                if (path.EndsWith("/key_permissions", StringComparison.Ordinal))
                    return Json("""
                        {
                          "can_view": true,
                          "can_trade": false,
                          "can_transfer": false,
                          "can_receive": false
                        }
                        """);
                if (path.EndsWith("/accounts", StringComparison.Ordinal))
                    return Json("""{ "accounts": [], "has_next": false }""");
                if (path.EndsWith("/orders/historical/batch", StringComparison.Ordinal))
                    return Json("""{ "orders": [], "has_next": false }""");
                if (path.EndsWith("/orders/historical/fills", StringComparison.Ordinal))
                    return Json("""{ "fills": [] }""");
                throw new AssertFailedException($"Unexpected smoke-test route: {path}");
            });
            using HttpClient publicHttp = new HttpClient(publicHandler);
            using HttpClient authenticatedHttp = CreateAuthenticatedHttp(authenticatedHandler);
            CoinbaseReadOnlySmokeRunner runner = new CoinbaseReadOnlySmokeRunner(
                new CoinbaseAdvancedTradeClient(publicHttp),
                new CoinbaseAdvancedTradeAuthenticatedClient(authenticatedHttp));

            CoinbaseReadOnlySmokeResult result = await runner.RunAsync("BTC-USD");

            Assert.IsTrue(result.ProductValidated);
            Assert.AreEqual(0, result.AccountCount);
            Assert.AreEqual(0, result.OrderCount);
            Assert.AreEqual(0, result.FillCount);
            Assert.HasCount(1, publicHandler.Requests);
            Assert.HasCount(4, authenticatedHandler.Requests);
            Assert.IsTrue(publicHandler.Requests.TrueForAll(request => request.Method == "GET"));
            Assert.IsTrue(authenticatedHandler.Requests.TrueForAll(request =>
                request.Method == "GET" && request.HadBearerToken));
            Assert.IsFalse(result.ToString().Contains("Bearer", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(result.ToString().Contains("product_id", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public async Task Runner_RejectsWriteCapableKeyBeforeAnyReadSmokeCalls()
        {
            RoutingHandler publicHandler = new RoutingHandler(_ =>
                throw new AssertFailedException("Public smoke read must not run before readiness passes."));
            RoutingHandler authenticatedHandler = new RoutingHandler(request =>
            {
                Assert.IsTrue(request.RequestUri.AbsolutePath.EndsWith(
                    "/key_permissions",
                    StringComparison.Ordinal));
                return Json("""
                    {
                      "can_view": true,
                      "can_trade": true,
                      "can_transfer": false,
                      "can_receive": false
                    }
                    """);
            });
            using HttpClient publicHttp = new HttpClient(publicHandler);
            using HttpClient authenticatedHttp = CreateAuthenticatedHttp(authenticatedHandler);
            CoinbaseReadOnlySmokeRunner runner = new CoinbaseReadOnlySmokeRunner(
                new CoinbaseAdvancedTradeClient(publicHttp),
                new CoinbaseAdvancedTradeAuthenticatedClient(authenticatedHttp));

            InvalidOperationException exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    runner.RunAsync("BTC-USD"));

            StringAssert.Contains(exception.Message, "read-only deployment");
            Assert.IsEmpty(publicHandler.Requests);
            Assert.HasCount(1, authenticatedHandler.Requests);
        }

        private static HttpClient CreateAuthenticatedHttp(HttpMessageHandler innerHandler)
        {
            return new HttpClient(new CoinbaseJwtAuthenticationHandler(new FixedJwtProvider())
            {
                InnerHandler = innerHandler
            });
        }

        private static HttpResponseMessage Json(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private sealed class FixedJwtProvider : ICoinbaseJwtProvider
        {
            public string CreateRestToken(HttpMethod method, Uri requestUri)
            {
                return "smoke-test-token";
            }

            public string CreateWebSocketToken()
            {
                return "smoke-test-websocket-token";
            }
        }

        private sealed class RoutingHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _route;

            public RoutingHandler(Func<HttpRequestMessage, HttpResponseMessage> route)
            {
                _route = route;
            }

            public List<CapturedRequest> Requests { get; } = new List<CapturedRequest>();

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(new CapturedRequest
                {
                    Method = request.Method.Method,
                    Uri = request.RequestUri.AbsoluteUri,
                    HadBearerToken = request.Headers.Authorization?.Scheme == "Bearer"
                });
                return Task.FromResult(_route(request));
            }
        }

        private sealed class CapturedRequest
        {
            public string Method { get; init; }

            public string Uri { get; init; }

            public bool HadBearerToken { get; init; }
        }
    }

    [TestClass]
    public class CoinbaseReadOnlyLiveSmokeTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [TestCategory("CoinbaseAdvancedTradeSmoke")]
        public async Task ExplicitlyEnabled_ViewOnlyProductionSmoke()
        {
            CoinbaseReadOnlySmokeGate gate = new CoinbaseReadOnlySmokeGate();
            if (!gate.IsEnabled())
                Assert.Inconclusive(
                    $"Set {CoinbaseReadOnlySmokeGate.EnabledVariable}=true to run this optional read-only smoke test.");

            EnvironmentCoinbaseCredentialSource credentialSource =
                new EnvironmentCoinbaseCredentialSource();
            CoinbaseApiCredentials credentials = credentialSource.GetCredentials();
            CoinbaseAdvancedTradeOptions options = new CoinbaseAdvancedTradeOptions();
            using CoinbaseJwtProvider jwtProvider = new CoinbaseJwtProvider(credentials);
            using HttpClient authenticatedHttp = new HttpClient(
                new CoinbaseJwtAuthenticationHandler(jwtProvider)
                {
                    InnerHandler = new HttpClientHandler()
                });
            using HttpClient publicHttp = new HttpClient();
            CoinbaseReadOnlySmokeRunner runner = new CoinbaseReadOnlySmokeRunner(
                new CoinbaseAdvancedTradeClient(publicHttp, options),
                new CoinbaseAdvancedTradeAuthenticatedClient(authenticatedHttp, options));

            CoinbaseReadOnlySmokeResult result = await runner.RunAsync(
                gate.GetProductId(),
                TestContext.CancellationTokenSource.Token);

            TestContext.WriteLine(result.ToString());
            Assert.IsTrue(result.ProductValidated);
        }
    }
}
