using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.interfaces.models;
using Newtonsoft.Json.Linq;

namespace exchange.core.interfaces.clients
{
    public interface IFillClient
    {
        List<IFill> Fills { get; set; }

        Task<List<IFill>> InitialiseFills();
        Task<List<IFill>> GetFills();
        IFill FromJToken(JToken jToken);
        IFill FromJToken(IFill fill, JToken jToken);
        IFill FromJToken(List<IFill> fills, JToken jToken);
    }
}
