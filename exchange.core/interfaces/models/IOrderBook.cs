using System.Collections.Concurrent;
using System.Collections.Generic;

namespace exchange.core.interfaces.models
{
    public interface IOrderBook
    {
        IProduct Product { get; set; }
        List<IBidAskOrder> Sells { get; set; }
        List<IBidAskOrder> Buys { get; set; }
        ConcurrentDictionary<string, decimal> Matches { get; set; }
        long Sequence { get; set; }
    }
}
