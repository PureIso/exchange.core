using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase.advancedtrade;
using exchange.coinbase.advancedtrade.authentication;
using exchange.coinbase.advancedtrade.websockets;
using exchange.core.implementations;
using exchange.core.interfaces;

namespace exchange.service
{
    public sealed class CoinbaseAdvancedTradeRegistrationOptions
    {
        public bool Enabled { get; init; }
    }

    public interface ICoinbaseAdvancedTradeRuntimeActivator
    {
        Task<AbstractExchangePlugin> CreateValidatedPluginAsync(
            CancellationToken cancellationToken);
    }

    public sealed class CoinbaseAdvancedTradeRuntimeActivator :
        ICoinbaseAdvancedTradeRuntimeActivator,
        IDisposable
    {
        private readonly ICoinbaseCredentialSource _credentialSource;
        private readonly Func<HttpMessageHandler> _authenticatedHandlerFactory;
        private readonly List<IDisposable> _ownedResources = new List<IDisposable>();
        private bool _disposed;

        public CoinbaseAdvancedTradeRuntimeActivator(
            ICoinbaseCredentialSource credentialSource)
            : this(credentialSource, () => new HttpClientHandler())
        {
        }

        public CoinbaseAdvancedTradeRuntimeActivator(
            ICoinbaseCredentialSource credentialSource,
            Func<HttpMessageHandler> authenticatedHandlerFactory)
        {
            _credentialSource = credentialSource ??
                throw new ArgumentNullException(nameof(credentialSource));
            _authenticatedHandlerFactory = authenticatedHandlerFactory ??
                throw new ArgumentNullException(nameof(authenticatedHandlerFactory));
        }

        public async Task<AbstractExchangePlugin> CreateValidatedPluginAsync(
            CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    nameof(CoinbaseAdvancedTradeRuntimeActivator));

            CoinbaseApiCredentials credentials = _credentialSource.GetCredentials();
            CoinbaseJwtProvider jwtProvider = null;
            HttpClient authenticatedHttpClient = null;
            HttpClient publicHttpClient = null;
            try
            {
                CoinbaseAdvancedTradeOptions options = new CoinbaseAdvancedTradeOptions();
                jwtProvider = new CoinbaseJwtProvider(credentials);
                CoinbaseJwtAuthenticationHandler authenticationHandler =
                    new CoinbaseJwtAuthenticationHandler(jwtProvider)
                    {
                        InnerHandler = _authenticatedHandlerFactory()
                            ?? throw new InvalidOperationException(
                                "The Coinbase authenticated HTTP handler factory returned null.")
                    };
                authenticatedHttpClient = new HttpClient(authenticationHandler);
                CoinbaseAdvancedTradeAuthenticatedClient authenticatedClient =
                    new CoinbaseAdvancedTradeAuthenticatedClient(
                        authenticatedHttpClient,
                        options);

                CoinbaseReadOnlyReadinessValidator readiness =
                    new CoinbaseReadOnlyReadinessValidator(authenticatedClient);
                await readiness.ValidateAsync(cancellationToken);

                publicHttpClient = new HttpClient();
                CoinbaseAdvancedTrade plugin = new CoinbaseAdvancedTrade(
                    new CoinbaseAdvancedTradeClient(publicHttpClient, options),
                    authenticatedClient,
                    new CoinbaseAdvancedTradeMarketDataWebSocket(options),
                    new CoinbaseAdvancedTradeUserOrderWebSocket(jwtProvider, options));
                _ownedResources.Add(publicHttpClient);
                _ownedResources.Add(authenticatedHttpClient);
                _ownedResources.Add(jwtProvider);
                return plugin;
            }
            catch
            {
                publicHttpClient?.Dispose();
                authenticatedHttpClient?.Dispose();
                jwtProvider?.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            for (int index = _ownedResources.Count - 1; index >= 0; index--)
                _ownedResources[index].Dispose();
            _ownedResources.Clear();
        }
    }

    public sealed class CoinbaseAdvancedTradePluginRegistrar
    {
        private readonly ICoinbaseAdvancedTradeRuntimeActivator _activator;
        private readonly CoinbaseAdvancedTradeRegistrationOptions _options;

        public CoinbaseAdvancedTradePluginRegistrar(
            CoinbaseAdvancedTradeRegistrationOptions options,
            ICoinbaseAdvancedTradeRuntimeActivator activator)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _activator = activator ?? throw new ArgumentNullException(nameof(activator));
        }

        public async Task<bool> RegisterAsync(
            IExchangePluginService pluginService,
            CancellationToken cancellationToken)
        {
            if (pluginService == null)
                throw new ArgumentNullException(nameof(pluginService));
            if (!_options.Enabled)
                return false;

            AbstractExchangePlugin plugin =
                await _activator.CreateValidatedPluginAsync(cancellationToken);
            pluginService.PluginExchanges.Add(plugin);
            return true;
        }
    }
}
