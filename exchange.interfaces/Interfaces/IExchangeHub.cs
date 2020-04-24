using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.Enums;

namespace exchange.core.interfaces
{
    public interface IExchangeHub
    {
        Task NotifyCurrentPrices(Dictionary<string, decimal> currentPrices);
        Task NotifyInformation(MessageType messageType, string message);
    }
}
