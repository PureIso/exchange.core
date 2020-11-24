using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using exchange.core.enums;
using exchange.core.indicators;
using exchange.core.interfaces;
using exchange.core.models;

namespace exchange.core.implementations
{
    public abstract class AbstractExchangePlugin
    {
        #region Actions

        //public virtual Action<string, Feed> FeedBroadcast { get; set; }
        public virtual Action<string, MessageType, string> ProcessLogBroadcast { get; set; }
        public virtual Func<string, Dictionary<string, decimal>, Task> NotifyAccountInfo { get; set; }
        public virtual Func<string, Dictionary<string, decimal>, Task> NotifyCurrentPrices { get; set; }
        public virtual Func<string, Dictionary<string, AssetInformation>, Task> NotifyAssetInformation { get; set; }
        public virtual Func<string, string, Task> NotifyMainCurrency { get; set; }
        public virtual Func<string, List<Fill>, Task> NotifyFills { get; set; }
        public virtual Func<string, List<Order>, Task> NotifyOrders { get; set; }
        public virtual Action<string, Dictionary<string, string>> TechnicalIndicatorInformationBroadcast { get; set; }
        #endregion

        #region Virtual Properties
        public virtual Dictionary<string, AssetInformation> AssetInformation { get; set; }
        public virtual List<RelativeStrengthIndex> RelativeStrengthIndices { get; set; }
        public virtual Dictionary<string, decimal> SubscribedPrices { get; set; }
        public virtual Dictionary<string, decimal> CurrentPrices { get; set; }
        public virtual Dictionary<string, decimal> AccountInfo { get; set; }
        public virtual Dictionary<string, Statistics> Statistics { get; set; }
        public virtual Feed CurrentFeed { get; set; }
        public virtual List<Fill> Fills { get; set; }
        public virtual Authentication Authentication { get; set; }
        public virtual ClientWebSocket ClientWebSocket { get; set; }
        public virtual ConnectionAdapter ConnectionAdapter { get; set; }
        public virtual string ApplicationName { get; set; }
        public virtual string Description { get; set; }
        public virtual string Author { get; set; }
        public virtual string Version { get; set; }
        public virtual List<Product> Products { get; set; }
        public virtual List<Order> Orders { get; set; }
        public virtual List<Product> SubscribeProducts { get; set; }
        public virtual string IndicatorSaveDataPath { get; set; }
        public virtual string INIFilePath { get; set; }
        public virtual string MainCurrency { get; set; }
        public virtual bool TestMode { get; set; }
        #endregion

        #region Virtual Methods
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
                if (ConnectionAdapter != null)
                    isClosed = await ConnectionAdapter?.WebSocketCloseAsync();
                else return true;
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
        public virtual Task<List<Fill>> UpdateFillsAsync(Product product)
        {
            throw new NotImplementedException();
        }
        public virtual Task<Order> PostOrdersAsync(Order order)
        {
            throw new NotImplementedException();
        }
        public virtual Task<bool> InitAsync(IExchangeSettings exchangeSettings)
        {
            throw new NotImplementedException();
        }
        public virtual bool InitIndicatorsAsync(List<Product> products)
        {
            throw new NotImplementedException();
        }
        public virtual Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(
            HistoricCandlesSearch historicCandlesSearch)
        {
            throw new NotImplementedException();
        }
        public virtual Task<Statistics> TwentyFourHoursRollingStatsAsync(Product product)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}