using System;
using exchange.core.models;
using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.Enums;
using exchange.core.Models;
using exchange.core.Interfaces;
using System.Net.WebSockets;
using exchange.core.implementations;
using exchange.core.Implementations;

namespace exchange.core.interfaces
{
    /// <summary>
    /// All exchange services have to implement the IExchangeService interface
    /// This interface will also be used to forward Hub Requests
    /// </summary>
    public interface IExchangePluginService
    {
        #region Constants
        private const string AssemblyBaseTypeFullName = "exchange.core.abstractexchangeplugin";
        #endregion

        #region Properties
        List<AbstractExchangePlugin> PluginExchanges { get; }
        #endregion


        #region Methods
        void AddPluginFromFolder(string folderPath);
        void AddPlugin(string pluginPath);
        #endregion
    }
}