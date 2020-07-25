using exchange.core.Enums;
using exchange.core.implementations;
using exchange.core.interfaces;
using exchange.core.models;
using exchange.core.Models;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace exchange.core
{
    public abstract class AbstractExchangePlugin : IExchangeService
    {
        #region Virtual Properties
        public virtual Authentication Authentication { get; set; }
        public virtual ClientWebSocket ClientWebSocket { get; set; }
        public virtual ConnectionAdapter ConnectionAdapter { get; set; }
        public virtual string ApplicationName { get; set; }
        public virtual string Description { get; set; }
        public virtual string Author { get; set; }
        public virtual string Version { get; set; }
        public virtual Action<Feed> FeedBroadcast { get; set; }
        public virtual Action<MessageType, string> ProcessLogBroadcast { get; set; }
        public virtual Dictionary<string, decimal> CurrentPrices { get; set; }
        public virtual List<Product> Products { get; set; }
        public virtual Action<Dictionary<string, string>> TechnicalIndicatorInformationBroadcast { get; set; }
        public virtual string INIFilePath { get; set; }

        public virtual bool ChangeFeed(string message)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<bool> CloseFeed()
        {
            bool isClosed = false;
            try
            {
                ProcessLogBroadcast?.Invoke(MessageType.General, $"Closing Feed Subscription.");
                isClosed = await ConnectionAdapter?.WebSocketCloseAsync();
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
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

        public virtual Task<bool> InitAsync()
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
