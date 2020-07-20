using System;
using exchange.core.models;
using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.Enums;
using exchange.core.Models;
using exchange.core.Interfaces;

namespace exchange.core.interfaces
{
    /// <summary>
    /// All exchange services have to implement the IExchangeService interface
    /// This interface will also be used to forward Hub Requests
    /// </summary>
    public interface IExchangeService
    {
        #region Actions
        Action<Dictionary<string, string>> TechnicalIndicatorInformationBroadcast { get; set; }
        Action<Feed> FeedBroadcast { get; set; }
        Action<MessageType, string> ProcessLogBroadcast { get; set; }
        #endregion

        #region Properties
        Dictionary<string, decimal> CurrentPrices { get; set; }
        List<Product> Products { get; set; }
        #endregion

        #region Methods
        Task<bool> InitAsync();
        bool InitConnectionAdapter(IConnectionAdapter connectionAdapter);
        bool InitIndicatorsAsync();
        Task<bool> CloseFeed();
        bool ChangeFeed(string message);
        Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(HistoricCandlesSearch historicCandlesSearch);
        #endregion

        public void Dispose();
    }
}
