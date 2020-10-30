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
            _exchangePluginService = exchangePluginService; 
            _logger = logger;
        }

        public async Task RequestedApplications()
        {
            List<string> applications = _exchangePluginService.PluginExchanges.Select(x => x.ApplicationName).ToList();
            await Clients.All.NotifyApplications(applications);
            _logger.LogInformation($"ExchangeHubService RequestedApplications request to all clients notification.");
        }
        public async Task RequestedMainCurrency()
        {
            foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
            {
                if (abstractExchangePlugin.AccountInfo == null)
                    continue;
                await Clients.All.NotifyMainCurrency(abstractExchangePlugin.ApplicationName,
                    abstractExchangePlugin.MainCurrency);
            }
            _logger.LogInformation($"ExchangeHubService RequestedMainCurrency request to all clients notification.");
        }
        public async Task RequestedAccountInfo()
        {
            _logger.LogInformation($"ExchangeHubService RequestedAccountInfo request to all clients notification.");
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
            _logger.LogInformation($"ExchangeHubService NotifyCurrentPrices request to all clients notification.");
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
            _logger.LogInformation($"ExchangeHubService RequestedSubscription request to all clients notification.");
            List<Product> products = symbols.Select(symbol => new Product {ID = symbol}).ToList();
            if(products.Any())
                await abstractExchangePlugin.ChangeFeed(products);
        }
        public async Task RequestedProducts()
        {
            _logger.LogInformation($"ExchangeHubService RequestedProducts request to all clients notification.");
            foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
                await Clients.All.NotifyProductChange(abstractExchangePlugin.ApplicationName,
                    abstractExchangePlugin.Products);
        }
    }
}