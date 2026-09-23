using System;

namespace exchange.coinbase.advancedtrade.authentication
{
    public sealed class CoinbaseApiCredentials
    {
        public CoinbaseApiCredentials(string apiKeyName, string privateKeyPem)
        {
            if (string.IsNullOrWhiteSpace(apiKeyName))
                throw new ArgumentException("The Coinbase API key name is required.", nameof(apiKeyName));
            if (string.IsNullOrWhiteSpace(privateKeyPem))
                throw new ArgumentException("The Coinbase private key is required.", nameof(privateKeyPem));

            ApiKeyName = apiKeyName;
            PrivateKeyPem = privateKeyPem;
        }

        public string ApiKeyName { get; }

        public string PrivateKeyPem { get; }

        public override string ToString()
        {
            return nameof(CoinbaseApiCredentials);
        }
    }
}
