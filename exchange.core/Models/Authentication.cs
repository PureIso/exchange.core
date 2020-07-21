using exchange.core.Interfaces;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace exchange.core.models
{
    public class Authentication : IAuthentication
    {

        #region Properties

        public string ApiKey { get; set; }
        public string Secret { get; set; }
        public string Passphrase { get; set; }
        public string EndpointUrl { get; set; }
        public Uri WebSocketUri { get; set; }
        #endregion

        public Authentication()
        {
        }
        public Authentication(string apiKey, string passphrase, string secret, string endpointUrl, string webSocketUri)
        {
            ApiKey = apiKey;
            if (passphrase == "PASSPHRASE")
                passphrase = null;
            Passphrase = passphrase;
            EndpointUrl = endpointUrl;
            WebSocketUri = new Uri(webSocketUri);
            Secret = secret;
        }
        #region Public Methods
        public IAuthenticationSignature ComputeSignature(IRequest request)
        {
            request.TimeStamp = DateTime.UtcNow.ToUnixTimestamp();
            string timestamp = request.TimeStamp.ToString(CultureInfo.InvariantCulture);
            string prehash = timestamp + request.Method + request.RequestUrl + request.RequestBody;
            byte[] data = Convert.FromBase64String(Secret);
            AuthenticationSignature authenticationSignature = new AuthenticationSignature
            {
                Signature = HashString(prehash, data),
                Timestamp = timestamp
            };
            return authenticationSignature;
        }

        public string ComputeSignature(string message)
        {
            byte[] key = Encoding.UTF8.GetBytes(Secret);
            string stringHash;
            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                stringHash = BitConverter.ToString(hash).Replace("-", "");
            }
            string signature = $"{message}&signature={stringHash}";
            return signature;
        }
        #endregion

        #region Private Methods

        public string HashString(string prehashString, byte[] secret)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(prehashString);
            using HMACSHA256 hmac = new HMACSHA256(secret);
            byte[] hash = hmac.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        #endregion

    }
}
