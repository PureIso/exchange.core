namespace exchange.core.interfaces.models
{
    public interface IProduct
    { 
        string ID { get; set; } 
        string Currency { get; set; } 
        string QuoteCurrency { get; set; } 
        decimal MinimumSize { get; set; } 
        decimal MaximumSize { get; set; } 
        decimal Increment { get; set; }
    }
}
