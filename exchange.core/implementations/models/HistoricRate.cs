using System;
using exchange.core.interfaces.models;
using Newtonsoft.Json.Linq;

namespace exchange.core.implementations.models
{
    public class HistoricRate : IHistoricRate
    {
        #region Properties
        public IProduct Product { get; set; }
        public DateTime DateAndTime { get; set; }
        public decimal Low { get; set; }
        public decimal High { get; set; }
        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        #endregion

        public HistoricRate()
        {
        }

        public HistoricRate(JToken jToken)
        {
            if (!(jToken is JArray)) 
                return;
            DateTime unix = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            DateAndTime = unix.AddSeconds(jToken[0].Value<long>());
            Low = jToken[1].Value<decimal>();
            High = jToken[2].Value<decimal>();
            Open = jToken[3].Value<decimal>();
            Close = jToken[4].Value<decimal>();
            Volume = jToken[5].Value<decimal>();
        }
    }
}