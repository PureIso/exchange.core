using exchange.core.enums;

namespace exchange.core.interfaces.models
{
    public interface IOrder
    {
        string ID { get; set; }
        decimal Size { get; set; }
        decimal Price { get; set; }
        decimal Fees { get; set; }
        IProduct Product { get; set; }
        OrderSide Side { get; set; }
        string Status { get; set; }
        OrderType Type { get; set; }
    }
}
