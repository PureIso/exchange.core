using System.Collections.Generic;
using System.Threading.Tasks;

namespace exchange.core.interfaces
{
    public interface IExchangeService
    {
        Task DelegateNotifyCurrentPrices(string applicationName, Dictionary<string, decimal> currentPrices);
        Task DelegateNotifyAccountInfo(string applicationName, Dictionary<string, decimal> accountInformation);
        Task DelegateNotifyTradeInfo(string applicationName, Dictionary<string, decimal> accountInformation);
    }
}