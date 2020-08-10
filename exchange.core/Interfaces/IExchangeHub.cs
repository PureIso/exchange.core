using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.Enums;

namespace exchange.core.interfaces
{
    public interface IExchangeHub
    {
        Task NotifyCurrentPrices(string exchange, Dictionary<string, decimal> currentPrices);
        Task NotifyInformation(string exchange, MessageType messageType, string message);

        Task NotifyTechnicalIndicatorInformation(string exchange, Dictionary<string, string> indicatorInformation);
    }
}
