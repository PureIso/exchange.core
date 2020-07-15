using exchange.core.models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.Enums;
using exchange.core.Models;

namespace exchange.core.interfaces
{
    /// <summary>
    /// All exchange services have to implement the IExchangeService interface
    /// This interface will also be used to forward Hub Requests
    /// </summary>
    public interface IExchangeService
    {
        #region Actions
        Action<Feed> FeedBroadcast { get; set; }
        Action<MessageType, string> ProcessLogBroadcast { get; set; }
        #endregion

    }
}
