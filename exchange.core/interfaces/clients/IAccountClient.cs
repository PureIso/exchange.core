using System.Collections.Generic;
using exchange.core.connectivity;
using exchange.core.interfaces.models;

namespace exchange.core.interfaces.clients
{
    public interface IAccountClient
    {
        List<IAccount> Accounts { get; set; }
        void InitialiseAccounts(IConnection connection);
        List<IAccount> GetAccounts(IRequest request);
    }
}
