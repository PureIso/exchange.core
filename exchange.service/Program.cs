using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using exchange.core.implementations;
using exchange.core.interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.Graylog;

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
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Graylog(new GraylogSinkOptions
                {
                    HostnameOrAddress = "192.168.1.203",
                    Port = 12201,
                    MinimumLogEventLevel = Serilog.Events.LogEventLevel.Information,
                    TransportType = Serilog.Sinks.Graylog.Core.Transport.TransportType.Udp,
                })
                .CreateLogger();
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                };
                // Get the response.
                while(response.StatusCode != HttpStatusCode.OK)
                {
                    try
                    {
                        response = client.GetAsync(
                        "http://192.168.1.203:7555/"
                        ).GetAwaiter().GetResult();
                    }
                    catch (Exception _)
                    {
                    }      
                    Task.Delay(1000).GetAwaiter();
                }
                Log.Information("Starting up - 127.0.0.1 - docker - 2");
                CreateHostBuilder(args).Build().Run();
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

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(loggingConfiguration => loggingConfiguration.ClearProviders())
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://exchange.service:5000/", "https://exchange.service:5001/");
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
                });
                //.ConfigureLogging((context, logging) =>
                //{
                //    //change configuration for logging
                //    //clearing out everyone listening to the logging event
                //    logging.ClearProviders();
                //    //add configuration with appsettings.json
                //    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                //    //add loggers (write to)
                //    logging.AddDebug();
                //    logging.AddConsole();
                //});
        }
    }
}