using System.Collections.Generic;
using System.Threading.Tasks;

namespace exchange.core.interfaces
{
    public interface IExchangeService
    {
        List<AbstractExchangePlugin> ExchangeServicePlugins { get; set; }
        void RequestedAccountInfo();
        void RequestedCurrentPrices();
    }
}
