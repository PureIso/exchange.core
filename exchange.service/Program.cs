using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using exchange.coinbase.advancedtrade.authentication;
using exchange.core.implementations;
using exchange.core.interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace exchange.service
{
    public static class Program
    {
#if DEBUG
        private const string DefaultEnvironmentName = "Development";
#else
        private const string DefaultEnvironmentName = "Production";
#endif
        public static async Task Main(string[] args)
        {
            try
            {
                WebApplicationBuilder builder = CreateApplicationBuilder(args);
                ConfigureLogger(builder.Configuration);
                builder.Logging.ClearProviders();
                builder.Host.UseSerilog();

                await WaitForGraylogAsync(builder.Configuration);
                ConfigureApplicationServices(builder.Services, builder.Configuration);
                ServiceHost.AddWebServices(builder.Services, builder.Configuration);

                string[] hostUrls = builder.Configuration
                    .GetSection("HostUrls")
                    .Get<string[]>() ?? Array.Empty<string>();
                if (hostUrls.Length > 0)
                {
                    Log.Information("Host Url: {HostUrls}", string.Join(",", hostUrls));
                    builder.WebHost.UseUrls(hostUrls);
                }

                await using WebApplication application = builder.Build();
                ServiceHost.Configure(application);

                Log.Information("Exchange Server Starting: {HostName}", Dns.GetHostName());
                Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
                await application.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static WebApplicationBuilder CreateApplicationBuilder(string[] args)
        {
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrWhiteSpace(environment))
                environment = DefaultEnvironmentName;

            return WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = Directory.GetCurrentDirectory(),
                EnvironmentName = environment
            });
        }

        private static void ConfigureLogger(IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        private static async Task WaitForGraylogAsync(IConfiguration configuration)
        {
            string grayLogRequestUrl = configuration["GrayLogUrl"];
            if (string.IsNullOrWhiteSpace(grayLogRequestUrl))
            {
                Log.Information("GrayLog url configuration is invalid. Graylog will not be used.");
                return;
            }

            using HttpClient client = new HttpClient();
            int delayInMilliseconds = 1000;
            int remainingAttempts = configuration.GetValue<int>("MaxGrayLogConnectionAttempt");
            while (remainingAttempts > 0)
            {
                bool connected = false;
                try
                {
                    Log.Information("Connecting to Graylog: {GrayLogRequestUrl}", grayLogRequestUrl);
                    using HttpResponseMessage response = await client.GetAsync(grayLogRequestUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information(
                            "Connected to Graylog: {GrayLogRequestUrl} status code: {StatusCode}",
                            grayLogRequestUrl,
                            response.StatusCode);
                        connected = true;
                    }
                    else
                    {
                        Log.Information(
                            "Connection to Graylog: {GrayLogRequestUrl} status code: {StatusCode}",
                            grayLogRequestUrl,
                            response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Unable to connect to Graylog: {GrayLogRequestUrl}", grayLogRequestUrl);
                }

                if (connected)
                    return;

                remainingAttempts--;
                if (remainingAttempts <= 0)
                    return;

                Log.Information("Graylog connection attempts remaining: {Attempts}", remainingAttempts);
                await Task.Delay(delayInMilliseconds);
                delayInMilliseconds += 1000;
            }
        }

        private static void ConfigureApplicationServices(
            IServiceCollection services,
            IConfiguration configuration)
        {
            ExchangeSettings exchangeSettings = configuration
                .GetSection("ExchangeSettings")
                .Get<ExchangeSettings>();
            if (exchangeSettings != null)
                services.AddSingleton<IExchangeSettings>(exchangeSettings);

            string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            if (string.IsNullOrEmpty(directoryName))
                return;

            string pluginDirectory = Path.Combine(directoryName, "plugin");
            Directory.CreateDirectory(pluginDirectory);
            ExchangePluginService exchangePluginService = new ExchangePluginService();
            exchangePluginService.AddPluginFromFolder(
                pluginDirectory,
                pluginType => !string.Equals(
                    pluginType.Assembly.GetName().Name,
                    "exchange.coinbase.advancedtrade",
                    StringComparison.OrdinalIgnoreCase));
            services.AddSingleton<IExchangePluginService>(exchangePluginService);
            services.AddSingleton(new CoinbaseAdvancedTradeRegistrationOptions
            {
                Enabled = configuration.GetValue<bool>("CoinbaseAdvancedTrade:Enabled")
            });
            services.AddSingleton<ICoinbaseCredentialSource,
                EnvironmentCoinbaseCredentialSource>();
            services.AddSingleton<ICoinbaseAdvancedTradeRuntimeActivator,
                CoinbaseAdvancedTradeRuntimeActivator>();
            services.AddSingleton<CoinbaseAdvancedTradePluginRegistrar>();
            services.AddSingleton<IExchangeService, ExchangeService>();
            services.AddHostedService<Worker>();
        }
    }
}
