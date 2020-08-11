using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.Enums;

namespace exchange.core.interfaces
{
    public interface IExchangeHub
    {
        Task RequestCurrentPrices();
        Task RequestAccountInfo();

        Task NotifyCurrentPrices(string applicationName, Dictionary<string, decimal> currentPrices);
        Task NotifyInformation(string applicationName, MessageType messageType, string message);
        Task NotifyAccountInfo(string applicationName, Dictionary<string, decimal> indicatorInformation);
        Task NotifyTechnicalIndicatorInformation(string applicationName, Dictionary<string, string> indicatorInformation);
    }
}
