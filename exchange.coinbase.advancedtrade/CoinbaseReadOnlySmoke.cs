using System;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.coinbase.advancedtrade
{
    public sealed class CoinbaseReadOnlySmokeGate
    {
        public const string EnabledVariable =
            "COINBASE_ADVANCED_TRADE_READ_ONLY_SMOKE_TESTS";
        public const string ProductIdVariable =
            "COINBASE_ADVANCED_TRADE_SMOKE_PRODUCT_ID";
        public const string DefaultProductId = "BTC-USD";

        private readonly Func<string, string> _environmentReader;

        public CoinbaseReadOnlySmokeGate(Func<string, string> environmentReader = null)
        {
            _environmentReader = environmentReader ?? Environment.GetEnvironmentVariable;
        }

        public bool IsEnabled()
        {
            return bool.TryParse(
                       _environmentReader(EnabledVariable),
                       out bool enabled) &&
                   enabled;
        }

        public string GetProductId()
        {
            string productId = _environmentReader(ProductIdVariable)?.Trim();
            return string.IsNullOrWhiteSpace(productId) ? DefaultProductId : productId;
        }
    }

    public sealed class CoinbaseReadOnlySmokeResult
    {
        public bool ProductValidated { get; init; }

        public int AccountCount { get; init; }

        public int OrderCount { get; init; }

        public int FillCount { get; init; }

        public override string ToString()
        {
            return $"Coinbase read-only smoke completed: product={ProductValidated}, " +
                   $"accounts={AccountCount}, orders={OrderCount}, fills={FillCount}.";
        }
    }

    public sealed class CoinbaseReadOnlySmokeRunner
    {
        private readonly CoinbaseAdvancedTradeAuthenticatedClient _authenticatedClient;
        private readonly CoinbaseAdvancedTradeClient _publicClient;
        private readonly CoinbaseReadOnlyReadinessValidator _readinessValidator;

        public CoinbaseReadOnlySmokeRunner(
            CoinbaseAdvancedTradeClient publicClient,
            CoinbaseAdvancedTradeAuthenticatedClient authenticatedClient)
        {
            _publicClient = publicClient ?? throw new ArgumentNullException(nameof(publicClient));
            _authenticatedClient = authenticatedClient ??
                throw new ArgumentNullException(nameof(authenticatedClient));
            _readinessValidator = new CoinbaseReadOnlyReadinessValidator(authenticatedClient);
        }

        public async Task<CoinbaseReadOnlySmokeResult> RunAsync(
            string productId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException(
                    "A product ID is required for the read-only smoke test.",
                    nameof(productId));

            await _readinessValidator.ValidateAsync(cancellationToken);
            await _publicClient.GetProductAsync(productId, cancellationToken);
            var accounts = await _authenticatedClient.ListAccountsAsync(cancellationToken);
            var orders = await _authenticatedClient.ListOrdersAsync(productId, cancellationToken);
            var fills = await _authenticatedClient.ListFillsAsync(productId, cancellationToken);

            return new CoinbaseReadOnlySmokeResult
            {
                ProductValidated = true,
                AccountCount = accounts.Count,
                OrderCount = orders.Count,
                FillCount = fills.Count
            };
        }
    }
}
