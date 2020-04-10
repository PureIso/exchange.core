using exchange.coinbase.models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace exchange.coinbase
{
    public class Authentication
    {

        #region Fields

        private readonly string _secret;

        #endregion

        #region Properties

        public string ApiKey { get; }
        public string Passphrase { get; }
        public string EndpointUrl { get; }
        public Uri ExchangeUri { get; }
        #endregion

        public Authentication(string apiKey, string passphrase, string secret, string endpointUrl, string uri)
        {
            ApiKey = apiKey;
            Passphrase = passphrase;
            EndpointUrl = endpointUrl;
            ExchangeUri = new Uri(uri);
            _secret = secret;
        }
        #region Public Methods
        public AuthenticationSignature ComputeSignature(Request request)
        {
            string timestamp = request.TimeStamp.ToString(CultureInfo.InvariantCulture);
            string prehash = timestamp + request.Method + request.RequestUrl + request.RequestBody;
            byte[] data = Convert.FromBase64String(_secret);
            AuthenticationSignature authenticationSignature = new AuthenticationSignature
            {
                Signature = HashString(prehash, data),
                Timestamp = timestamp
            };
            return authenticationSignature;
        }
        #endregion

        #region Private Methods

        private static string HashString(string prehashString, byte[] secret)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(prehashString);
            using (HMACSHA256 hmac = new HMACSHA256(secret))
            {
                byte[] hash = hmac.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        #endregion

    }
}
