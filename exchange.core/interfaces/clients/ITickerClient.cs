using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.interfaces.models;
using Newtonsoft.Json.Linq;

namespace exchange.core.interfaces.clients
{
    public interface ITickerClient
    {
        List<ITicker> Tickers { get; set; }

        Task<List<ITicker>> InitialiseTickers();

        ITicker FromJToken(JToken jToken);
        ITicker FromJToken(ITicker ticker, JToken jToken);
        ITicker FromJToken(List<ITicker> tickers, JToken jToken);
    }
}
