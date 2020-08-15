using exchange.core.implementations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace exchange.service
{
    public class Startup
    {
        public static readonly string AllowSpecificOrigins = "AllowedOrigins";
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors(builder =>
            {
                builder.WithOrigins($"http://localhost:9000")
                    .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            });
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ExchangeHubService>("/hubs/exchange");
            });
        }
    }
}