using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace exchange.service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ExchangeSettings ExchangeSettings { get; }
        private Coinbase _coinbase;

        public Worker(ILogger<Worker> logger, ExchangeSettings configuration)
        {
            _logger = logger;
            ExchangeSettings = configuration;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
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
            _coinbase.UpdateProductOrderBookAsync(_coinbase.Products[0]).Wait();
            //_connection.UpdateTickers(new List<string> { "EUR" }).Wait();
            //_connection.SelectedProduct = _connection.Products.FirstOrDefault(x => x.ID == "BTC-EUR");
            _logger.LogInformation($"Acount Count: {_coinbase.Accounts.Count}");
           
            return base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Worker stopped at: {DateTime.Now}");
            return base.StopAsync(cancellationToken);
        }
        public override void Dispose()
        {
            _logger.LogInformation($"Worker disposed at: {DateTime.Now}");
            base.Dispose();
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _coinbase.Subscribe();

            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    //_connection.OnUpdate += OnUpdate;
            //    //_connection.OnOrderMatch += OnOrderMatch;
            //    //_connection.OnOrderPlaced += OnOrderPlaced;
            //    //while (true)
            //    //{
            //    //    Task task = _connection.Subscribe();
            //    //    task.Wait();
            //    //}


            //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(1000, stoppingToken);
            //}
        }
    }
}
