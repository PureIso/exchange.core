using exchange.core.interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace exchange.service.hubs
{
    public class ExchangeHub : Hub<IExchangeHub>
    {
        private IExchangeService IExchangeService { get; set; }
        public ExchangeHub(IExchangeService exchangeService)
        {
            IExchangeService = exchangeService;
        }

        #region Request Methods
        public virtual void RequestAccounts()
        {

        }
        #endregion

        #region Notification Methods
        public async Task CurrentPrices(Dictionary<string, decimal> currentPrices)
        {
            await Clients.All.CurrentPrices(currentPrices);
        }
        #endregion

        #region Overridden Methods
        public override Task OnConnectedAsync()
        {
            //Clients.All.InvokeAsync("broadcastMessage", "system", $"{Context.ConnectionId} joined the conversation");
            return base.OnConnectedAsync();
        }
        #endregion
    }
}
