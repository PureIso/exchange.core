using exchange.core.implementations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace exchange.service
{
    public class Startup
    {
        public static readonly string AllowSpecificOrigins = "AllowedOrigins";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            string[] corsOrigins = Configuration.GetSection("CorsOrigins").Get<string[]>(); 
            if(corsOrigins != null)
            {
                logger.LogInformation($"CORS Origins: {string.Join(",", corsOrigins)}");
                if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
                app.UseCors(builder =>
                {
                    builder.WithOrigins(corsOrigins)
                        .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
                app.UseRouting();
                app.UseEndpoints(endpoints => { endpoints.MapHub<ExchangeHubService>("/hubs/exchange"); });
            }
            else
            {
                logger.LogInformation($"CORS Origins: N/A");
            }
        }
    }
}