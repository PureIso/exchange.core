using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace exchange.service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .ConfigureHostConfiguration(configHost =>{
                configHost.SetBasePath(Directory.GetCurrentDirectory());
                configHost.AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) => {
                IConfiguration configuration = hostContext.Configuration;
                ExchangeSettings exchangeSettings = configuration.GetSection("ExchangeSettings").Get<ExchangeSettings>();
                services.AddSingleton(exchangeSettings);
                services.AddSignalR();
                services.AddHostedService<Worker>();
             })
            .ConfigureAppConfiguration((hostContext, configApp) =>{
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
