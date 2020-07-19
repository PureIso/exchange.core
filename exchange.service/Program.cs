using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using exchange.core.Interfaces;
using exchange.coinbase;
using exchange.core.interfaces;
using exchange.core;
using exchange.binance;

namespace exchange.service
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls(new string[] { "http://*:5000/", "https://*:5001/" });
                webBuilder.UseStartup<Startup>();
            })
            .ConfigureHostConfiguration(configHost =>{
                configHost.SetBasePath(Directory.GetCurrentDirectory());
                configHost.AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) => {
                IConfiguration configuration = hostContext.Configuration;
                //cross origin requests
                services.AddCors(options => options.AddPolicy(name: Startup.AllowSpecificOrigins, builder => {
                    builder.WithOrigins($"http://*:9000/")
                        .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                }));
                services.AddHttpClient<IConnectionAdapter, ConnectionAdapter>();
                services.AddSignalR();
                //services.AddSingleton<IExchangeService, Coinbase>();
                services.AddSingleton<IExchangeService, Binance>();
                services.AddHostedService<Worker>();
             })
            .ConfigureAppConfiguration((hostContext, configApp) =>
            {
                string f = Directory.GetCurrentDirectory();
                configApp.SetBasePath(Directory.GetCurrentDirectory());
                configApp.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                configApp.AddCommandLine(args);

            })
            .ConfigureLogging((context, logging) =>{
                //change configuration for logging
                //clearing out everyone listening to the logging event
                logging.ClearProviders();
                //add configuration with appsettings.json
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                //add loggers (write to)
                logging.AddDebug();
                logging.AddConsole();
            });
    }
}
