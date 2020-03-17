using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace exchange.core.interfaces.models
{
    /// <summary>
    /// Exchange interface
    /// </summary>
    public interface IExchange
    {
        /// <summary>
        /// Exchange initializer
        /// </summary>
        /// <param name="apiKey">Exchange API key</param>
        /// <param name="secret">Exchange API secret</param>
        /// <returns></returns>
        Task Initialise(string apiKey, string secret);
        Task UpdateAccounts();
        Task UpdateTickers();
        Task UpdateOrders();
        Task UpdateFills();
        Task UpdateOrderBook();
        Task UpdateRealTimeTradeInformation();

        Task<List<IHistoricRate>> GetHistoryRates(IProduct product, DateTime startingDateTime, DateTime endingDateTime, int granularity);

        Task SubscribeRealTime(string message);
        Task UnSubscribeRealTime();

        List<IAccount> Accounts { get; set; }
        List<ITicker> Tickers { get; set; }
        List<IOrder> Orders { get; set; }
        List<IFill> Fills { get; set; }
        List<IOrderBook> OrderBook { get; set; }
        List<IRealTimeTradeInformation> RealTimeTradeInformation { get; set; }
    }
}
