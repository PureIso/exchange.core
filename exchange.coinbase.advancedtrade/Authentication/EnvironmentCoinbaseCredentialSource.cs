using System;

namespace exchange.coinbase.advancedtrade.authentication
{
    public sealed class EnvironmentCoinbaseCredentialSource : ICoinbaseCredentialSource
    {
        public const string DefaultApiKeyNameVariable = "COINBASE_ADVANCED_TRADE_API_KEY_NAME";
        public const string DefaultPrivateKeyVariable = "COINBASE_ADVANCED_TRADE_PRIVATE_KEY";

        private readonly string _apiKeyNameVariable;
        private readonly Func<string, string> _environmentReader;
        private readonly string _privateKeyVariable;

        public EnvironmentCoinbaseCredentialSource(
            string apiKeyNameVariable = DefaultApiKeyNameVariable,
            string privateKeyVariable = DefaultPrivateKeyVariable,
            Func<string, string> environmentReader = null)
        {
            if (string.IsNullOrWhiteSpace(apiKeyNameVariable))
                throw new ArgumentException("An API key-name environment variable is required.", nameof(apiKeyNameVariable));
            if (string.IsNullOrWhiteSpace(privateKeyVariable))
                throw new ArgumentException("A private-key environment variable is required.", nameof(privateKeyVariable));

            _apiKeyNameVariable = apiKeyNameVariable;
            _privateKeyVariable = privateKeyVariable;
            _environmentReader = environmentReader ?? Environment.GetEnvironmentVariable;
        }

        public CoinbaseApiCredentials GetCredentials()
        {
            string apiKeyName = ReadRequired(_apiKeyNameVariable);
            string privateKeyPem = ReadRequired(_privateKeyVariable);
            return new CoinbaseApiCredentials(apiKeyName, privateKeyPem);
        }

        private string ReadRequired(string variableName)
        {
            string value = _environmentReader(variableName);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    $"The required environment variable '{variableName}' is not configured.");
            return value;
        }
    }
}
