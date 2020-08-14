using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using exchange.core.Enums;
using exchange.core.interfaces;
using exchange.core.models;
using Microsoft.AspNetCore.SignalR;

namespace exchange.core.Implementations
{
    public abstract class ExchangeServiceBase : Hub<IExchangeService>, IExchangeService
    {
        public List<AbstractExchangePlugin> ExchangeServicePlugins { get; set; }

        public virtual Task NotifyCurrentPrices(string applicationName, Dictionary<string, decimal> currentPrices)
        {
            throw new NotImplementedException();
        }

        public virtual Task NotifyInformation(string applicationName, MessageType messageType, string message)
        {
            throw new NotImplementedException();
        }

        public virtual Task NotifyAccountInfo(string applicationName, Dictionary<string, decimal> indicatorInformation)
        {
            throw new NotImplementedException();
        }

        public virtual Task NotifyTechnicalIndicatorInformation(string applicationName, Dictionary<string, string> indicatorInformation)
        {
            throw new NotImplementedException();
        }

        protected ExchangeServiceBase(IExchangePluginService exchangePluginService)
        {
            ExchangeServicePlugins = exchangePluginService.PluginExchanges;
        }
        public virtual void RequestAccountInfo()
        {
            foreach (AbstractExchangePlugin abstractExchangePlugin in ExchangeServicePlugins)
            {
                if (abstractExchangePlugin.AccountInfo == null)
                    continue;
                abstractExchangePlugin.AccountInfoBroadcast?.Invoke(abstractExchangePlugin.ApplicationName, abstractExchangePlugin.AccountInfo);
            }
        }

        public virtual void RequestCurrentPrices()
        {
            foreach (AbstractExchangePlugin abstractExchangePlugin in ExchangeServicePlugins)
            {
                if (abstractExchangePlugin.CurrentFeed == null)
                    continue;
                abstractExchangePlugin.FeedBroadcast?.Invoke(abstractExchangePlugin.ApplicationName, abstractExchangePlugin.CurrentFeed);
            }
        }

        public async void TechnicalIndicatorInformationBroadcast(string applicationName, Dictionary<string, string> indicatorInformation)
        {
            await Clients.All.NotifyTechnicalIndicatorInformation(applicationName, indicatorInformation);
        }
        public async void AccountInfoBroadcast(string applicationName, Dictionary<string, decimal> accountInformation)
        {
            await Clients.All.NotifyAccountInfo(applicationName, accountInformation);
        }
        public async void ProcessLogBroadcast(string applicationName, MessageType messageType, string message)
        {
           // _logger.LogInformation($"Log Broadcast: [ApplicationName: {applicationName} , Type: {messageType.GetStringValue()} , Message: {message}]");
            await Clients.All.NotifyInformation(applicationName, messageType, message);
        }
        public async void FeedBroadCast(string applicationName, Feed feed)
        {
            if (feed.ProductID == null)
                return;
            await Clients.All.NotifyCurrentPrices(applicationName, feed.CurrentPrices);
            //await _exchangeHub.Clients.All.NotifyInformation(applicationName, MessageType.General, $"Feed: [ApplicationName: {applicationName} ,Product: {feed.ProductID}, Price: {feed.Price}, Side: {feed.Side}, ID:{feed.Type}]");
            //_logger.LogInformation($"Feed: [ApplicationName: {applicationName} ,Product: {feed.ProductID}, Price: {feed.Price}, Side: {feed.Side}, ID:{feed.Type}]");
        }

    }
}
