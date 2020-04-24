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
using exchange.core.Enums;

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
            _coinbase.FeedBroadcast += FeedBroadCast; 
            _coinbase.ProcessLogBroadcast += ProcessLogBroadcast;
            await _coinbase.UpdateAccountsAsync();
            if (_coinbase.Accounts != null && _coinbase.Accounts.Any())
            {
                await _coinbase.UpdateAccountHistoryAsync(_coinbase.Accounts[0].ID);
                await _coinbase.UpdateAccountHoldsAsync(_coinbase.Accounts[0].ID);

                _coinbase.UpdateProductsAsync().Wait(cancellationToken);
                List<Product> products = new List<Product>
                {
                    _coinbase.Products.FirstOrDefault(x => x.BaseCurrency == "BTC" && x.QuoteCurrency == "EUR"),
                    _coinbase.Products.FirstOrDefault(x => x.BaseCurrency == "ETH" && x.QuoteCurrency == "EUR")
                };
                products.RemoveAll(x => x == null);
                if (products.Any())
                {
                    _coinbase.UpdateProductOrderBookAsync(products[0]).Wait(cancellationToken);
                    _coinbase.UpdateOrdersAsync().Wait(cancellationToken);
                    _coinbase.UpdateFillsAsync(products[0]).Wait(cancellationToken);
                    _coinbase.UpdateTickersAsync(products).Wait(cancellationToken);
                    _coinbase.ChangeFeed(products.ToSubscribeString());
                    
                    _coinbase.StartProcessingFeed();

                    //market order
                    //buy
                    Order marketOrderBuy = new Order {Size = "0.1", Side = OrderSide.Buy, Type = OrderType.Market, ProductID = "BTC-EUR"};
                    Order marketBuyOrderResponse = await _coinbase.PostOrdersAsync(marketOrderBuy);
                    //sell
                    Order marketOrderSell = new Order { Size = "0.1", Side = OrderSide.Sell, Type = OrderType.Market, ProductID = "BTC-EUR" };
                    Order marketSellOrderResponse = await _coinbase.PostOrdersAsync(marketOrderSell);
                    //limit order
                    Order limitOrder = new Order { Size = "0.1", Side = OrderSide.Buy, Type = OrderType.Limit, ProductID = "BTC-EUR", Price = "1000" };
                    Order limitOrderResponse = await _coinbase.PostOrdersAsync(limitOrder);
                    //cancel order
                    await _coinbase.CancelOrdersAsync(limitOrderResponse);
                    List<HistoricRate> historicRates =  await _coinbase.UpdateProductHistoricCandlesAsync(products[0], 
                        DateTime.Now.AddHours(-2).ToUniversalTime(),
                        DateTime.Now.ToUniversalTime(), 900);//15 minutes
                }
                _logger.LogInformation($"Account Count: {_coinbase.Accounts.Count}");
            }
           
            await base.StartAsync(cancellationToken);
        }

        private async void ProcessLogBroadcast(MessageType messageType, string message)
        {
            await _exchangeHub.Clients.All.NotifyInformation(messageType,message);
        }

        private async void FeedBroadCast(Feed feed)
        {
            if (feed.ProductID == null)
                return;
            _coinbase.CurrentPrices[feed.ProductID] = feed.Price.ToDecimal();
            await _exchangeHub.Clients.All.NotifyCurrentPrices(_coinbase.CurrentPrices);
            await _exchangeHub.Clients.All.NotifyInformation(MessageType.General, $"Feed: [Product: {feed.ProductID}, Price: {feed.Price}, Side: {feed.Side}, ID:{feed.Type}]");
            _logger.LogInformation($"Feed: [Product: {feed.ProductID}, Price: {feed.Price}, Side: {feed.Side}, ID:{feed.Type}]");
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker stopped at: {DateTime.Now}");
            List<Product> products = new List<Product>
            {
                _coinbase.Products.FirstOrDefault(x => x.BaseCurrency == "BTC" && x.QuoteCurrency == "EUR"),
                _coinbase.Products.FirstOrDefault(x => x.BaseCurrency == "ETH" && x.QuoteCurrency == "EUR")
            };
            products.RemoveAll(x => x == null);
            _coinbase.ChangeFeed(products.ToUnSubscribeString());
            _coinbase.CloseFeed();
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
