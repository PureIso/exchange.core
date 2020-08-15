using System.Collections.Generic;
using exchange.core.implementations;

namespace exchange.core.interfaces
{
    /// <summary>
    /// All exchange services have to implement the IExchangeService interface
    /// This interface will also be used to forward Hub Requests
    /// </summary>
    public interface IExchangePluginService
    {

        #region Properties
        List<AbstractExchangePlugin> PluginExchanges { get; }
        #endregion


        #region Methods
        void AddPluginFromFolder(string folderPath);
        #endregion
    }
}