using System;
using System.Net.Http;
using exchange.core.connectivity;

namespace exchange.core.implementations.connectivity
{
    public abstract class AbstractRequest : IRequest
    {
        #region Properties
        public virtual bool Sign { get; set; }
        public virtual string Query { get; set; }
        public virtual Uri Uri { get; set; }
        public virtual Uri AbsoluteUri { get; set; }
        public virtual string Url { get; set; }
        public virtual string Body { get; set; }
        public virtual string ContentType { get; set; }
        public virtual string Method { get; set; }
        public virtual string TimeStamp { get; set; }
        #endregion

        #region Methods
        public virtual void Compose(string signature)
        {
            if (Sign)
            {
                string composedUrl = $"{Url}{Query}{signature}";
                AbsoluteUri = new Uri(Uri, composedUrl);
            }
            else
            {
                string composedUrl = string.IsNullOrEmpty(Query) ? $"{Url}{Query}" : $"{Url}";
                AbsoluteUri = new Uri(Uri, composedUrl);
            }
        }
        public virtual HttpClient ComposeDefaultRequestHeader(HttpClient httpClient, IAuthentication authentication)
        {
            httpClient.DefaultRequestHeaders.Add("CB-ACCESS-KEY", authentication.APIKey);
            httpClient.DefaultRequestHeaders.Add("CB-ACCESS-SIGN", authentication.Signature);
            httpClient.DefaultRequestHeaders.Add("CB-ACCESS-TIMESTAMP", authentication.TimeStamp);
            httpClient.DefaultRequestHeaders.Add("CB-ACCESS-PASSPHRASE", authentication.PassPhrase);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "sefbkn.github.io");
            //Binance
            httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", authentication.APIKey);
            return httpClient;
        }
        #endregion
    }
}
