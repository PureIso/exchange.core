using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using exchange.core.Enums;
using exchange.core.models;
using exchange.core.Models;

namespace exchange.core.implementations
{
    public abstract class AbstractExchangePlugin
    {
        public AbstractExchangePlugin()
        {
            AccountInfo = new Dictionary<string, decimal>();
            CurrentPrices = new Dictionary<string, decimal>();
            SubscribedPrices = new Dictionary<string, decimal>();
            ConnectionAdapter = new ConnectionAdapter();
            Products = new List<Product>();
        }

        #region Actions

        public virtual Action<string, Feed> FeedBroadcast { get; set; }
        public virtual Action<string, MessageType, string> ProcessLogBroadcast { get; set; }


        public virtual Func<string, Dictionary<string, decimal>, Task> NotifyAccountInfo { get; set; }
        public virtual Func<string, Dictionary<string, decimal>, Task> NotifyCurrentPrices { get; set; }


        public virtual Action<string, Dictionary<string, string>> TechnicalIndicatorInformationBroadcast { get; set; }

        #endregion

        #region Virtual Properties
        public virtual Dictionary<string, decimal> SubscribedPrices { get; set; }
        public virtual Dictionary<string, decimal> CurrentPrices { get; set; }
        public virtual Dictionary<string, decimal> AccountInfo { get; set; }

        public Feed CurrentFeed { get; set; }
        public virtual Authentication Authentication { get; set; }
        public virtual ClientWebSocket ClientWebSocket { get; set; }
        public virtual ConnectionAdapter ConnectionAdapter { get; set; }
        public virtual string ApplicationName { get; set; }
        public virtual string Description { get; set; }
        public virtual string Author { get; set; }
        public virtual string Version { get; set; }
        public virtual List<Product> Products { get; set; }
        public virtual List<Product> SubscribeProducts { get; set; }

        public virtual string INIFilePath { get; set; }
        public bool TestMode { get; set; }

        public virtual Task ChangeFeed(List<Product> product)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<bool> CloseFeed()
        {
            bool isClosed = false;
            try
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.General, "Closing Feed Subscription.");
                isClosed = await ConnectionAdapter?.WebSocketCloseAsync();
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(ApplicationName, MessageType.Error,
                    $"Method: CloseFeed\r\nException Stack Trace: {e.StackTrace}");
            }

            return isClosed;
        }

        public virtual void Dispose()
        {
            ConnectionAdapter?.Dispose();
            CurrentPrices = null;
            Products = null;
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        public virtual Task<bool> InitAsync(bool testMode)
        {
            throw new NotImplementedException();
        }

        public virtual bool InitIndicatorsAsync()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Virtual Methods

        public virtual Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(
            HistoricCandlesSearch historicCandlesSearch)
        {
            throw new NotImplementedException();
        }

        public Task RequestedAccountInfo()
        {
            throw new NotImplementedException();
        }

        public Task RequestedCurrentPrices()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}