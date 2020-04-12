using System.Collections.Generic;
using System.Threading.Tasks;

namespace exchange.service.interfaces
{
    public interface IExchangeHub
    {
        Task CurrentPrices(Dictionary<string, decimal> currentPrices);
    }
}
