using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using exchange.core.interfaces;

namespace exchange.core.Implementations
{
    public class ExchangeService : IExchangeService
    {
        public List<AbstractExchangePlugin> ExchangeServicePlugins { get; set; }

        public ExchangeService()
        {
            ExchangeServicePlugins = new List<AbstractExchangePlugin>();
        }

        public void RequestedAccountInfo()
        {
            foreach (AbstractExchangePlugin abstractExchangePlugin in ExchangeServicePlugins)
            {
                if (abstractExchangePlugin.AccountInfo == null)
                    continue;
                abstractExchangePlugin.AccountInfoBroadcast?.Invoke(abstractExchangePlugin.ApplicationName, abstractExchangePlugin.AccountInfo);
            }
        }

        public void RequestedCurrentPrices()
        {
            foreach (AbstractExchangePlugin abstractExchangePlugin in ExchangeServicePlugins)
            {
                if(abstractExchangePlugin.CurrentFeed == null)
                    continue;
                abstractExchangePlugin.FeedBroadcast?.Invoke(abstractExchangePlugin.ApplicationName, abstractExchangePlugin.CurrentFeed);
            }
        }
    }
}
