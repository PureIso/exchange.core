using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using exchange.core.interfaces;
using exchange.core.models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace exchange.core.implementations
{
    public class ExchangeHubService : Hub<IExchangeHubService>
    {
        private readonly IExchangePluginService _exchangePluginService;
        private readonly ILogger<ExchangeHubService> _logger;

        public ExchangeHubService(IExchangePluginService exchangePluginService, ILogger<ExchangeHubService> logger)
        {
            if (_exchangePluginService == null)
            {
                _exchangePluginService = exchangePluginService;
                logger.LogInformation("ExchangeHubService exchange plugin service loaded.");
            }
            if (_logger == null)
            {
                _logger = logger;
                logger.LogInformation("ExchangeHubService logger loaded.");
            }  
        }

        public async Task RequestedApplications()
        {
            List<string> applications =_exchangePluginService.PluginExchanges.Select(x => x.ApplicationName).ToList();
            await Clients.All.NotifyApplications(applications);
            _logger.LogInformation("ExchangeHubService RequestedApplications request to all clients notification.");
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
            List<Product> products = symbols.Select(symbol => new Product {ID = symbol}).ToList();
            await abstractExchangePlugin.ChangeFeed(products);
        }
        public async Task RequestedProducts()
        {
            foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
            {
                await Clients.All.NotifyProductChange(abstractExchangePlugin.ApplicationName, abstractExchangePlugin.Products);
            }
        }
    }
}