using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using exchange.core.enums;
using exchange.core.implementations;
using exchange.core.interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace exchange.service
{
    public class Worker : BackgroundService
    {
        private readonly IExchangePluginService _exchangePluginService;
        private readonly IExchangeService _exchangeService;
        private readonly IExchangeSettings _exchangeSettings;
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, IExchangeSettings exchangeSettings, IExchangeService exchangeService,
            IExchangePluginService exchangePluginService)
        {
            _logger = logger;
            _exchangeSettings = exchangeSettings;
            _exchangeService = exchangeService;
            _exchangePluginService = exchangePluginService;
            logger.LogInformation("ExchangeHubService exchange plugin service loaded.");
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker started at: {DateTime.Now}");
            if (_exchangePluginService.PluginExchanges != null && _exchangePluginService.PluginExchanges.Any())
            {
                AbstractExchangePlugin[] plugins = _exchangePluginService.PluginExchanges.ToArray();
                _exchangePluginService.PluginExchanges.Clear();
                for (int index = plugins.Length-1; index >= 0; index--)
                {
                    AbstractExchangePlugin abstractExchangePlugin = plugins[index];
                    abstractExchangePlugin.NotifyAccountInfo += _exchangeService.DelegateNotifyAccountInfo;
                    abstractExchangePlugin.NotifyCurrentPrices += _exchangeService.DelegateNotifyCurrentPrices;
                    abstractExchangePlugin.NotifyMainCurrency += _exchangeService.DelegateNotifyMainCurrency;
                    abstractExchangePlugin.NotifyAssetInformation += _exchangeService.DelegateNotifyAssetInformation;
                    abstractExchangePlugin.NotifyFills += _exchangeService.DelegateNotifyFills;
                    abstractExchangePlugin.NotifyOrders += _exchangeService.DelegateNotifyOrders;

                    abstractExchangePlugin.ProcessLogBroadcast += ProcessLogBroadcast;
                    bool result = await abstractExchangePlugin.InitAsync(_exchangeSettings);
                    if (!result) continue;
                    _logger.LogInformation($"Plugin {abstractExchangePlugin.ApplicationName} loaded.");
                    plugins = plugins.Where(plugin => plugin.ApplicationName != abstractExchangePlugin.ApplicationName).ToArray();
                    _exchangePluginService.PluginExchanges.Add(abstractExchangePlugin);
                }
            }
            _logger.LogInformation($"Plugin Loading completed.");
            await base.StartAsync(cancellationToken);
        }

        private void ProcessLogBroadcast(string applicationName, MessageType messageType, string message)
        {
            if (messageType == MessageType.Error)
            {
                _logger.LogError($"Application Name: {applicationName}\r\n{message}");
            }
            else
            {
                _logger.LogInformation($"Application Name: {applicationName}\r\n{message}");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker stopped at: {DateTime.Now}");
            if (_exchangePluginService.PluginExchanges == null || !_exchangePluginService.PluginExchanges.Any())
                return base.StopAsync(cancellationToken);
            for (int i = _exchangePluginService.PluginExchanges.Count - 1; i >= 0; i--)
            {
                AbstractExchangePlugin abstractExchangePlugin = _exchangePluginService.PluginExchanges[i];
                abstractExchangePlugin.CloseFeed().GetAwaiter();
            }

            return base.StopAsync(cancellationToken);
        }

        public override async void Dispose()
        {
            _logger.LogInformation($"Worker disposed at: {DateTime.Now}");
            if (_exchangePluginService.PluginExchanges != null && _exchangePluginService.PluginExchanges.Any())
                for (int i = _exchangePluginService.PluginExchanges.Count - 1; i >= 0; i--)
                {
                    AbstractExchangePlugin abstractExchangePlugin = _exchangePluginService.PluginExchanges[i];
                    await abstractExchangePlugin.CloseFeed();
                    abstractExchangePlugin.Dispose();
                }

            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}