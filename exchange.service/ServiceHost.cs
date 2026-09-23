using System;
using exchange.core.implementations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace exchange.service
{
    public static class ServiceHost
    {
        public const string AllowSpecificOrigins = "AllowedOrigins";

        public static void AddWebServices(
            IServiceCollection services,
            IConfiguration configuration)
        {
            string[] corsOrigins = GetCorsOrigins(configuration);
            services.AddCors(options =>
            {
                if (corsOrigins.Length == 0)
                    return;

                options.AddPolicy(AllowSpecificOrigins, policy =>
                    policy.WithOrigins(corsOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
            });
            services.AddSignalR();
        }

        public static void Configure(WebApplication application)
        {
            application.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

            string[] corsOrigins = GetCorsOrigins(application.Configuration);
            if (corsOrigins.Length == 0)
            {
                application.Logger.LogInformation("CORS Origins: N/A");
                return;
            }

            application.Logger.LogInformation(
                "CORS Origins: {CorsOrigins}",
                string.Join(",", corsOrigins));
            if (application.Environment.IsDevelopment())
                application.UseDeveloperExceptionPage();

            application.UseRouting();
            application.UseCors(AllowSpecificOrigins);
            application.MapHub<ExchangeHubService>("/hubs/exchange");
        }

        private static string[] GetCorsOrigins(IConfiguration configuration)
        {
            return configuration.GetSection("CorsOrigins").Get<string[]>()
                ?? Array.Empty<string>();
        }
    }
}
