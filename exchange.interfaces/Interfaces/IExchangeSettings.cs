namespace exchange.core.Interfaces
{
    public interface IExchangeSettings
    {
        string Uri { get; set; }
        string APIKey { get; set; }
        string PassPhrase { get; set; }
        string Secret { get; set; }
        string EndpointUrl { get; set; }
    }
}
