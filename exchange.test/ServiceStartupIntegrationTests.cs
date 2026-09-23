using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using exchange.core.implementations;
using exchange.core.interfaces;
using exchange.service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class ServiceStartupIntegrationTests
    {
        private const string AllowedOrigin = "https://dashboard.example.test";

        [TestMethod]
        public async Task ModernHost_HostsTheExistingSignalRNegotiationRoute()
        {
            using IHost host = await StartHostAsync();
            using HttpClient client = CreateClient(host);

            using HttpResponseMessage negotiation = await client.PostAsync(
                "/hubs/exchange/negotiate?negotiateVersion=1",
                new ByteArrayContent(Array.Empty<byte>()));
            using HttpResponseMessage oldRoute = await client.PostAsync(
                "/exchange/negotiate?negotiateVersion=1",
                new ByteArrayContent(Array.Empty<byte>()));

            Assert.AreEqual(HttpStatusCode.OK, negotiation.StatusCode);
            using JsonDocument body = JsonDocument.Parse(await negotiation.Content.ReadAsStringAsync());
            Assert.IsTrue(body.RootElement.TryGetProperty("connectionId", out _));
            Assert.IsTrue(body.RootElement.TryGetProperty("connectionToken", out _));
            Assert.IsTrue(body.RootElement.TryGetProperty("availableTransports", out _));
            Assert.AreEqual(HttpStatusCode.NotFound, oldRoute.StatusCode);
        }

        [TestMethod]
        public async Task ModernHost_ExposesHealthWithoutCorsConfiguration()
        {
            using IHost host = await StartHostAsync(includeCors: false);
            using HttpClient client = CreateClient(host);

            using HttpResponseMessage health = await client.GetAsync("/health");
            using HttpResponseMessage negotiation = await client.PostAsync(
                "/hubs/exchange/negotiate?negotiateVersion=1",
                new ByteArrayContent(Array.Empty<byte>()));

            Assert.AreEqual(HttpStatusCode.OK, health.StatusCode);
            Assert.AreEqual(
                "{\"status\":\"healthy\"}",
                await health.Content.ReadAsStringAsync());
            Assert.AreEqual(HttpStatusCode.NotFound, negotiation.StatusCode);
        }

        [TestMethod]
        public async Task ModernHost_AppliesCredentialedCorsOnlyToConfiguredOrigin()
        {
            using IHost host = await StartHostAsync();
            using HttpClient client = CreateClient(host);

            using HttpResponseMessage allowed = await SendPreflightAsync(client, AllowedOrigin);
            using HttpResponseMessage rejected = await SendPreflightAsync(
                client,
                "https://untrusted.example.test");

            Assert.AreEqual(HttpStatusCode.NoContent, allowed.StatusCode);
            Assert.AreEqual(AllowedOrigin, GetSingleHeader(allowed, "Access-Control-Allow-Origin"));
            Assert.AreEqual("true", GetSingleHeader(allowed, "Access-Control-Allow-Credentials"));
            Assert.IsTrue(GetSingleHeader(allowed, "Access-Control-Allow-Methods")
                .Contains("POST", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(HttpStatusCode.NoContent, rejected.StatusCode);
            Assert.IsFalse(rejected.Headers.Contains("Access-Control-Allow-Origin"));
            Assert.IsFalse(rejected.Headers.Contains("Access-Control-Allow-Credentials"));
        }

        [TestMethod]
        public async Task ModernHost_ResolvesCurrentHubServicesWithAnEmptyPluginSet()
        {
            using IHost host = await StartHostAsync();

            IExchangePluginService plugins =
                host.Services.GetRequiredService<IExchangePluginService>();
            IExchangeService exchangeService =
                host.Services.GetRequiredService<IExchangeService>();

            Assert.IsNotNull(exchangeService);
            Assert.IsEmpty(plugins.PluginExchanges);
            Assert.IsFalse(plugins.PluginExchanges.Any(plugin =>
                plugin.GetType().Assembly.GetName().Name ==
                "exchange.coinbase.advancedtrade"));
        }

        [TestMethod]
        public void DefaultServiceReferencesRemainLegacyOnlyAfterPackagingRepair()
        {
            string repository = FindRepositoryRoot();
            string copyScript = File.ReadAllText(Path.Combine(repository, "copy_plugins.bat"));
            string packageScript = File.ReadAllText(Path.Combine(
                repository,
                "scripts",
                "package-plugins.ps1"));
            string serviceProject = File.ReadAllText(Path.Combine(
                repository,
                "exchange.service",
                "exchange.service.csproj"));
            string serviceProgram = File.ReadAllText(Path.Combine(
                repository,
                "exchange.service",
                "Program.cs"));

            StringAssert.Contains(copyScript, "package-plugins.ps1");
            StringAssert.Contains(packageScript, "net10.0");
            StringAssert.Contains(packageScript, "legacy-default");
            StringAssert.Contains(packageScript, "advanced-trade-acceptance");
            Assert.IsFalse(packageScript.Contains("net5.0", StringComparison.OrdinalIgnoreCase));
            StringAssert.Contains(serviceProject, "exchange.binance.csproj");
            StringAssert.Contains(serviceProject, "exchange.coinbase.csproj");
            StringAssert.Contains(serviceProject, "exchange.coinbase.advancedtrade.csproj");
            StringAssert.Contains(serviceProgram, "CoinbaseAdvancedTrade:Enabled");
            StringAssert.Contains(serviceProgram, "\"exchange.coinbase.advancedtrade\"");
        }

        [TestMethod]
        public void SignalR_RequestAndNotificationMethodNames_AreCharacterized()
        {
            string[] requests = typeof(ExchangeHubService)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(method => method.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            string[] notifications = typeof(IExchangeHubService)
                .GetMethods()
                .Select(method => method.Name)
                .Where(name => name.StartsWith("Notify", StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            CollectionAssert.AreEqual(new[]
            {
                "RequestedAccountInfo",
                "RequestedApplications",
                "RequestedCancelAllOrder",
                "RequestedCurrentPrices",
                "RequestedFillStatistics",
                "RequestedFills",
                "RequestedMainCurrency",
                "RequestedOrder",
                "RequestedPlaceOrder",
                "RequestedProducts",
                "RequestedSubscription"
            }, requests);
            CollectionAssert.AreEqual(new[]
            {
                "NotifyAccountInfo",
                "NotifyApplications",
                "NotifyAssetInformation",
                "NotifyCurrentPrices",
                "NotifyFillStatistics",
                "NotifyFills",
                "NotifyMainCurrency",
                "NotifyOrders",
                "NotifyProductChange",
                "NotifySubscriptionStatus"
            }, notifications);
        }

        private static async Task<IHost> StartHostAsync(bool includeCors = true)
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();
            if (includeCors)
                settings["CorsOrigins:0"] = AllowedOrigin;
            WebApplicationBuilder builder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    EnvironmentName = Environments.Development
                });
            builder.WebHost.UseKestrel(options =>
                options.Listen(IPAddress.Loopback, 0));
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(settings);
            ServiceHost.AddWebServices(builder.Services, builder.Configuration);
            builder.Services.AddSingleton<IExchangePluginService>(
                new ExchangePluginService());
            builder.Services.AddSingleton<IExchangeService, ExchangeService>();

            WebApplication host = builder.Build();
            ServiceHost.Configure(host);
            await host.StartAsync();
            return host;
        }

        private static HttpClient CreateClient(IHost host)
        {
            IServer server = host.Services.GetRequiredService<IServer>();
            string address = server.Features
                .Get<IServerAddressesFeature>()
                .Addresses
                .Single();
            return new HttpClient { BaseAddress = new Uri(address) };
        }

        private static Task<HttpResponseMessage> SendPreflightAsync(
            HttpClient client,
            string origin)
        {
            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Options,
                "/hubs/exchange/negotiate?negotiateVersion=1");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "content-type,x-requested-with");
            return client.SendAsync(request);
        }

        private static string GetSingleHeader(HttpResponseMessage response, string name)
        {
            return response.Headers.GetValues(name).Single();
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "exchange.core.sln")))
                    return current.FullName;
                current = current.Parent;
            }
            throw new DirectoryNotFoundException(
                "The Exchange Core repository root could not be located.");
        }
    }
}
