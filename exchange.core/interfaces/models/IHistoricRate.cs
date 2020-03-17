using System;

namespace exchange.core.interfaces.models
{
    public interface IHistoricRate
    {
        IProduct Product { get; set; }
        DateTime DateAndTime { get; set; }
        decimal Low { get; set; }
        decimal High { get; set; }
        decimal Open { get; set; }
        decimal Close { get; set; }
        decimal Volume { get; set; }
    }
}
