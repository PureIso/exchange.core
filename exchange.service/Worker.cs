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
using exchange.core.Enums;
using exchange.service.Plugins;
using System.Reflection;
using exchange.core.Interfaces;
using exchange.core.helpers;
using exchange.core.Implementations;

namespace exchange.service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IExchangeSettings _exchangeSettings;
       // private readonly IHubContext<ExchangeHub, IExchangeHub> _exchangeHub;
        private readonly ExchangeServiceBase _exchangeService;
        private IExchangePluginService _exchangePluginService;

        public Worker(ILogger<Worker> logger,IExchangeSettings exchangeSettings, ExchangeServiceBase exchangeService, IExchangePluginService exchangePluginService)
        {
            _logger = logger;
            //_exchangeHub = exchangeHub;
            _exchangeSettings = exchangeSettings;
            _exchangeService = exchangeService;
            _exchangePluginService = exchangePluginService;
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
            _exchangePluginService.AddPlugin(pluginDirectory);
            if (_exchangePluginService.PluginExchanges != null && _exchangePluginService.PluginExchanges.Any())
            {
                foreach (AbstractExchangePlugin abstractExchangePlugin in _exchangePluginService.PluginExchanges)
                {
                    abstractExchangePlugin.FeedBroadcast += _exchangeService.FeedBroadCast;
                    abstractExchangePlugin.ProcessLogBroadcast += _exchangeService.ProcessLogBroadcast;
                    abstractExchangePlugin.TechnicalIndicatorInformationBroadcast += _exchangeService.TechnicalIndicatorInformationBroadcast;
                    abstractExchangePlugin.AccountInfoBroadcast += _exchangeService.AccountInfoBroadcast;
                    await abstractExchangePlugin.InitAsync(_exchangeSettings.TestMode);
                    abstractExchangePlugin.InitIndicatorsAsync();
                    _exchangeService.ExchangeServicePlugins.Add(abstractExchangePlugin);
                    _logger.LogInformation($"Plugin {abstractExchangePlugin.ApplicationName} loaded.");
                }
            }        
            await base.StartAsync(cancellationToken);
        }

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
