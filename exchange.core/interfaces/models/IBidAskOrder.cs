namespace exchange.core.interfaces.models
{
    public interface IBidAskOrder
    {
        decimal Price { get; set; }
        decimal Size { get; set; }
    }
}
