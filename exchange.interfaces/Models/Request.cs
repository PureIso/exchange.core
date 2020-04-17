using exchange.core.Interfaces;
using System;
using System.Net.Http;
using System.Text;

namespace exchange.core.models
{
    public class Request : IRequest
    {
        #region Properties
        public string Method { get; }
        public string RequestBody { get; set; }
        public string RequestUrl { get; set; }
        public long TimeStamp { get; set; }
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
            return !string.IsNullOrEmpty(RequestBody) ? new StringContent(RequestBody, Encoding.UTF8, contentType) : null;
        }
    }
}
