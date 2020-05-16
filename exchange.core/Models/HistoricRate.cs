using System;

namespace exchange.core.models
{
    public class HistoricRate
    {
        #region Properties
        public DateTime DateAndTime { get; set; }
        public decimal Low { get; set; }
        public decimal High { get; set; }
        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        #endregion
    }
}
