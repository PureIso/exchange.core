using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using exchange.coinbase.advancedtrade;
using exchange.coinbase.advancedtrade.authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseJwtProviderTests
    {
        private static readonly DateTimeOffset FixedTime =
            new DateTimeOffset(2026, 8, 15, 12, 30, 45, TimeSpan.Zero);

        [TestMethod]
        public void CreateRestToken_BuildsAndSignsCoinbaseClaims()
        {
            using ECDsa verificationKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            string privateKey = verificationKey.ExportECPrivateKeyPem().Replace("\n", "\\n");
            CoinbaseAdvancedTradeOptions options = CreateOptions(privateKey);
            using CoinbaseJwtProvider provider = new CoinbaseJwtProvider(
                options,
                new FixedClock(FixedTime),
                new FixedNonceProvider("00112233445566778899aabbccddeeff"));

            string token = provider.CreateRestToken(
                HttpMethod.Post,
                new Uri("https://api.coinbase.com/api/v3/brokerage/orders?preview=true"));

            string[] segments = token.Split('.');
            Assert.HasCount(3, segments);
            using JsonDocument header = DecodeJson(segments[0]);
            Assert.AreEqual("ES256", header.RootElement.GetProperty("alg").GetString());
            Assert.AreEqual(options.ApiKeyName, header.RootElement.GetProperty("kid").GetString());
            Assert.AreEqual("00112233445566778899aabbccddeeff",
                header.RootElement.GetProperty("nonce").GetString());
            Assert.AreEqual("JWT", header.RootElement.GetProperty("typ").GetString());

            using JsonDocument claims = DecodeJson(segments[1]);
            long now = FixedTime.ToUnixTimeSeconds();
            Assert.AreEqual("cdp", claims.RootElement.GetProperty("iss").GetString());
            Assert.AreEqual(options.ApiKeyName, claims.RootElement.GetProperty("sub").GetString());
            Assert.AreEqual(now, claims.RootElement.GetProperty("nbf").GetInt64());
            Assert.AreEqual(now + 120, claims.RootElement.GetProperty("exp").GetInt64());
            Assert.AreEqual(
                "POST api.coinbase.com/api/v3/brokerage/orders",
                claims.RootElement.GetProperty("uri").GetString());

            byte[] signature = Base64UrlDecode(segments[2]);
            Assert.HasCount(64, signature);
            Assert.IsTrue(verificationKey.VerifyData(
                Encoding.ASCII.GetBytes($"{segments[0]}.{segments[1]}"),
                signature,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }

        [TestMethod]
        public void CreateWebSocketToken_OmitsRequestUri()
        {
            using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using CoinbaseJwtProvider provider = new CoinbaseJwtProvider(
                CreateOptions(key.ExportECPrivateKeyPem()),
                new FixedClock(FixedTime),
                new FixedNonceProvider("websocket-nonce"));

            string token = provider.CreateWebSocketToken();

            string[] segments = token.Split('.');
            using JsonDocument header = DecodeJson(segments[0]);
            using JsonDocument claims = DecodeJson(segments[1]);
            Assert.AreEqual("websocket-nonce", header.RootElement.GetProperty("nonce").GetString());
            Assert.IsFalse(claims.RootElement.TryGetProperty("uri", out _));
            Assert.AreEqual(
                claims.RootElement.GetProperty("nbf").GetInt64() + 120,
                claims.RootElement.GetProperty("exp").GetInt64());
        }

        [TestMethod]
        public void CreateTokens_RequestsANewNonceForEveryToken()
        {
            using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            SequenceNonceProvider nonces = new SequenceNonceProvider("nonce-one", "nonce-two");
            using CoinbaseJwtProvider provider = new CoinbaseJwtProvider(
                CreateOptions(key.ExportECPrivateKeyPem()),
                new FixedClock(FixedTime),
                nonces);

            string first = provider.CreateWebSocketToken();
            string second = provider.CreateWebSocketToken();

            using JsonDocument firstHeader = DecodeJson(first.Split('.')[0]);
            using JsonDocument secondHeader = DecodeJson(second.Split('.')[0]);
            Assert.AreEqual("nonce-one", firstHeader.RootElement.GetProperty("nonce").GetString());
            Assert.AreEqual("nonce-two", secondHeader.RootElement.GetProperty("nonce").GetString());
            Assert.AreNotEqual(first, second);
        }

        [TestMethod]
        public void Constructor_RejectsUnsupportedKeysWithoutExposingSecret()
        {
            const string secret = "TOP-SECRET-PRIVATE-KEY";
            CoinbaseAdvancedTradeOptions invalidOptions = CreateOptions(secret);

            ArgumentException invalidKeyException = Assert.Throws<ArgumentException>(
                () => new CoinbaseJwtProvider(invalidOptions));

            Assert.IsFalse(invalidKeyException.ToString().Contains(secret, StringComparison.Ordinal));

            using ECDsa p384Key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
            Assert.Throws<ArgumentException>(() =>
                new CoinbaseJwtProvider(CreateOptions(p384Key.ExportECPrivateKeyPem())));
        }

        [TestMethod]
        public void Provider_ValidatesRequestsAndDisposal()
        {
            using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CoinbaseJwtProvider provider = new CoinbaseJwtProvider(
                CreateOptions(key.ExportECPrivateKeyPem()),
                new FixedClock(FixedTime),
                new FixedNonceProvider("nonce"));

            Assert.Throws<ArgumentNullException>(() => provider.CreateRestToken(null, new Uri("https://api.coinbase.com")));
            Assert.Throws<ArgumentException>(() =>
                provider.CreateRestToken(HttpMethod.Get, new Uri("http://api.coinbase.com/accounts")));

            provider.Dispose();
            Assert.Throws<ObjectDisposedException>(() => provider.CreateWebSocketToken());
        }

        private static CoinbaseAdvancedTradeOptions CreateOptions(string privateKey)
        {
            return new CoinbaseAdvancedTradeOptions
            {
                ApiKeyName = "organizations/test-org/apiKeys/test-key",
                PrivateKeyPem = privateKey
            };
        }

        private static JsonDocument DecodeJson(string value)
        {
            return JsonDocument.Parse(Base64UrlDecode(value));
        }

        private static byte[] Base64UrlDecode(string value)
        {
            string base64 = value.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            return Convert.FromBase64String(base64);
        }

        private sealed class FixedClock : ICoinbaseClock
        {
            public FixedClock(DateTimeOffset utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTimeOffset UtcNow { get; }
        }

        private sealed class FixedNonceProvider : ICoinbaseNonceProvider
        {
            private readonly string _nonce;

            public FixedNonceProvider(string nonce)
            {
                _nonce = nonce;
            }

            public string CreateNonce()
            {
                return _nonce;
            }
        }

        private sealed class SequenceNonceProvider : ICoinbaseNonceProvider
        {
            private readonly Queue<string> _nonces;

            public SequenceNonceProvider(params string[] nonces)
            {
                _nonces = new Queue<string>(nonces);
            }

            public string CreateNonce()
            {
                return _nonces.Dequeue();
            }
        }
    }
}
