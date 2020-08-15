using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.interfaces;
using Microsoft.AspNetCore.SignalR;

namespace exchange.core.implementations
{
    public class ExchangeHubService : Hub<IExchangeHubService>
    {
        private readonly IExchangePluginService _exchangePluginService;

        public ExchangeHubService(IExchangePluginService exchangePluginService)
        {
            _exchangePluginService = exchangePluginService;
        }

        public async Task RequestedAccountInfo()
        {
            foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
            {

                if (abstractExchangePlugin.AccountInfo == null)
                    continue;
                await Clients.All.NotifyAccountInfo(abstractExchangePlugin.ApplicationName,
                    abstractExchangePlugin.AccountInfo);
            }
        }
        public async Task RequestedCurrentPrices()
        {
            foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
            {
                if (abstractExchangePlugin.CurrentFeed == null)
                    continue;
                await Clients.All.NotifyCurrentPrices(abstractExchangePlugin.ApplicationName, abstractExchangePlugin.CurrentFeed.CurrentPrices);
            }
        }

        public async Task NotifyCurrentPrices(string applicationName, Dictionary<string, decimal> currentPrices)
        {
            await Clients.All.NotifyCurrentPrices(applicationName, currentPrices);
        }

        public async Task NotifyAccountInfo(string applicationName, Dictionary<string, decimal> accountInformation)
        {
            await Clients.All.NotifyAccountInfo(applicationName, accountInformation);
        }
    }
}
