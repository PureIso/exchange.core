namespace exchange.core.interfaces
{
    public interface IExchangeSettings
    {
        public bool TestMode { get; set; }
        public string IndicatorDirectoryPath { get; set; }
        public string INIDirectoryPath { get; set; }
        public string MainCurrency { get; set; }
    }
}