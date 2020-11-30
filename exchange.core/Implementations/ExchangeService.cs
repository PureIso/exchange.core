using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.interfaces;
using exchange.core.models;
using Microsoft.AspNetCore.SignalR;

namespace exchange.core.implementations
{
    public class ExchangeService : IExchangeService
    {
        private readonly IHubContext<ExchangeHubService, IExchangeHubService> _exchangeHubService;

        public ExchangeService(IHubContext<ExchangeHubService, IExchangeHubService> exchangeHubService)
        {
            _exchangeHubService ??= exchangeHubService;
        }

        public async Task DelegateNotifyCurrentPrices(string applicationName, Dictionary<string, decimal> currentPrices)
        {
            if (_exchangeHubService.Clients == null)
                return;
            await _exchangeHubService.Clients.All.NotifyCurrentPrices(applicationName, currentPrices);
        }

        public async Task DelegateNotifyAccountInfo(string applicationName, Dictionary<string, decimal> accountInformation)
        {
            if (_exchangeHubService.Clients == null)
                return;
            await _exchangeHubService.Clients.All.NotifyAccountInfo(applicationName, accountInformation);
        }
        public async Task DelegateNotifyAssetInformation(string applicationName, Dictionary<string, AssetInformation> assetInformation)
        {
            if (_exchangeHubService.Clients == null)
                return;
            await _exchangeHubService.Clients.All.NotifyAssetInformation(applicationName, assetInformation);
        }
        public async Task DelegateNotifyMainCurrency(string applicationName, string mainCurrency)
        {
            if (_exchangeHubService.Clients == null)
                return;
            await _exchangeHubService.Clients.All.NotifyMainCurrency(applicationName, mainCurrency);
        }
        public async Task DelegateNotifyFills(string applicationName, List<Fill> fills)
        {
            if (_exchangeHubService.Clients == null)
                return;
            await _exchangeHubService.Clients.All.NotifyFills(applicationName, fills);
        }
        public async Task DelegateNotifyOrders(string applicationName, List<Order> orders)
        {
            if (_exchangeHubService.Clients == null)
                return;
            await _exchangeHubService.Clients.All.NotifyOrders(applicationName, orders);
        }
    }
}