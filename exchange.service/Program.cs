using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const string DefaultEnvironmentName = "Docker";
#else
        private const string DefaultEnvironmentName = "Production";
#endif
        public static void Main(string[] args)
        {
            try
            {
                //Read Configuration from appSettings
                string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (string.IsNullOrEmpty(environment))
                    environment = DefaultEnvironmentName;
                IConfigurationRoot config = new ConfigurationBuilder()
                        .AddJsonFile($"appsettings.{environment}.json", true)
                        .Build();
                //Initialize Logger
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config)
                    .CreateLogger();

                HttpClient client = new HttpClient();
                HttpResponseMessage response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                };
                int delayInMS = 1000;
                // Get the response.
                int maxGrayLogConnectionAttempt = config.GetSection("MaxGrayLogConnectionAttempt").Get<int>();
                while (response.StatusCode != HttpStatusCode.OK && maxGrayLogConnectionAttempt > 0)
                {
                    try
                    {
                        string grayLogRequestUrl = config.GetSection("GrayLogUrl").Value;
                        if (!string.IsNullOrEmpty(grayLogRequestUrl))
                        {
                            Log.Information($"Connecting to Graylog: {grayLogRequestUrl} status code: {response.StatusCode} attempt left:{maxGrayLogConnectionAttempt}");
                            response = client.GetAsync(grayLogRequestUrl).GetAwaiter().GetResult();
                            maxGrayLogConnectionAttempt--;
                            delayInMS += 1000;
                        }
                        else
                        {
                            Log.Information($"GrayLog url configuration is invalid.\r\nGraylog will not be used.");
                            response.StatusCode = HttpStatusCode.OK;
                            maxGrayLogConnectionAttempt = 0;
                        }
                    }
                    catch (Exception)
                    {
                        maxGrayLogConnectionAttempt--;
                        // ignored
                    }
                    Task.Delay(delayInMS).GetAwaiter();
                }
                Log.Information($"Exchange Server Starting: {Dns.GetHostName()}");
                Log.Information($"Environment: {environment}");
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
                    //Create a Generic Host - HTTP workload
                    //Read Configuration from appSettings
                    string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                    if (string.IsNullOrEmpty(environment))
                        environment = DefaultEnvironmentName;
                    IConfigurationRoot config = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{environment}.json", true)
                        .Build();
                    string[] hostUrls = config.GetSection("HostUrls").Get<string[]>();
                    Log.Information($"Host Url: {string.Join(",", hostUrls)}");
                    webBuilder.UseUrls(hostUrls);
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    //The host configuration is also added to the app configuration.
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddEnvironmentVariables();
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                    if (string.IsNullOrEmpty(environment))
                        environment = DefaultEnvironmentName;
                    configApp.AddJsonFile($"appsettings.{environment}.json", true);
                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    //Creates a Generic Host using non-HTTP workload.
                    //The IHostedService implementation is added to the DI container
                    IConfiguration configuration = hostContext.Configuration;
                    hostContext.HostingEnvironment.ApplicationName = "Exchange.Service";
                    string[] allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();
                    if (allowedOrigins != null)
                    {
                        Log.Information($"Allowed Origins: {string.Join(",", allowedOrigins)}");
                        ExchangeSettings exchangeSettings =
                            configuration.GetSection("ExchangeSettings").Get<ExchangeSettings>();
                        if (exchangeSettings != null)
                            services.AddSingleton<IExchangeSettings>(exchangeSettings);
                        //cross origin requests
                        services.AddCors(options => options.AddPolicy(Startup.AllowSpecificOrigins, builder =>
                        {
                            builder.WithOrigins(allowedOrigins)
                                .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                        }));
                    }
                    else
                    {
                        Log.Information($"Service CORS Origins: N/A");
                    }

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
                });
        }
    }
}