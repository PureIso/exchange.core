using System;

namespace exchange.core.interfaces.models
{
    public interface ITicker
    {
        IProduct Product { get; set; }
        decimal Size { get; set; } 
        decimal Price { get; set; } 
        decimal Bid { get; set; } 
        decimal Ask { get; set; } 
        decimal Volume { get; set; } 
        DateTime Time { get; set; }
    }
}
