using System.Collections.Generic;
using System.Threading.Tasks;

namespace exchange.core.interfaces
{
    public interface IExchangeHubService
    {
        Task RequestedAccountInfo();
        Task RequestedCurrentPrices();

        Task NotifyCurrentPrices(string applicationName, Dictionary<string, decimal> currentPrices);
        Task NotifyAccountInfo(string applicationName, Dictionary<string, decimal> accountInformation);
    }
}
