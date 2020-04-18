using System;
using System.Collections.Generic;
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

namespace exchange.service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHubContext<ExchangeHub, IExchangeHub> _exchangeHub;
        private readonly IExchangeService _coinbase;

        public Worker(ILogger<Worker> logger, IHubContext<ExchangeHub, IExchangeHub> exchangeHub, IExchangeService coinbase)
        {
            _logger = logger;
            _exchangeHub = exchangeHub;
            _coinbase = coinbase;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker started at: {DateTime.Now}");
            _coinbase.UpdateAccountsAsync().Wait(cancellationToken);
            _coinbase.UpdateProductsAsync().Wait(cancellationToken);
            List<Product> products = new List<Product>
            {
                _coinbase.Products.FirstOrDefault(x => x.BaseCurrency == "BTC" && x.QuoteCurrency == "EUR"),
                _coinbase.Products.FirstOrDefault(x => x.BaseCurrency == "ETH" && x.QuoteCurrency == "EUR")
            };
            _coinbase.UpdateProductOrderBookAsync(products[0]).Wait(cancellationToken);
            _coinbase.UpdateOrdersAsync().Wait(cancellationToken);
            _coinbase.UpdateTickersAsync(products).Wait(cancellationToken); 
            _coinbase.Subscribe(products.ToSubscribeString());
            
            _coinbase.FeedBroadCast += FeedBroadCast;
            _coinbase.ProcessFeed();

            _logger.LogInformation($"Account Count: {_coinbase.Accounts.Count}");
            await base.StartAsync(cancellationToken);
        }

        private async void FeedBroadCast(Feed feed)
        {
            _coinbase.CurrentPrices[feed.ProductID] = feed.Price.ToDecimal();
            await _exchangeHub.Clients.All.CurrentPrices(_coinbase.CurrentPrices);
            _logger.LogInformation($"Feed: [Product: {feed.ProductID}, Price: {feed.Price}, Side: {feed.Side}]");
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker stopped at: {DateTime.Now}");
            _coinbase.Close();
            return base.StopAsync(cancellationToken);
        }
        public override void Dispose()
        {
            _logger.LogInformation($"Worker disposed at: {DateTime.Now}");
            base.Dispose();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);              
            await Task.Delay(1000, stoppingToken);
        }
    }
}
