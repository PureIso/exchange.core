using exchange.core.interfaces;

namespace exchange.core.implementations
{
    public class ExchangeSettings : IExchangeSettings
    {
        public bool TestMode { get; set; }
        public string IndicatorSavePath { get; set; }
        public string INIFilePath { get; set; }
    }
}