using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace exchange.coinbase.advancedtrade.authentication
{
    public sealed class CoinbaseJwtProvider : ICoinbaseJwtProvider, IDisposable
    {
        private const string P256ObjectIdentifier = "1.2.840.10045.3.1.7";
        private const long LifetimeSeconds = 120;

        private readonly ICoinbaseClock _clock;
        private readonly ECDsa _key;
        private readonly string _keyName;
        private readonly object _keyLock = new object();
        private readonly ICoinbaseNonceProvider _nonceProvider;
        private bool _disposed;

        public CoinbaseJwtProvider(
            CoinbaseAdvancedTradeOptions options,
            ICoinbaseClock clock = null,
            ICoinbaseNonceProvider nonceProvider = null)
            : this(GetCredentials(options), clock, nonceProvider)
        {
        }

        public CoinbaseJwtProvider(
            CoinbaseApiCredentials credentials,
            ICoinbaseClock clock = null,
            ICoinbaseNonceProvider nonceProvider = null)
        {
            if (credentials == null)
                throw new ArgumentNullException(nameof(credentials));

            _keyName = credentials.ApiKeyName;
            _clock = clock ?? SystemCoinbaseClock.Instance;
            _nonceProvider = nonceProvider ?? CryptographicCoinbaseNonceProvider.Instance;
            _key = ImportPrivateKey(credentials.PrivateKeyPem);
        }

        public string CreateRestToken(HttpMethod method, Uri requestUri)
        {
            ThrowIfDisposed();
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (requestUri == null)
                throw new ArgumentNullException(nameof(requestUri));
            if (!requestUri.IsAbsoluteUri || requestUri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("A Coinbase REST token requires an absolute HTTPS URI.", nameof(requestUri));

            string host = requestUri.IsDefaultPort ? requestUri.IdnHost : requestUri.Authority;
            string uriClaim = $"{method.Method.ToUpperInvariant()} {host}{requestUri.AbsolutePath}";
            return CreateToken(uriClaim);
        }

        public string CreateWebSocketToken()
        {
            ThrowIfDisposed();
            return CreateToken(null);
        }

        public void Dispose()
        {
            lock (_keyLock)
            {
                if (_disposed)
                    return;
                _key.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        private string CreateToken(string uriClaim)
        {
            long now = _clock.UtcNow.ToUnixTimeSeconds();
            string nonce = _nonceProvider.CreateNonce();
            if (string.IsNullOrWhiteSpace(nonce))
                throw new InvalidOperationException("The Coinbase nonce provider returned an empty nonce.");

            Dictionary<string, object> header = new Dictionary<string, object>
            {
                ["alg"] = "ES256",
                ["kid"] = _keyName,
                ["nonce"] = nonce,
                ["typ"] = "JWT"
            };
            Dictionary<string, object> claims = new Dictionary<string, object>
            {
                ["iss"] = "cdp",
                ["nbf"] = now,
                ["exp"] = checked(now + LifetimeSeconds),
                ["sub"] = _keyName
            };
            if (uriClaim != null)
                claims["uri"] = uriClaim;

            string encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
            string encodedClaims = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(claims));
            string signingInput = $"{encodedHeader}.{encodedClaims}";
            byte[] signingBytes = Encoding.ASCII.GetBytes(signingInput);
            byte[] signature;

            lock (_keyLock)
            {
                ThrowIfDisposed();
                signature = _key.SignData(
                    signingBytes,
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            }

            return $"{signingInput}.{Base64UrlEncode(signature)}";
        }

        private static ECDsa ImportPrivateKey(string privateKeyPem)
        {
            ECDsa key = ECDsa.Create();
            try
            {
                key.ImportFromPem(privateKeyPem.Replace("\\n", "\n"));
                ECParameters parameters = key.ExportParameters(true);
                if (parameters.Curve.Oid.Value != P256ObjectIdentifier || parameters.D == null)
                    throw new CryptographicException();
                return key;
            }
            catch (Exception exception) when (exception is ArgumentException or CryptographicException)
            {
                key.Dispose();
                throw new ArgumentException(
                    "The Coinbase private key must be an unencrypted ECDSA P-256 PEM private key.",
                    nameof(privateKeyPem));
            }
        }

        private static CoinbaseApiCredentials GetCredentials(CoinbaseAdvancedTradeOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            options.ValidateAuthentication();
            return new CoinbaseApiCredentials(options.ApiKeyName, options.PrivateKeyPem);
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
