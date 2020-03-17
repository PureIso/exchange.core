using System;
using System.Collections.Generic;
using System.Linq;
using exchange.core.interfaces.models;
using Newtonsoft.Json.Linq;

namespace exchange.core.implementations.models
{
    public class Account : IAccount
    {
        public Account() { }

        #region Properties
        public string ID { get; set; }
        public string Currency { get; set; }
        public decimal Balance { get; set; }
        public decimal Hold { get; set; }
        public decimal Available { get; set; }
        public decimal MinimumPrice { get; set; }
        public decimal MaximumPrice { get; set; }
        public decimal MinimumQuantity { get; set; }
        public decimal MaximumQuantity { get; set; }
        public decimal StepSize { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TickSize { get; set; }
        public bool Trading { get; set; }
        #endregion

        public string ToReadableString()
        {
            return $"{Balance:F4} {Currency}";
        }

        #region Static Methods
        public static Account FromJToken(JToken jToken)
        {
            if (jToken["id"] == null || jToken["currency"] == null ||
                jToken["hold"] == null || jToken["balance"] == null ||
                jToken["available"] == null) return null;
            decimal balance = Math.Round(jToken["balance"].Value<decimal>(), 8);
            if (balance <= 0 && jToken["currency"].Value<string>() != "EUR" && jToken["currency"].Value<string>() != "BTC")
                return null;
            Account account = new Account
            {
                ID = jToken["id"].Value<string>(),
                Currency = jToken["currency"].Value<string>(),
                Hold = Math.Round(jToken["hold"].Value<decimal>(), 8),
                Balance = balance,
                Available = Math.Round(jToken["available"].Value<decimal>(), 8)
            };
            return account;
        }
        public static Account FromJToken(List<Account> accounts, JToken jToken)
        {
            if (jToken["free"] == null ||
                jToken["asset"] == null ||
                jToken["locked"] == null)
                return null;
            Account account = null;
            if (accounts != null)
                account = accounts.FirstOrDefault(x => x.Currency == jToken["asset"].Value<string>());
            if (account == null)
            {
                account = new Account
                {
                    Currency = jToken["asset"].Value<string>(),
                    Balance = Math.Round(jToken["free"].Value<decimal>(), 8) +
                              Math.Round(jToken["locked"].Value<decimal>(), 8),
                    Available = Math.Round(jToken["free"].Value<decimal>(), 8)
                };
            }
            else
            {
                account.Currency = jToken["asset"].Value<string>();
                account.Balance = Math.Round(jToken["free"].Value<decimal>(), 8) +
                                  Math.Round(jToken["locked"].Value<decimal>(), 8);
                account.Available = Math.Round(jToken["free"].Value<decimal>(), 8);
            }

            return account;
        }
        public static Account FromJToken(List<Account> accounts, JToken jToken, string quoteAsset)
        {
            if (jToken["status"] == null || jToken["quoteAsset"] == null ||
                jToken["baseAsset"] == null || jToken["filters"] == null)
                return null;
            if (jToken["quoteAsset"].Value<string>() != quoteAsset)
                return null;
            Account account = accounts.FirstOrDefault(x => x.Currency == jToken["baseAsset"].Value<string>());
            if (account == null)
                return null;
            account.Trading = jToken["status"].Value<string>() == "TRADING";

            JToken filtersToken = jToken["filters"];
            if (filtersToken is JArray)
                foreach (JToken filterToken in filtersToken)
                {
                    if (filterToken["filterType"] == null)
                        continue;
                    if (filterToken["filterType"].Value<string>() == "PRICE_FILTER")
                    {
                        if (filterToken["minPrice"] == null ||
                            filterToken["maxPrice"] == null ||
                            filterToken["tickSize"] == null)
                            continue;
                        account.MinimumPrice = Math.Round(filterToken["minPrice"].Value<decimal>(), 8);
                        // account.MaximumPrice = Math.Round(filterToken["maxPrice"].Value<decimal>(), 8);
                        account.TickSize = Math.Round(filterToken["tickSize"].Value<decimal>(), 8);
                    }
                    else if (filterToken["filterType"].Value<string>() == "LOT_SIZE")
                    {
                        //if (filterToken["minQty"] == null ||
                        //    filterToken["maxQty"] == null ||
                        //    filterToken["stepSize"] == null)
                        //    continue;
                        //account.MinimumQuantity = Math.Round(filterToken["minQty"].Value<decimal>(), 8);
                        //account.MaximumQuantity = Math.Round(filterToken["maxQty"].Value<decimal>(), 8);
                        //account.StepSize = Math.Round(filterToken["stepSize"].Value<decimal>(), 8);
                    }
                }

            return account;
        }
        #endregion
    }
}
