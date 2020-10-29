using exchange.core.interfaces;

namespace exchange.core.implementations
{
    public class ExchangeSettings : IExchangeSettings
    {
        public bool TestMode { get; set; }
        public string IndicatorDirectoryPath { get; set; }
        public string INIDirectoryPath { get; set; }
    }
}