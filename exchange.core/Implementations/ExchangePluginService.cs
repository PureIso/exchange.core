using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using exchange.core.implementations;
using exchange.core.interfaces;

namespace exchange.service.Plugins
{
    public class ExchangePluginService : IExchangePluginService
    {
        #region Constants
        private const string AssemblyBaseTypeFullName = "exchange.core.implementations.abstractexchangeplugin";
        #endregion

        #region Fields
        private readonly List<AbstractExchangePlugin> _pluginExchanges = new List<AbstractExchangePlugin>();
        #endregion

        #region Properties
        public List<AbstractExchangePlugin> PluginExchanges
        {
            get
            {
                _pluginExchanges.Sort((x, y) => string.CompareOrdinal(y.ApplicationName, x.ApplicationName));
                return _pluginExchanges;
            }
        }
        #endregion

        #region Methods
        public void AddPluginFromFolder(string folderPath)
        {
            try
            {
                foreach (string plugin in Directory.GetFiles(folderPath))
                {
                    FileInfo file = new FileInfo(plugin);
                    if (file.Extension.Equals(".dll"))
                    {
                        Assembly pluginAssembly = Assembly.LoadFrom(plugin); //Load assembly given its full name and path

                        foreach (Type pluginType in pluginAssembly.GetTypes())
                        {
                            if (!pluginType.IsPublic) continue; //break the for each loop to next iteration if any
                            if (pluginType.IsAbstract) continue; //break the for each loop to next iteration if any
                            //search for specified interface while ignoring case sensitivity
                            if (pluginType.BaseType == null ||
                                pluginType.BaseType.FullName.ToLower() != AssemblyBaseTypeFullName)
                                continue;
                            //New plug-in information setting
                            AbstractExchangePlugin pluginInterfaceInstance =
                                (AbstractExchangePlugin)(Activator.CreateInstance(pluginAssembly.GetType(pluginType.ToString())));
                            _pluginExchanges.Add(pluginInterfaceInstance);
                        }
                    }
                }
                
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        #endregion
    }
}
