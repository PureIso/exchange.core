using System;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.coinbase.advancedtrade
{
    public sealed class CoinbaseReadOnlyReadinessValidator
    {
        private readonly CoinbaseAdvancedTradeAuthenticatedClient _client;

        public CoinbaseReadOnlyReadinessValidator(CoinbaseAdvancedTradeAuthenticatedClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<CoinbaseApiKeyPermissions> ValidateAsync(
            CancellationToken cancellationToken = default)
        {
            CoinbaseApiKeyPermissions permissions =
                await _client.GetApiKeyPermissionsAsync(cancellationToken);

            if (!permissions.CanView)
                throw new InvalidOperationException(
                    "The Coinbase API key is not ready: view permission is required.");
            if (permissions.CanTrade || permissions.CanTransfer || permissions.CanReceive)
                throw new InvalidOperationException(
                    "The Coinbase API key is not ready for read-only deployment: trade, transfer, and receive permissions must be disabled.");

            return permissions;
        }
    }
}
