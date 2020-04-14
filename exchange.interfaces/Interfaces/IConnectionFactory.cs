using exchange.core.models;
using System.Net.Http;

namespace exchange.core.Interfaces
{
    public interface IConnectionFactory
    {
        Authentication Authentication { get; set; }
        HttpClient HttpClient { get; set; }
    }
}
