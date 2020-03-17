using System;
using System.Net.Http;

namespace exchange.core.connectivity
{
    public interface IRequest
    {
        bool Sign { get; set; }
        string Query { get; set; }
        string Url { get; set; }
        Uri Uri { get; set; }
        Uri AbsoluteUri { get; set; }
        string Body { get; set; }
        string ContentType { get; set; }
        string Method { get; set; }
        string TimeStamp { get; set; }

        void Compose(string signature);
        HttpClient ComposeDefaultRequestHeader(HttpClient httpClient, IAuthentication authentication);
    }
}
