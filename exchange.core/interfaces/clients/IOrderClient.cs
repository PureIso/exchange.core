using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.interfaces.models;
using Newtonsoft.Json.Linq;

namespace exchange.core.interfaces.clients
{
    public interface IOrderClient
    {
        List<IOrder> Orders { get; set; }

        Task<List<IOrder>> InitialiseOrders();

        bool CancelOrders(List<IOrder> orders);
        bool PlaceOrders(List<IOrder> orders);

        IOrder FromJToken(JToken jToken);
        IOrder FromJToken(IFill order, JToken jToken);
        IOrder FromJToken(List<IFill> orders, JToken jToken);
    }
}
