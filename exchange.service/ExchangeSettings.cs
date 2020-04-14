using exchange.core.Interfaces;

namespace exchange.service
{
    public class ExchangeSettings : IExchangeSettings
    {
        public string Uri { get; set; }
        public string APIKey { get; set; }
        public string PassPhrase { get; set; }
        public string Secret { get; set; }
        public string EndpointUrl { get; set; }
    }
}
