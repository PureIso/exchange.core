using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.interfaces.models;

namespace exchange.core.interfaces.clients
{
    public interface IOrderBookClient
    {
        event Action<ConcurrentDictionary<string, decimal>> OnMatch;
        event Action<IOrderDone> OnOrderDone;

        IOrderBook OrderBook { get; set; }
        ConcurrentDictionary<string, decimal> Matches { get; set; }
        List<IProduct> SubscribedProduct { get; set; }

        void ParseMessage(string message);
        Task<IOrderBook> GetProductOrderBook(string symbol, int limit);
        Task<List<IHistoricRate>> GetProductHistoricCandles(string symbol, int addedStartingMinutes, int addedEndingMinutes);
        Task Subscribe(string message);
        Task<bool> Unsubscribe();
    }
}
