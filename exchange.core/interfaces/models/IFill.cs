using System;
using exchange.core.enums;

namespace exchange.core.interfaces.models
{
    public interface IFill
    {
        string ID { get; set; }
        string OrderID { get; set; }
        string Symbol { get; set; } 
        decimal Price { get; set; } 
        decimal Size { get; set; }
        OrderSide Side { get; set; }
        decimal Fee { get; set; }
        string FeeSymbol { get; set; }
        DateTime DateTime { get; set; }
        bool Settled { get; set; }
    }
}
