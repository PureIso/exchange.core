using System;
using System.Net.Http;
using System.Text;

namespace exchange.coinbase.models
{
    public class Request
    {
        #region Properties

        public bool IsExpired => GetCurrentUnixTimeStamp() - TimeStamp >= 30;

        public string Method { get; }

        public string RequestBody { get; set; }

        public string RequestUrl { get; set; }

        public double TimeStamp { get; }
        public Uri AbsoluteUri { get; set; }

        #endregion

        public Request(string endpointUrl, string method, string requestUrl)
        {
            Method = method;
            RequestUrl = requestUrl;
            AbsoluteUri = new Uri(new Uri(endpointUrl), RequestUrl);
            TimeStamp = DateTime.UtcNow.ToUnixTimestamp();

            
        }
        public StringContent GetRequestBody(string contentType = "application/json")
        {
            if (!string.IsNullOrEmpty(RequestBody))
                return new StringContent(RequestBody, Encoding.UTF8, contentType);
            return null;
        }
        #region Private Methods

        private static double GetCurrentUnixTimeStamp()
        {
            return DateTime.UtcNow.ToUnixTimestamp();
        }

        #endregion
    }
}
