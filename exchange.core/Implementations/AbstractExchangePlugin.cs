using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using exchange.core.enums;
using exchange.core.helpers;
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
        public virtual Func<string, FillStatistics, Task> NotifyFillStatistics { get; set; }
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
        public virtual string FileName { get; set; }
        public virtual OrderBook OrderBook { get; set; }
        public virtual List<HistoricRate> HistoricRates { get; set; }
        public virtual Product SelectedProduct { get; set; }
        public virtual List<Ticker> Tickers { get; set; }
        public virtual List<Account> Accounts { get; set; }
        public virtual Authentication Authentication { get; set; }
        public virtual ClientWebSocket ClientWebSocket { get; set; }
        public virtual ConnectionAdapter ConnectionAdapter { get; set; }
        public virtual string ApplicationName { get; set; }
        public virtual string Description { get; set; }
        public virtual string Author { get; set; }
        public virtual string Version { get; set; }
        public virtual List<Product> Products { get; set; }
        public virtual Dictionary<string, FillStatistics> FillStatistics { get; set; }
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
        public virtual Task<List<Order>> CancelAllOrdersAsync(Product product)
        {
            throw new NotImplementedException();
        }
        public virtual Task<List<Order>> CancelOrderAsync(Order order)
        {
            throw new NotImplementedException();
        }
        public virtual Task<List<Order>> UpdateOrdersAsync(Product product = null)
        {
            throw new NotImplementedException();
        }
        public virtual Task<List<Fill>> UpdateFillsAsync(Product product)
        {
            throw new NotImplementedException();
        }
        public virtual async Task<FillStatistics> UpdateFillStatistics(Product product, List<Fill> fills=null)
        {
            Product validatedProduct = Products.FirstOrDefault(p => p.ID == product.ID);
            if (validatedProduct == null)
                return null;
            //We want to get the last price we sold
            //This will allow us to find out what price range to buy
            fills ??= await UpdateFillsAsync(validatedProduct);
            List<MiniFill> miniFillSellAboveList = new List<MiniFill>();
            List<MiniFill> miniFillBuyBelowList = new List<MiniFill>();
            //Quote Currency Balance: example EUR / BTC
            Account selectedQuoteCurrencyAccount = Accounts.FirstOrDefault(account => account.Currency == validatedProduct.QuoteCurrency);
            if (selectedQuoteCurrencyAccount == null)
                return null;
            //Base Currency Balance: example BTC / ETH
            Account selectedBaseCurrencyAccount = Accounts.FirstOrDefault(account => account.Currency == validatedProduct.BaseCurrency);
            if (selectedBaseCurrencyAccount == null)
                return null;
            bool buyCompleted = false;
            bool sellCompleted = false;
            decimal quoteCurrencyAccumulatedBalance = 0;
            decimal baseCurrencyAccumulatedBalance = 0;
            foreach (Fill fill in fills)
            {
                //Get the final fill price
                decimal fillPrice;
                decimal fee;
                switch (fill.Side)
                {
                    case "sell" when !sellCompleted:
                        {
                            //Quote currency fee EUR
                            fee = fill.Fee.ToDecimal();
                            //Quote currency price EUR
                            fillPrice = fill.Price.ToDecimal();
                            //Quote currency total fee EUR
                            decimal buyBelowPrice = fillPrice - (fee / fill.Size.ToDecimal());
                            quoteCurrencyAccumulatedBalance += fillPrice * fill.Size.ToDecimal() + fee;
                            if (quoteCurrencyAccumulatedBalance > selectedQuoteCurrencyAccount.Balance.ToDecimal())
                            {
                                sellCompleted = true;
                            }
                            miniFillBuyBelowList.Add(new MiniFill { Price = buyBelowPrice, Size = fill.Size.ToDecimal() });
                            break;
                        }
                    case "buy" when !buyCompleted:
                        {
                            //Quote currency fee EUR
                            fee = fill.Fee.ToDecimal();
                            //Quote currency price EUR
                            fillPrice = fill.Price.ToDecimal();
                            //Quote currency total fee EUR
                            decimal sellAbovePrice = (fee / fill.Size.ToDecimal()) + fillPrice;
                            baseCurrencyAccumulatedBalance += fill.Size.ToDecimal();
                            if (baseCurrencyAccumulatedBalance > selectedBaseCurrencyAccount.Balance.ToDecimal())
                            {
                                decimal sizeDifference = baseCurrencyAccumulatedBalance -
                                                         selectedBaseCurrencyAccount.Balance.ToDecimal();
                                decimal fillSizeDifference = fill.Size.ToDecimal() - sizeDifference;
                                if(fillSizeDifference <= 0)
                                    fillSizeDifference = 1;
                                sellAbovePrice = (fee / fillSizeDifference) + fillPrice;
                                buyCompleted = true;
                            }
                            miniFillSellAboveList.Add(new MiniFill { Price = sellAbovePrice, Size = fill.Size.ToDecimal() });
                            break;
                        }
                }
            }
            FillStatistics fillStatistics = new FillStatistics
            {
                BaseCurrency = selectedBaseCurrencyAccount.Currency,
                MiniFillBuyBelowList = miniFillBuyBelowList,
                MiniFillSellAboveList = miniFillSellAboveList,
                ProductID = validatedProduct.ID,
                QuoteCurrency = selectedQuoteCurrencyAccount.Currency
            };
            FillStatistics ??= new Dictionary<string, FillStatistics>();
            if (!FillStatistics.ContainsKey(validatedProduct.ID))
                FillStatistics.Add(validatedProduct.ID, fillStatistics);
            else
                FillStatistics[validatedProduct.ID] = fillStatistics;
            NotifyFillStatistics?.Invoke(ApplicationName, fillStatistics);
            return fillStatistics;
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