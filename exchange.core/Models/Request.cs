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
        public string RequestQuery { get; set; }
        public string RequestBody { get; set; }
        public string RequestUrl { get; set; }
        public string RequestSignature { get; set; }
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
        public string Compose()
        {
            string composedString = $"{RequestUrl}{RequestQuery}{RequestSignature}".Replace("??", "?");
            return composedString;
        }

        public Uri ComposeRequestUriAbsolute(string endpointUrl)
        {
            return new Uri(new Uri(endpointUrl), Compose().Replace("??","?"));
        }

        public StringContent GetRequestBody(string contentType = "application/json")
        {
            return !string.IsNullOrEmpty(RequestBody) ? new StringContent(RequestBody, Encoding.UTF8, contentType) : null;
        }
    }
}
