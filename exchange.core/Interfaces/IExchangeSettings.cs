namespace exchange.core.interfaces
{
    public interface IExchangeSettings
    {
        public bool TestMode { get; set; }
        public string IndicatorSavePath { get; set; }
    }
}