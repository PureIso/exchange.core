﻿using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.models;

namespace exchange.core.interfaces
{
    public interface IExchangeService
    {
        Task DelegateNotifySubscriptionStatus(string applicationName, bool isRunning);
        Task DelegateNotifyFills(string applicationName, List<Fill> fills);
        Task DelegateNotifyOrders(string applicationName, List<Order> orders);
        Task DelegateNotifyCurrentPrices(string applicationName, Dictionary<string, decimal> currentPrices);
        Task DelegateNotifyAccountInfo(string applicationName, Dictionary<string, decimal> accountInformation);
        Task DelegateNotifyAssetInformation(string applicationName, Dictionary<string, AssetInformation> assetInformation);
        Task DelegateNotifyMainCurrency(string applicationName, string mainCurrency);
        Task DelegateNotifyFillStatistics(string applicationName, FillStatistics fillStatistic);
    }
}