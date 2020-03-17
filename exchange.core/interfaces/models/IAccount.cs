using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace exchange.core.interfaces.models
{
    public interface IAccount
    {
        bool Trading { get; set; }
        string ID { get; set; } 
        string Currency { get; set; } 
        decimal Balance { get; set; } 
        decimal Hold { get; set; } 
        decimal Available { get; set; } 
        decimal MinimumPrice { get; set; }
        decimal MaximumPrice { get; set; }
        decimal MinimumQuantity { get; set; }
        decimal MaximumQuantity { get; set; }
        decimal StepSize { get; set; }
        decimal CurrentPrice { get; set; }
        decimal TickSize { get; set; }

        string ToReadableString();
    }
}
