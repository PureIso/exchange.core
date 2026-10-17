using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using exchange.coinbase.advancedtrade;
using exchange.coinbase.advancedtrade.authentication;
using exchange.core.implementations;
using exchange.service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseAdvancedTradeRuntimeRegistrationTests
    {
        [TestMethod]
        public async Task DisabledRegistration_DoesNotReadCredentialsOrRegisterPlugin()
        {
            CountingCredentialSource credentials = new CountingCredentialSource();
            using CoinbaseAdvancedTradeRuntimeActivator activator =
                new CoinbaseAdvancedTradeRuntimeActivator(credentials);
            CoinbaseAdvancedTradePluginRegistrar registrar =
                new CoinbaseAdvancedTradePluginRegistrar(
                    new CoinbaseAdvancedTradeRegistrationOptions { Enabled = false },
                    activator);
            ExchangePluginService plugins = new ExchangePluginService();

            bool registered = await registrar.RegisterAsync(plugins, CancellationToken.None);

            Assert.IsFalse(registered);
            Assert.AreEqual(0, credentials.ReadCount);
            Assert.IsEmpty(plugins.PluginExchanges);
        }

        [TestMethod]
        public async Task EnabledRegistration_RequiresViewOnlyReadinessBeforeAddingPlugin()
        {
            CountingCredentialSource credentials = new CountingCredentialSource();
            ReadinessHandler handler = new ReadinessHandler(
                """
                {
                  "can_view": true,
                  "can_trade": false,
                  "can_transfer": false,
                  "can_receive": false
                }
                """);
            using CoinbaseAdvancedTradeRuntimeActivator activator =
                new CoinbaseAdvancedTradeRuntimeActivator(credentials, () => handler);
            CoinbaseAdvancedTradePluginRegistrar registrar =
                new CoinbaseAdvancedTradePluginRegistrar(
                    new CoinbaseAdvancedTradeRegistrationOptions { Enabled = true },
                    activator);
            ExchangePluginService plugins = new ExchangePluginService();

            bool registered = await registrar.RegisterAsync(plugins, CancellationToken.None);

            Assert.IsTrue(registered);
            Assert.AreEqual(1, credentials.ReadCount);
            Assert.AreEqual(
                "https://api.coinbase.com/api/v3/brokerage/key_permissions",
                handler.RequestUri.AbsoluteUri);
            Assert.IsTrue(handler.HadBearerToken);
            Assert.HasCount(1, plugins.PluginExchanges);
            Assert.IsInstanceOfType<CoinbaseAdvancedTrade>(plugins.PluginExchanges[0]);
            plugins.PluginExchanges[0].Dispose();
        }

        [TestMethod]
        public async Task EnabledRegistration_RejectsWriteCapableKeyWithoutAddingPlugin()
        {
            CountingCredentialSource credentials = new CountingCredentialSource();
            ReadinessHandler handler = new ReadinessHandler(
                """
                {
                  "can_view": true,
                  "can_trade": true,
                  "can_transfer": false,
                  "can_receive": false
                }
                """);
            using CoinbaseAdvancedTradeRuntimeActivator activator =
                new CoinbaseAdvancedTradeRuntimeActivator(credentials, () => handler);
            CoinbaseAdvancedTradePluginRegistrar registrar =
                new CoinbaseAdvancedTradePluginRegistrar(
                    new CoinbaseAdvancedTradeRegistrationOptions { Enabled = true },
                    activator);
            ExchangePluginService plugins = new ExchangePluginService();

            InvalidOperationException exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    registrar.RegisterAsync(plugins, CancellationToken.None));

            StringAssert.Contains(exception.Message, "read-only deployment");
            Assert.AreEqual(1, credentials.ReadCount);
            Assert.IsEmpty(plugins.PluginExchanges);
        }

        [TestMethod]
        public async Task PublicDataRegistration_DoesNotReadCredentialsOrCreateAuthenticatedClients()
        {
            CountingCredentialSource credentials = new CountingCredentialSource();
            RecordingPublicHandler publicHandler = new RecordingPublicHandler();
            using CoinbaseAdvancedTradeRuntimeActivator activator =
                new CoinbaseAdvancedTradeRuntimeActivator(
                    credentials,
                    () => new HttpClientHandler(),
                    () => new HttpClient(publicHandler));
            CoinbaseAdvancedTradePluginRegistrar registrar =
                new CoinbaseAdvancedTradePluginRegistrar(
                    new CoinbaseAdvancedTradeRegistrationOptions
                    {
                        PublicDataEnabled = true
                    },
                    activator);
            ExchangePluginService plugins = new ExchangePluginService();

            bool registered = await registrar.RegisterAsync(plugins, CancellationToken.None);

            Assert.IsTrue(registered);
            Assert.AreEqual(0, credentials.ReadCount);
            Assert.IsEmpty(publicHandler.Requests);
            Assert.HasCount(1, plugins.PluginExchanges);
            CoinbaseAdvancedTrade plugin =
                (CoinbaseAdvancedTrade)plugins.PluginExchanges[0];
            FieldInfo authenticatedClientField = typeof(CoinbaseAdvancedTrade)
                .GetField("_authenticatedClient", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo userWebSocketField = typeof(CoinbaseAdvancedTrade)
                .GetField("_userOrderWebSocket", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNull(authenticatedClientField?.GetValue(plugin));
            Assert.IsNull(userWebSocketField?.GetValue(plugin));
            plugin.Dispose();
        }

        [TestMethod]
        public async Task PublicDataRegistration_FailsClosedForAccountHistoryAndWrites()
        {
            using CoinbaseAdvancedTradeRuntimeActivator activator =
                new CoinbaseAdvancedTradeRuntimeActivator(new CountingCredentialSource());
            CoinbaseAdvancedTradePluginRegistrar registrar =
                new CoinbaseAdvancedTradePluginRegistrar(
                    new CoinbaseAdvancedTradeRegistrationOptions
                    {
                        PublicDataEnabled = true
                    },
                    activator);
            ExchangePluginService plugins = new ExchangePluginService();

            await registrar.RegisterAsync(plugins, CancellationToken.None);
            CoinbaseAdvancedTrade plugin =
                (CoinbaseAdvancedTrade)plugins.PluginExchanges[0];

            await Assert.ThrowsAsync<NotSupportedException>(
                () => plugin.UpdateAccountsAsync());
            await Assert.ThrowsAsync<NotSupportedException>(
                () => plugin.UpdateOrdersAsync());
            await Assert.ThrowsAsync<NotSupportedException>(
                () => plugin.UpdateFillsAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(
                () => plugin.CancelAllOrdersAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(
                () => plugin.CancelOrderAsync(null));
            await Assert.ThrowsAsync<NotSupportedException>(
                () => plugin.PostOrdersAsync(null));
            plugin.Dispose();
        }

        [TestMethod]
        public async Task AuthenticatedAndPublicProfilesCannotBeEnabledTogether()
        {
            using CoinbaseAdvancedTradeRuntimeActivator activator =
                new CoinbaseAdvancedTradeRuntimeActivator(new CountingCredentialSource());
            CoinbaseAdvancedTradePluginRegistrar registrar =
                new CoinbaseAdvancedTradePluginRegistrar(
                    new CoinbaseAdvancedTradeRegistrationOptions
                    {
                        Enabled = true,
                        PublicDataEnabled = true
                    },
                    activator);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => registrar.RegisterAsync(new ExchangePluginService(), CancellationToken.None));

            StringAssert.Contains(exception.Message, "mutually exclusive");
        }

        [TestMethod]
        public void RuntimeManagedPlugin_IsExcludedFromGenericFolderScanning()
        {
            string temporaryParent = Path.Combine(
                Path.GetTempPath(),
                "exchange-core-runtime-registration-tests");
            string pluginDirectory = Path.Combine(
                temporaryParent,
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pluginDirectory);
            try
            {
                string assemblyPath = typeof(CoinbaseAdvancedTrade).Assembly.Location;
                File.Copy(
                    assemblyPath,
                    Path.Combine(pluginDirectory, Path.GetFileName(assemblyPath)));
                ExchangePluginService plugins = new ExchangePluginService();

                plugins.AddPluginFromFolder(
                    pluginDirectory,
                    pluginType => !string.Equals(
                        pluginType.Assembly.GetName().Name,
                        "exchange.coinbase.advancedtrade",
                        StringComparison.OrdinalIgnoreCase));

                Assert.IsEmpty(plugins.PluginExchanges);
            }
            finally
            {
                string safeParent = Path.GetFullPath(temporaryParent)
                    .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string fullPluginDirectory = Path.GetFullPath(pluginDirectory);
                if (fullPluginDirectory.StartsWith(
                        safeParent,
                        StringComparison.OrdinalIgnoreCase) &&
                    Directory.Exists(fullPluginDirectory))
                {
                    Directory.Delete(fullPluginDirectory, recursive: true);
                }
            }
        }

        private sealed class CountingCredentialSource : ICoinbaseCredentialSource
        {
            private readonly CoinbaseApiCredentials _credentials;

            public CountingCredentialSource()
            {
                using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                _credentials = new CoinbaseApiCredentials(
                    "organizations/test/apiKeys/read-only",
                    key.ExportPkcs8PrivateKeyPem());
            }

            public int ReadCount { get; private set; }

            public CoinbaseApiCredentials GetCredentials()
            {
                ReadCount++;
                return _credentials;
            }
        }

        private sealed class ReadinessHandler : HttpMessageHandler
        {
            private readonly string _json;

            public ReadinessHandler(string json)
            {
                _json = json;
            }

            public bool HadBearerToken { get; private set; }

            public Uri RequestUri { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                RequestUri = request.RequestUri;
                HadBearerToken = request.Headers.Authorization?.Scheme == "Bearer" &&
                    !string.IsNullOrWhiteSpace(request.Headers.Authorization.Parameter);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_json, Encoding.UTF8, "application/json")
                });
            }
        }

        private sealed class RecordingPublicHandler : HttpMessageHandler
        {
            public List<Uri> Requests { get; } = new List<Uri>();

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(request.RequestUri);
                throw new InvalidOperationException(
                    "The public registration test must not perform REST requests.");
            }
        }
    }
}
