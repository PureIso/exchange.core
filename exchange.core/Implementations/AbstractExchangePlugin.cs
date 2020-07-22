using exchange.core.Enums;
using exchange.core.interfaces;
using exchange.core.Interfaces;
using exchange.core.models;
using exchange.core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace exchange.core
{
    public abstract class AbstractExchangePlugin : IExchangeService
    {
        #region Virtual Properties
        public virtual string ApplicationName { get; set; }
        public virtual string Description { get; set; }
        public virtual string Author { get; set; }
        public virtual string Version { get; set; }
        public virtual Action<Feed> FeedBroadcast { get; set; }
        public virtual Action<MessageType, string> ProcessLogBroadcast { get; set; }
        public virtual Dictionary<string, decimal> CurrentPrices { get; set; }
        public virtual List<Product> Products { get; set; }
        public virtual Action<Dictionary<string, string>> TechnicalIndicatorInformationBroadcast { get; set; }

        public virtual bool ChangeFeed(string message)
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> CloseFeed()
        {
            throw new NotImplementedException();
        }

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }

        public virtual Task<bool> InitAsync()
        {
            throw new NotImplementedException();
        }

        public virtual bool InitConnectionAdapter(IConnectionAdapter connectionAdapter)
        {
            throw new NotImplementedException();
        }

        public virtual bool InitIndicatorsAsync()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Virtual Methods

        public virtual Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(HistoricCandlesSearch historicCandlesSearch)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
