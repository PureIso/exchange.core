using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using exchange.core.connectivity;
using exchange.core.interfaces.clients;
using exchange.core.interfaces.models;
using Newtonsoft.Json.Linq;

namespace exchange.core.implementations.clients
{
    public abstract class AbstractAccountClient : IAccountClient
    {
        #region Fields
        private IConnection _connection;
        private IRequest _request;
        #endregion

        #region Properties
        public virtual List<IAccount> Accounts { get; set; }
        #endregion

        public virtual void InitialiseAccounts(IConnection connection)
        {
            _connection = connection;
        }

        public virtual List<IAccount> GetAccounts(IRequest request)
        {
            return null;
            //string json =  _connection.RequestAsync(_request).Result;
            //if (string.IsNullOrWhiteSpace(json))
            //    return Accounts;

            //JObject jObject = JObject.Parse(json);
            //if (jObject["balances"] == null)
            //    return null;
            //JToken balancesToken = jObject["balances"];
            //List<IAccount> accounts = null;
            //if (balancesToken is JArray)
            //    accounts = balancesToken.Select(x => IAccount.FromJToken(AllAccounts, x)).ToList();
            //if (accounts == null)
            //    return null;
            //Accounts = accounts.Where(x => x.Balance > 0 && (x.Trading || x.Currency == "BTC")).ToList();
            //return Accounts;


            //JToken token = JToken.Parse(json);
            //switch (token)
            //{
            //    case JArray _:
            //        JArray jArray = JArray.Parse(json);
            //        Accounts = jArray.Select(IAccount.FromJToken).ToList();
            //        Accounts.RemoveAll(x => x?.Currency == null);
            //        break;
            //    case JObject _:
            //        Account account = new Account(token);
            //        if (account.ID == null)
            //            return null;
            //        Accounts = new List<Account>
            //        {
            //            account
            //        };
            //        Accounts.RemoveAll(x => x?.Currency == null);
            //        break;
            //}

            //return Accounts;
        }

        private static IAccount FromJToken(JToken jToken)
        {
            return null;
        }
        private static IAccount FromJToken(IAccount account, JToken jToken)
        {
            return null;
        }
        private static IAccount FromJToken(List<IAccount> accounts, JToken jToken)
        {
            return null;
            //if (jToken["free"] == null ||
            //    jToken["asset"] == null ||
            //    jToken["locked"] == null)
            //    return null;
            //IAccount account = null;
            //if (accounts != null)
            //    account = accounts.FirstOrDefault(x => x.Currency == jToken["asset"].Value<string>());
            //if (account == null)
            //{
            //    account = new Account
            //    {
            //        Currency = jToken["asset"].Value<string>(),
            //        Balance = Math.Round(jToken["free"].Value<decimal>(), 8) +
            //                  Math.Round(jToken["locked"].Value<decimal>(), 8),
            //        Available = Math.Round(jToken["free"].Value<decimal>(), 8)
            //    };
            //}
            //else
            //{
            //    account.Currency = jToken["asset"].Value<string>();
            //    account.Balance = Math.Round(jToken["free"].Value<decimal>(), 8) +
            //                      Math.Round(jToken["locked"].Value<decimal>(), 8);
            //    account.Available = Math.Round(jToken["free"].Value<decimal>(), 8);
            //}

            //return account;
        }
    }
}
