using System;

namespace exchange.core.Models
{
    public class HistoricCandlesSearch
    {
        public string Symbol { get; set; }
        public DateTime StartingDateTime { get; set; }
        public DateTime EndingDateTime { get; set; }
        public Granularity Granularity { get; set; }
    }
}