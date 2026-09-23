using System;
using System.Security.Cryptography;

namespace exchange.coinbase.advancedtrade.authentication
{
    internal sealed class CryptographicCoinbaseNonceProvider : ICoinbaseNonceProvider
    {
        public static CryptographicCoinbaseNonceProvider Instance { get; } =
            new CryptographicCoinbaseNonceProvider();

        private CryptographicCoinbaseNonceProvider()
        {
        }

        public string CreateNonce()
        {
            Span<byte> bytes = stackalloc byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
