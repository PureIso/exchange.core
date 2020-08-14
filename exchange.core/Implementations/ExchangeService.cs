using System.Collections.Generic;
using exchange.core.interfaces;

namespace exchange.core.Implementations
{
    public class ExchangeService : ExchangeServiceBase
    {

        public ExchangeService(IExchangePluginService exchangePluginService) : base(exchangePluginService)
        {
        }
    }
}
