using System;
using System.Security.Cryptography;
using System.Text;
using exchange.core.connectivity;
using exchange.core.utilities;

namespace exchange.core.implementations.connectivity
{
    public abstract class AbstractAuthentication : IAuthentication
    {
        #region Fields
        private string _secret;
        #endregion

        #region Properties
        public string APIKey { get; private set; }
        public string PassPhrase { get; private set; }
        public string Signature { get; private set; }
        public string TimeStamp { get; private set; }
        public Uri Uri { get; private set; }
        #endregion

        #region Methods
        public virtual void Initialise(string apiKey, string passPhrase, string secret, Uri uri)
        {
            APIKey = apiKey;
            PassPhrase = passPhrase;
            Uri = uri;
            _secret = secret;
        }
        public virtual string ComputeSignature(string key)
        {
            string timestamp =
                $"{(string.IsNullOrWhiteSpace(key) ? "?" : "&")}timestamp={DateTime.Now.ToUniversalTime().GenerateUnixStringTimeStamp()}";
            key += timestamp;
            byte[] secret = Encoding.UTF8.GetBytes(_secret);
            return $"{timestamp}&signature={ComputeHash(key, secret)}";
        }
        public virtual void ComputeSignature(IRequest request)
        {
            TimeStamp = request.TimeStamp;
            string message = request.TimeStamp + request.Method + request.Url + request.Body;
            byte[] secret = Convert.FromBase64String(_secret);
            Signature = ComputeHash(message, secret);
        }
        public virtual string ComputeHash(string key, byte[] secret)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(key);
            using (HMACSHA256 hmac = new HMACSHA256(secret))
            {
                byte[] hash = hmac.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        #endregion
    }
}
