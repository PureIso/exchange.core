using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase;
using exchange.coinbase.models;
using exchange.core.interfaces;
using exchange.service.hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace exchange.service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHubContext<ExchangeHub, IExchangeHub> _exchangeHub;

        private ExchangeSettings ExchangeSettings { get; }
        private Coinbase _coinbase;

        public Worker(ILogger<Worker> logger, ExchangeSettings configuration, IHubContext<ExchangeHub, IExchangeHub> exchangeHub)
        {
            _logger = logger;
            _exchangeHub = exchangeHub;
            ExchangeSettings = configuration;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker started at: {DateTime.Now}");
            Authentication authentication = new Authentication(
                ExchangeSettings.APIKey,
                ExchangeSettings.PassPhrase,
                ExchangeSettings.Secret,
                ExchangeSettings.EndpointUrl,
                ExchangeSettings.Uri);

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("CB-ACCESS-KEY", authentication.ApiKey);
            httpClient.DefaultRequestHeaders.Add("CB-ACCESS-PASSPHRASE", authentication.Passphrase);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "sefbkn.github.io");
            _coinbase = new Coinbase(
                authentication,
                httpClient);
            _coinbase.UpdateAccountsAsync().Wait();
            _coinbase.UpdateProductsAsync().Wait();
            List<Product> products = new List<Product>
            {
                _coinbase.Products.FirstOrDefault(x => x.BaseCurrency == "BTC" && x.QuoteCurrency == "EUR"),
                _coinbase.Products.FirstOrDefault(x => x.BaseCurrency == "ETH" && x.QuoteCurrency == "EUR")
            };
            _coinbase.UpdateProductOrderBookAsync(products[0]).Wait();
            _coinbase.UpdateOrdersAsync(products[0]).Wait();
            _coinbase.UpdateTickersAsync(products).Wait();
            _coinbase.WebSocketSubscribe(products);
            _coinbase.FeedBroadCast += FeedBroadCast;
            _logger.LogInformation($"Acount Count: {_coinbase.Accounts.Count}");
            await base.StartAsync(cancellationToken);
        }
        public async void FeedBroadCast(Feed feed)
        {
            _coinbase.CurrentPrices[feed.ProductID] = feed.Price.ToDecimal();
            await _exchangeHub.Clients.All.CurrentPrices(_coinbase.CurrentPrices);
            _logger.LogInformation($"Feed: [Product: {feed.ProductID}, Price: {feed.Price}, Side: {feed.Side}]");
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker stopped at: {DateTime.Now}");
            _coinbase.WebSocketClose();
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
            await Task.Delay(1000);
        }
    }
}
