using exchange.service.hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace exchange.service
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ExchangeHub>("/hubs/exchange");
            });
        }
    }
}