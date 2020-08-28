using System.IO;
using System.Reflection;
using exchange.core.implementations;
using exchange.core.implementations;
using exchange.core.interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace exchange.service
{
    public static class Program
    {
#if DEBUG
        private const string DefaultEnvironmentName = "Development";
#else
        private const string DefaultEnvironmentName = "Production";
#endif

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://*:5000/", "https://*:5001/");
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    ExchangeSettings exchangeSettings =
                        configuration.GetSection("ExchangeSettings").Get<ExchangeSettings>();
                    services.AddSingleton<IExchangeSettings>(exchangeSettings);
                    //cross origin requests
                    services.AddCors(options => options.AddPolicy(Startup.AllowSpecificOrigins, builder =>
                    {
                        builder.WithOrigins("http://*:9000/")
                            .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                    }));
                    services.AddSignalR();
                    string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                    if (string.IsNullOrEmpty(directoryName))
                        return;
                    string pluginDirectory = Path.Combine(directoryName, "plugin");
                    if (!Directory.Exists(pluginDirectory))
                        Directory.CreateDirectory(pluginDirectory);
                    ExchangePluginService exchangePluginService = new ExchangePluginService();
                    exchangePluginService.AddPluginFromFolder(pluginDirectory);
                    services.AddSingleton<IExchangePluginService>(exchangePluginService);
                    services.AddSingleton<IExchangeService, ExchangeService>();
                    services.AddHostedService<Worker>();
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.SetBasePath(Directory.GetCurrentDirectory());
                    configApp.AddJsonFile($"appsettings.{DefaultEnvironmentName}.json", true);
                    configApp.AddCommandLine(args);
                })
                .ConfigureLogging((context, logging) =>
                {
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
}