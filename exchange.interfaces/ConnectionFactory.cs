using exchange.core.Interfaces;
using exchange.core.models;
using System.Net.Http;

namespace exchange.core
{
    public class ConnectionFactory : IConnectionFactory
    {
        public Authentication Authentication { get; set; }
        public HttpClient HttpClient { get; set; }
        public ConnectionFactory(HttpClient httpClient, IExchangeSettings exchangeSettings)
        {
            Authentication = new Authentication(
                exchangeSettings.APIKey,
                exchangeSettings.PassPhrase,
                exchangeSettings.Secret,
                exchangeSettings.EndpointUrl,
                exchangeSettings.Uri);
            HttpClient = httpClient;
        }
    }
}
