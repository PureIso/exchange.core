using exchange.core.interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace exchange.service.hubs
{
    public class ExchangeHub : Hub<IExchangeHub>
    {
        private readonly IExchangeService _exchangeService;
        public ExchangeHub(IExchangeService exchangeService)
        {
            _exchangeService = exchangeService;
        }


        #region Requested Methods from UI to Service
        public void RequestCurrentPrices()
        {
            _exchangeService.RequestCurrentPrices();
        }
        public void RequestAccountInfo()
        {
            _exchangeService.RequestAccountInfo();
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
