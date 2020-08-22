using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using exchange.core.helpers;
using exchange.core.interfaces;
using exchange.core.models;
using Microsoft.AspNetCore.SignalR;

namespace exchange.core.implementations
{
    public class ExchangeHubService : Hub<IExchangeHubService>, IExchangeHubService
    {
        private readonly IExchangePluginService _exchangePluginService;

        public ExchangeHubService(IExchangePluginService exchangePluginService)
        {
            _exchangePluginService = exchangePluginService;
        }

        public async Task RequestedApplications()
        {
            List<string> applications =_exchangePluginService.PluginExchanges.Select(x => x.ApplicationName).ToList();
            await Clients.All.NotifyApplications(applications);
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
                await Clients.All.NotifyCurrentPrices(abstractExchangePlugin.ApplicationName,
                    abstractExchangePlugin.CurrentFeed.CurrentPrices);
            }
        }

        public async Task RequestedSubscription(string applicationName, List<string> symbols)
        {
            AbstractExchangePlugin abstractExchangePlugin =
                _exchangePluginService.PluginExchanges.FirstOrDefault(x => x.ApplicationName == applicationName);
            if (abstractExchangePlugin == null)
                return;
            List<Product> products = abstractExchangePlugin.Products.Where(x => symbols.Contains(x.ID)).ToList();
            await abstractExchangePlugin.ChangeFeed(products);
        }

        public async Task NotifyApplications(List<string> applicationNames)
        {
            await Clients.All.NotifyApplications(applicationNames);
        }

        public async Task RequestedProducts()
        {
            foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
            {
                if (abstractExchangePlugin.CurrentFeed == null)
                    continue;
                await Clients.All.NotifyProductChange(abstractExchangePlugin.ApplicationName,
                    abstractExchangePlugin.Products.ProductsToSymbols());
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

        public async Task NotifyProductChange(string applicationName, List<string> symbols)
        {
            await Clients.All.NotifyProductChange(applicationName, symbols);
        }
    }
}