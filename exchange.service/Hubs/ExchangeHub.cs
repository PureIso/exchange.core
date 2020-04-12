using exchange.service.interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace exchange.service.hubs
{
    public class ExchangeHub : Hub<IExchangeHub>
    {
        public async Task CurrentPrices(Dictionary<string, decimal> currentPrices)
        {
            await Clients.All.CurrentPrices(currentPrices);
        }

        public override Task OnConnectedAsync()
        {
            //Clients.All.InvokeAsync("broadcastMessage", "system", $"{Context.ConnectionId} joined the conversation");
            return base.OnConnectedAsync();
        }
    }
}
