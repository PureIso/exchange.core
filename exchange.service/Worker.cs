using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using exchange.core.interfaces;
using exchange.service.hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using exchange.core.models;
using exchange.core;
using exchange.core.Enums;
using exchange.service.Plugins;
using System.Reflection;
using exchange.core.Interfaces;
using exchange.core.helpers;

namespace exchange.service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IExchangeSettings _exchangeSettings;
        private readonly IHubContext<ExchangeHub, IExchangeHub> _exchangeHub;
        private readonly IExchangeService _exchangeService;
        private ExchangePluginService _exchangePluginService;

        public Worker(ILogger<Worker> logger, IHubContext<ExchangeHub, IExchangeHub> exchangeHub,IExchangeSettings exchangeSettings, IExchangeService exchangeService)
        {
            _logger = logger;
            _exchangeHub = exchangeHub;
            _exchangeSettings = exchangeSettings;
            _exchangeService = exchangeService;
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker started at: {DateTime.Now}");
            //Load available plugins
            string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            if (string.IsNullOrEmpty(directoryName))
                return;
            string pluginDirectory = Path.Combine(directoryName, "plugin");
            if (!Directory.Exists(pluginDirectory))
                Directory.CreateDirectory(pluginDirectory);
            _exchangePluginService = new ExchangePluginService(pluginDirectory);
            if (_exchangePluginService.PluginExchanges != null && _exchangePluginService.PluginExchanges.Any())
            {
                foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
                {
                    abstractExchangePlugin.FeedBroadcast += FeedBroadCast;
                    abstractExchangePlugin.ProcessLogBroadcast += ProcessLogBroadcast;
                    abstractExchangePlugin.TechnicalIndicatorInformationBroadcast += TechnicalIndicatorInformationBroadcast;
                    abstractExchangePlugin.AccountInfoBroadcast += AccountInfoBroadcast;
                    await abstractExchangePlugin.InitAsync(_exchangeSettings.TestMode);
                    abstractExchangePlugin.InitIndicatorsAsync();
                    _exchangeService.ExchangeServicePlugins.Add(abstractExchangePlugin);
                    _logger.LogInformation($"Plugin {abstractExchangePlugin.ApplicationName} loaded.");
                }
            }        
            await base.StartAsync(cancellationToken);
        }

        #region Broadcast
        private async void TechnicalIndicatorInformationBroadcast(string applicationName, Dictionary<string, string> indicatorInformation)
        {
            await _exchangeHub.Clients.All.NotifyTechnicalIndicatorInformation(applicationName, indicatorInformation);
        }
        private async void AccountInfoBroadcast(string applicationName, Dictionary<string, decimal> accountInformation)
        {
            await _exchangeHub.Clients.All.NotifyAccountInfo(applicationName, accountInformation);
        }
        private async void ProcessLogBroadcast(string applicationName, MessageType messageType, string message)
        {
            _logger.LogInformation($"Log Broadcast: [ApplicationName: {applicationName} , Type: {messageType.GetStringValue()} , Message: {message}]");
            await _exchangeHub.Clients.All.NotifyInformation(applicationName, messageType, message);
        }
        private async void FeedBroadCast(string applicationName, Feed feed)
        {
            if (feed.ProductID == null)
                return;
            await _exchangeHub.Clients.All.NotifyCurrentPrices(applicationName, feed.CurrentPrices);
            //await _exchangeHub.Clients.All.NotifyInformation(applicationName, MessageType.General, $"Feed: [ApplicationName: {applicationName} ,Product: {feed.ProductID}, Price: {feed.Price}, Side: {feed.Side}, ID:{feed.Type}]");
            _logger.LogInformation($"Feed: [ApplicationName: {applicationName} ,Product: {feed.ProductID}, Price: {feed.Price}, Side: {feed.Side}, ID:{feed.Type}]");
        }
        #endregion

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker stopped at: {DateTime.Now}");
            foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
            {
                abstractExchangePlugin.CloseFeed().GetAwaiter();
            }
            return base.StopAsync(cancellationToken);
        }
        public override void Dispose()
        {
            _logger.LogInformation($"Worker disposed at: {DateTime.Now}");
            foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
            {
                abstractExchangePlugin.Dispose();
            }
            base.Dispose();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);              
            await Task.Delay(1000, stoppingToken);
        }

        public Task RequestedAccountInfo()
        {
            throw new NotImplementedException();
        }

        public Task RequestedCurrentPrices()
        {
            throw new NotImplementedException();
        }
    }
}
