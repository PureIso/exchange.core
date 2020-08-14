using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.Enums;
using exchange.core.Implementations;

namespace exchange.core.interfaces
{
    public interface IExchangeService
    {
        List<AbstractExchangePlugin> ExchangeServicePlugins { get; set; }
        void RequestAccountInfo();
        void RequestCurrentPrices();
        Task NotifyCurrentPrices(string applicationName, Dictionary<string, decimal> currentPrices);
        Task NotifyInformation(string applicationName, MessageType messageType, string message);
        Task NotifyAccountInfo(string applicationName, Dictionary<string, decimal> indicatorInformation);
        Task NotifyTechnicalIndicatorInformation(string applicationName, Dictionary<string, string> indicatorInformation);
    }
}
