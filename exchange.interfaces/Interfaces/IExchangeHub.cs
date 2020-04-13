using System.Collections.Generic;
using System.Threading.Tasks;

namespace exchange.core.interfaces
{
    public interface IExchangeHub
    {
        Task CurrentPrices(Dictionary<string, decimal> currentPrices);
    }
}
