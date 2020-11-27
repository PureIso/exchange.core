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
        public async Task RequestedCancelAllOrder(string applicationName, string symbol)
        {
            AbstractExchangePlugin abstractExchangePlugin =
                _exchangePluginService.PluginExchanges.FirstOrDefault(x => x.ApplicationName == applicationName);
            if (abstractExchangePlugin == null)
                return;
            Product product = new Product { ID = symbol };
            List<Order> orders = await abstractExchangePlugin.CancelOrdersAsync(product);
            await Clients.Caller.NotifyOrders(abstractExchangePlugin.ApplicationName, orders);
        }
        public async Task RequestedApplications()
        {
            List<string> applications = _exchangePluginService.PluginExchanges.Select(x => x.ApplicationName).ToList();
            await Clients.All.NotifyApplications(applications);
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
        public async Task RequestedProducts()
        {
            foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
                await Clients.All.NotifyProductChange(abstractExchangePlugin.ApplicationName,
                    abstractExchangePlugin.Products);
        }
        public async Task RequestedSubscription(string applicationName, List<string> symbols)
        {
            AbstractExchangePlugin abstractExchangePlugin =
                _exchangePluginService.PluginExchanges.FirstOrDefault(x => x.ApplicationName == applicationName);
            if (abstractExchangePlugin == null)
                return;
            List<Product> products = symbols.Select(symbol => new Product {ID = symbol}).ToList();
            if(products.Any())
                await abstractExchangePlugin.ChangeFeed(products);
        }
        public async Task RequestedOrder(string applicationName, Order order)
        {
            AbstractExchangePlugin abstractExchangePlugin =
                _exchangePluginService.PluginExchanges.FirstOrDefault(x => x.ApplicationName == applicationName);
            if (abstractExchangePlugin == null)
                return;
            Product product = new Product {ID = order.ProductID};
            await abstractExchangePlugin.PostOrdersAsync(order);
            List<Order> postedOrders = abstractExchangePlugin.Orders;
            await Clients.Caller.NotifyOrders(applicationName, postedOrders);
            List<Fill> fills = await abstractExchangePlugin.UpdateFillsAsync(product);
            await Clients.Caller.NotifyFills(applicationName, fills);
        }
        public async Task RequestedFills(string applicationName, string symbol)
        {
            AbstractExchangePlugin abstractExchangePlugin =
                _exchangePluginService.PluginExchanges.FirstOrDefault(x => x.ApplicationName == applicationName);
            if (abstractExchangePlugin == null)
                return;
            Product product = new Product {ID = symbol};
            List<Fill> fills = await abstractExchangePlugin.UpdateFillsAsync(product);
            await Clients.Caller.NotifyFills(abstractExchangePlugin.ApplicationName, fills);
        }
    }
}