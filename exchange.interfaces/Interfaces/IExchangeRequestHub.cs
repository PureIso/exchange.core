using System.Collections.Generic;
using System.Threading.Tasks;

namespace exchange.core.Interfaces
{
    public interface IExchangeRequestHub
    {
        Task RequestedCurrentPrices(Dictionary<string, decimal> currentPrices);
    }
}
