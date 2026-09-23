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
    public class CoinbaseReadinessTests
    {
        [TestMethod]
        public void EnvironmentCredentialSource_LoadsNamedValuesWithoutReadingRealEnvironment()
        {
            Dictionary<string, string> environment = new Dictionary<string, string>
            {
                [EnvironmentCoinbaseCredentialSource.DefaultApiKeyNameVariable] =
                    "organizations/test/apiKeys/read-only",
                [EnvironmentCoinbaseCredentialSource.DefaultPrivateKeyVariable] =
                    "secret-private-key"
            };
            EnvironmentCoinbaseCredentialSource source = new EnvironmentCoinbaseCredentialSource(
                environmentReader: name => environment.GetValueOrDefault(name));

            CoinbaseApiCredentials credentials = source.GetCredentials();

            Assert.AreEqual("organizations/test/apiKeys/read-only", credentials.ApiKeyName);
            Assert.AreEqual("secret-private-key", credentials.PrivateKeyPem);
            Assert.AreEqual(nameof(CoinbaseApiCredentials), credentials.ToString());
        }

        [TestMethod]
        [DataRow(EnvironmentCoinbaseCredentialSource.DefaultApiKeyNameVariable)]
        [DataRow(EnvironmentCoinbaseCredentialSource.DefaultPrivateKeyVariable)]
        public void EnvironmentCredentialSource_FailsClosedWithoutExposingConfiguredSecret(
            string missingVariable)
        {
            const string secret = "DO-NOT-EXPOSE-THIS-PRIVATE-KEY";
            Dictionary<string, string> environment = new Dictionary<string, string>
            {
                [EnvironmentCoinbaseCredentialSource.DefaultApiKeyNameVariable] = "test-key-name",
                [EnvironmentCoinbaseCredentialSource.DefaultPrivateKeyVariable] = secret
            };
            environment.Remove(missingVariable);
            EnvironmentCoinbaseCredentialSource source = new EnvironmentCoinbaseCredentialSource(
                environmentReader: name => environment.GetValueOrDefault(name));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                source.GetCredentials);

            StringAssert.Contains(exception.Message, missingVariable);
            Assert.IsFalse(exception.ToString().Contains(secret, StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task GetApiKeyPermissionsAsync_AuthenticatesAndMapsCurrentSchema()
        {
            CapturingHandler innerHandler = new CapturingHandler(
                JsonResponse(
                    """
                    {
                      "can_view": true,
                      "can_trade": false,
                      "can_transfer": false,
                      "can_receive": false,
                      "portfolio_uuid": "5d4f69c9-c02e-4f45-b8a0-a38c87859989",
                      "portfolio_type": "DEFAULT"
                    }
                    """));
            CoinbaseAdvancedTradeAuthenticatedClient client = CreateClient(innerHandler);

            CoinbaseApiKeyPermissions permissions = await client.GetApiKeyPermissionsAsync();

            Assert.IsTrue(permissions.CanView);
            Assert.IsFalse(permissions.CanTrade);
            Assert.IsFalse(permissions.CanTransfer);
            Assert.IsFalse(permissions.CanReceive);
            Assert.AreEqual("5d4f69c9-c02e-4f45-b8a0-a38c87859989", permissions.PortfolioUuid);
            Assert.AreEqual("DEFAULT", permissions.PortfolioType);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/key_permissions",
                innerHandler.RequestUri.AbsoluteUri);
            Assert.AreEqual("Bearer readiness-token", innerHandler.Authorization);
        }

        [TestMethod]
        public async Task ReadinessValidator_AcceptsViewOnlyKey()
        {
            CoinbaseReadOnlyReadinessValidator validator = CreateValidator(
                canView: true,
                canTrade: false,
                canTransfer: false,
                canReceive: false);

            CoinbaseApiKeyPermissions permissions = await validator.ValidateAsync();

            Assert.IsTrue(permissions.CanView);
        }

        [TestMethod]
        [DataRow(false, false, false, false, "view permission")]
        [DataRow(true, true, false, false, "read-only deployment")]
        [DataRow(true, false, true, false, "read-only deployment")]
        [DataRow(true, false, false, true, "read-only deployment")]
        public async Task ReadinessValidator_RejectsMissingOrExcessPermissions(
            bool canView,
            bool canTrade,
            bool canTransfer,
            bool canReceive,
            string expectedMessage)
        {
            CoinbaseReadOnlyReadinessValidator validator = CreateValidator(
                canView,
                canTrade,
                canTransfer,
                canReceive);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => validator.ValidateAsync());

            StringAssert.Contains(exception.Message, expectedMessage);
        }

        private static CoinbaseReadOnlyReadinessValidator CreateValidator(
            bool canView,
            bool canTrade,
            bool canTransfer,
            bool canReceive)
        {
            string json =
                $$"""
                {
                  "can_view": {{canView.ToString().ToLowerInvariant()}},
                  "can_trade": {{canTrade.ToString().ToLowerInvariant()}},
                  "can_transfer": {{canTransfer.ToString().ToLowerInvariant()}},
                  "can_receive": {{canReceive.ToString().ToLowerInvariant()}},
                  "portfolio_uuid": "test-portfolio",
                  "portfolio_type": "DEFAULT"
                }
                """;
            CoinbaseAdvancedTradeAuthenticatedClient client = CreateClient(
                new CapturingHandler(JsonResponse(json)));
            return new CoinbaseReadOnlyReadinessValidator(client);
        }

        private static CoinbaseAdvancedTradeAuthenticatedClient CreateClient(
            CapturingHandler innerHandler)
        {
            CoinbaseJwtAuthenticationHandler authenticationHandler =
                new CoinbaseJwtAuthenticationHandler(new FixedJwtProvider())
                {
                    InnerHandler = innerHandler
                };
            return new CoinbaseAdvancedTradeAuthenticatedClient(
                new HttpClient(authenticationHandler));
        }

        private static HttpResponseMessage JsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private sealed class CapturingHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public CapturingHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            public Uri RequestUri { get; private set; }

            public string Authorization { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                RequestUri = request.RequestUri;
                Authorization = request.Headers.Authorization?.ToString();
                return Task.FromResult(_response);
            }
        }

        private sealed class FixedJwtProvider : ICoinbaseJwtProvider
        {
            public string CreateRestToken(HttpMethod method, Uri requestUri)
            {
                return "readiness-token";
            }

            public string CreateWebSocketToken()
            {
                return "readiness-token";
            }
        }
    }
}
