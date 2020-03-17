using exchange.core.interfaces.models;

namespace exchange.core.implementations.models
{
    public class BidAskOrder : IBidAskOrder
    {
        public decimal Price { get; set; }
        public decimal Size { get; set; }
    }
}
