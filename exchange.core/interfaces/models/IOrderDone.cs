
using exchange.core.enums;

namespace exchange.core.interfaces.models
{
    public interface IOrderDone
    {
        string ID { get; set; }
        decimal RemainingSize { get; set; }
        OrderSide OrderSide { get; set; }
        string Reason { get; set; }
    }
}
