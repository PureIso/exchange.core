using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.Enums;
using exchange.core.interfaces;
using Microsoft.AspNetCore.SignalR;

namespace exchange.core.implementations
{
    //public abstract class ExchangeServiceBase : Hub<IExchangeHubService>//, IExchangeService
    //{
    //    private readonly IHubContext<ExchangeServiceBase, IExchangeHubService> _hubContext;
    //    private readonly IExchangePluginService _exchangePluginService;

    //    public ExchangeServiceBase(IExchangePluginService exchangePluginService, IHubContext<ExchangeServiceBase, IExchangeHubService> hubContext)
    //    {
    //        var a = Clients;
    //        _hubContext = hubContext;
    //        _exchangePluginService = exchangePluginService;
    //    }

    //    public async Task RequestedAccountInfo()
    //    {
    //        foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
    //        {
                
    //            if (abstractExchangePlugin.AccountInfo == null)
    //                continue;
    //            await Clients.All.NotifyAccountInfo(abstractExchangePlugin.ApplicationName,
    //                abstractExchangePlugin.AccountInfo);
    //        }
    //    }
    //    public async Task RequestedCurrentPrices()
    //    {
    //        foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
    //        {
    //            if (abstractExchangePlugin.CurrentFeed == null)
    //                continue;
    //            await Clients.All.NotifyCurrentPrices(abstractExchangePlugin.ApplicationName, abstractExchangePlugin.CurrentFeed.CurrentPrices);
    //        }
    //    }



    //    public virtual async Task DelegateNotifyCurrentPrices(string applicationName, Dictionary<string, decimal> currentPrices)
    //    {
    //        await _hubContext.Clients.All.NotifyCurrentPrices(applicationName, currentPrices);
    //    }

    //    public virtual async Task DelegateNotifyAccountInfo(string applicationName, Dictionary<string, decimal> accountInformation)
    //    {
    //        await _hubContext.Clients.All.NotifyAccountInfo(applicationName, accountInformation);
    //    }

    //}
}
