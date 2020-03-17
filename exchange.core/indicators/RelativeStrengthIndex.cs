using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using exchange.core.interfaces.models;
using Newtonsoft.Json;

namespace exchange.core.indicators
{
    [Serializable]
    public class RelativeStrengthIndex : IDatabase
    {
        #region Fields
        [NonSerialized]
        private Timer _updater;
        [NonSerialized]
        private IExchange _exchange;
        [NonSerialized]
        private string _databaseFilePath;
        #endregion

        #region Properties
        [JsonIgnore]
        public string FileName { get; set; }
        [JsonProperty]
        public string HistoricChartHourlyLastDateTime { get; set; }
        [JsonProperty]
        public decimal HistoricChartHourlyPreviousHistoricRateClose { get; set; }
        [JsonProperty]
        public decimal HistoricChartHourlyPreviousHistoricRateOpen { get; set; }
        [JsonProperty]
        public decimal HistoricChartHourlyPreviousHistoricRateLow { get; set; }
        [JsonProperty]
        public decimal HistoricChartHourlyPreviousHistoricRateHigh { get; set; }
        [JsonProperty]
        public decimal HistoricChartHourlyPreviousHistoricRateVolume { get; set; }
        [JsonProperty]
        public decimal HistoricChartHourlyAverageGain { get; set; }
        [JsonProperty]
        public decimal HistoricChartHourlyAverageLoss { get; set; }
        [JsonProperty]
        public int HistoricChartHourlyCurrentPeriodCount { get; set; }
        [JsonProperty]
        public decimal RelativeIndexDaily { get; set; }
        [JsonProperty]
        public string HistoricChartDailyLastDateTime { get; set; }
        [JsonProperty]
        public decimal HistoricChartDailyPreviousHistoricRateClose { get; set; }
        [JsonProperty]
        public decimal HistoricChartDailyPreviousHistoricRateOpen { get; set; }
        [JsonProperty]
        public decimal HistoricChartDailyPreviousHistoricRateLow { get; set; }
        [JsonProperty]
        public decimal HistoricChartDailyPreviousHistoricRateHigh { get; set; }
        [JsonProperty]
        public decimal HistoricChartDailyPreviousHistoricRateVolume { get; set; }
        [JsonProperty]
        public decimal HistoricChartDailyAverageGain { get; set; }
        [JsonProperty]
        public decimal HistoricChartDailyAverageLoss { get; set; }
        [JsonProperty]
        public int HistoricChartDailyCurrentPeriodCount { get; set; }
        [JsonProperty]
        public decimal RelativeIndexHourly { get; set; }
        [JsonProperty]
        public string HistoricChartQuarterlyLastDateTime { get; set; }
        [JsonProperty]
        public decimal HistoricChartQuarterlyPreviousHistoricRateClose { get; set; }
        [JsonProperty]
        public decimal HistoricChartQuarterlyPreviousHistoricRateOpen { get; set; }
        [JsonProperty]
        public decimal HistoricChartQuarterlyPreviousHistoricRateLow { get; set; }
        [JsonProperty]
        public decimal HistoricChartQuarterlyPreviousHistoricRateHigh { get; set; }
        [JsonProperty]
        public decimal HistoricChartQuarterlyPreviousHistoricRateVolume { get; set; }
        [JsonProperty]
        public decimal HistoricChartQuarterlyAverageGain { get; set; }
        [JsonProperty]
        public decimal HistoricChartQuarterlyAverageLoss { get; set; }
        [JsonProperty]
        public int HistoricChartQuarterlyCurrentPeriodCount { get; set; }
        [JsonProperty]
        public decimal RelativeIndexQuarterly { get; set; }

        #endregion

        public RelativeStrengthIndex(IExchange exchange,string databaseFilePath)
        {
            _updater = new Timer();
            _exchange = exchange;
            _databaseFilePath = databaseFilePath;
        }

        #region Public Methods
        public void Enable()
        {
            _updater.Elapsed += UpdateAsync;
            _updater.Interval = 600000;
            _updater.Enabled = true;
            _updater.Start();
            UpdateAsync(null, null);
        }
        public void Disable()
        {
            _updater.Enabled = false;
            _updater.Stop();
            _updater.Elapsed -= UpdateAsync;
        }
        #endregion

        #region Private Methods
        private void UpdateAsync(object source, ElapsedEventArgs e)
        {
            Task.WhenAll(
                Task.Run(ProcessHistoryChartDownload), 
                Task.Run(ProcessHistoryHourlyChartDownload),
                Task.Run(ProcessHistoryQuarterlyChartDownload));
        }
        private void ProcessHistoryChartDownload()
        {
            //Initialise fields
            const int period = 14;
            const int maPeriod = 7;
            string filename = $"coinbase_btc_eur_historic_data.csv";
            string initialData = "DateTime,High,Open,Close,Low,Volume,MA7,RSI14\n";
            int granularity = 86400;
            IProduct product = null;
            DateTime startingDateTime = new DateTime();
            DateTime endingDateTime = new DateTime();
            IHistoricRate previousHistoricRate = null;
            //RelativeIndexDaily = -1;
            decimal change = 0;
            decimal increases = 0;
            decimal decreases = 0;
            Queue<IHistoricRate> maQueue = new Queue<IHistoricRate>();
            //Check if we have an empty file or not
            if (string.IsNullOrWhiteSpace(HistoricChartDailyLastDateTime))
            {
                startingDateTime = new DateTime(2015, 4, 23).Date.ToUniversalTime();
                HistoricChartDailyAverageGain = 0;
                HistoricChartDailyAverageLoss = 0;
                HistoricChartDailyCurrentPeriodCount = 0;
            }
            else
            {
                startingDateTime = DateTime.Parse(HistoricChartDailyLastDateTime);
                //previousHistoricRate = new HistoricRate()
                //{
                //    Close = settingsHistoricChartPreviousHistoricRateClose,
                //    Open = settingsHistoricChartPreviousHistoricRateOpen,
                //    Low = settingsHistoricChartPreviousHistoricRateLow,
                //    High = settingsHistoricChartPreviousHistoricRateHigh,
                //    Volume = settingsHistoricChartPreviousHistoricRateVolume
                //};
            }

            //Begin data parsing
            DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            while (startingDateTime.Date < now)
            {
                endingDateTime = startingDateTime.AddMonths(6).ToUniversalTime();
                //Get the latest historic data
                List<IHistoricRate> result = _exchange.GetHistoryRates(null, startingDateTime, endingDateTime, granularity).Result;
                if (result == null || !result.Any()) break;
                result.Reverse();
                //Iterate though the historic data
                foreach (IHistoricRate rate in result)
                {
                    if (rate.DateAndTime.Date >= now)
                        break;
                    //Moving Average 7 days
                    if (maQueue.Count == maPeriod)
                        maQueue.Dequeue();
                    maQueue.Enqueue(rate);
                    //Calculate RSI 14 days
                    if (HistoricChartDailyCurrentPeriodCount > 0)
                    {
                        change = rate.Close - previousHistoricRate.Close;
                        if (change > 0)
                        {
                            increases += change;
                            if (HistoricChartDailyCurrentPeriodCount > period)
                            {
                                HistoricChartDailyAverageGain = ((HistoricChartDailyAverageGain * (period - 1)) + change) / period;
                                HistoricChartDailyAverageLoss = (HistoricChartDailyAverageLoss * (period - 1)) / period;
                            }
                        }
                        else if (change < 0)
                        {
                            decreases += (change * -1);
                            if (HistoricChartDailyCurrentPeriodCount > period)
                            {
                                HistoricChartDailyAverageGain = (HistoricChartDailyAverageGain * (period - 1)) / period;
                                HistoricChartDailyAverageLoss = ((HistoricChartDailyAverageLoss * (period - 1)) + (change * -1)) / period;
                            }
                        }
                        if (HistoricChartDailyCurrentPeriodCount >= period)
                        {
                            if (HistoricChartDailyCurrentPeriodCount == period)
                            {
                                HistoricChartDailyAverageGain = increases / period;
                                HistoricChartDailyAverageLoss = decreases / period;
                            }
                            if (HistoricChartDailyCurrentPeriodCount >= period)
                            {
                                RelativeIndexDaily = HistoricChartDailyAverageLoss == 0 ? 100 : Math.Round(100 - (100 / (1 + (HistoricChartDailyAverageGain / HistoricChartDailyAverageLoss))), 2);
                            }
                            //Generate data
                            initialData =
                            $"{rate.DateAndTime}," +
                            $"{rate.High}," +
                            $"{rate.Open}," +
                            $"{rate.Close}," +
                            $"{rate.Low}," +
                            $"{rate.Volume}," +
                            $"{maQueue.Average(x => x.Close)}," +
                            $"{RelativeIndexDaily}" +
                            $"\n";
                            WriteFile(filename, initialData);
                        }
                    }
                    previousHistoricRate = rate;
                    HistoricChartDailyCurrentPeriodCount++;
                }
                if (endingDateTime.Date > now)
                    startingDateTime = now;
                else
                    startingDateTime = endingDateTime.AddDays(1);
            }
        }
        private void ProcessHistoryHourlyChartDownload()
        {
            string fileName = $"coinbase_btc_eur_historic_data_hourly.csv";
            string data = "DateTime,High,Open,Close,Low,Volume,MA7,RSI14\n";
            //Validate file
            if (!File.Exists(fileName))
            {
                File.Create(fileName).Close();
                WriteFile(fileName, data);
            } 
            //Initialise fields
            const int period = 14;
            const int maPeriod = 7;
            int granularity = 3600;
            DateTime startingDateTime;
            DateTime endingDateTime;
            //IHistoricRate previousHistoricRate = new IHistoricRate()
            //{
            //    Close = 0,
            //    Open = 0,
            //    Low = 0,
            //    High = 0,
            //    Volume = 0
            //};
            //RelativeIndexHourly = -1;
            decimal change = 0;
            decimal increases = 0;
            decimal decreases = 0;
            Queue<IHistoricRate> maQueue = new Queue<IHistoricRate>();
            //Check if we have an empty file or not
            if (string.IsNullOrWhiteSpace(HistoricChartHourlyLastDateTime))
            {
                startingDateTime = new DateTime(2015, 4, 23).Date.ToUniversalTime();
                HistoricChartHourlyAverageGain = 0;
                HistoricChartHourlyAverageLoss = 0;
                HistoricChartHourlyCurrentPeriodCount = 0;
            }
            else
            {
                startingDateTime = DateTime.Parse(HistoricChartHourlyLastDateTime);
                //previousHistoricRate = new HistoricRate()
                //{
                //    Close = settingsHistoricChartPreviousHistoricRateClose,
                //    Open = settingsHistoricChartPreviousHistoricRateOpen,
                //    Low = settingsHistoricChartPreviousHistoricRateLow,
                //    High = settingsHistoricChartPreviousHistoricRateHigh,
                //    Volume = settingsHistoricChartPreviousHistoricRateVolume
                //};
            }
            //Begin data parsing
            DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0).ToUniversalTime();
            while (startingDateTime < now)
            {
                endingDateTime = startingDateTime.AddDays(6).ToUniversalTime();
                //Get the latest historic data
                List<IHistoricRate> result = _exchange.GetHistoryRates(null, startingDateTime, endingDateTime, granularity).Result;
                if (result == null || !result.Any()) break;
                result.Reverse();
                //Iterate though the historic data
                foreach (IHistoricRate rate in result)
                {
                    if (rate.DateAndTime >= now)
                        break;
                    //Moving Average 7 days
                    if (maQueue.Count == maPeriod)
                        maQueue.Dequeue();
                    maQueue.Enqueue(rate);
                    //Calculate RSI 14 days
                    if (HistoricChartHourlyCurrentPeriodCount > 0)
                    {
                        //change = rate.Close - previousHistoricRate.Close;
                        if (change > 0)
                        {
                            increases += change;
                            if (HistoricChartHourlyCurrentPeriodCount > period)
                            {
                                HistoricChartHourlyAverageGain = ((HistoricChartHourlyAverageGain * (period - 1)) + change) / period;
                                HistoricChartHourlyAverageLoss = (HistoricChartHourlyAverageLoss * (period - 1)) / period;
                            }
                        }
                        else if (change < 0)
                        {
                            decreases += (change * -1);
                            if (HistoricChartHourlyCurrentPeriodCount > period)
                            {
                                HistoricChartHourlyAverageGain = (HistoricChartHourlyAverageGain * (period - 1)) / period;
                                HistoricChartHourlyAverageLoss = ((HistoricChartHourlyAverageLoss * (period - 1)) + (change * -1)) / period;
                            }
                        }
                        if (HistoricChartHourlyCurrentPeriodCount >= period)
                        {
                            if (HistoricChartHourlyCurrentPeriodCount == period)
                            {
                                HistoricChartHourlyAverageGain = increases / period;
                                HistoricChartHourlyAverageLoss = decreases / period;
                            }
                            if (HistoricChartHourlyCurrentPeriodCount >= period)
                            {
                                RelativeIndexHourly = HistoricChartHourlyAverageLoss == 0 ? 100 : Math.Round(100 - (100 / (1 + (HistoricChartHourlyAverageGain / HistoricChartHourlyAverageLoss))), 2);
                            }
                            //Generate data
                            data =
                            $"{rate.DateAndTime}," +
                            $"{rate.High}," +
                            $"{rate.Open}," +
                            $"{rate.Close}," +
                            $"{rate.Low}," +
                            $"{rate.Volume}," +
                            $"{maQueue.Average(x => x.Close)}," +
                            $"{RelativeIndexHourly}" +
                            $"\n";
                            WriteFile(fileName, data);
                        }
                    }
                    //previousHistoricRate = rate;
                    HistoricChartHourlyCurrentPeriodCount++;
                }
                if (endingDateTime > now)
                    startingDateTime = now;
                else
                    startingDateTime = endingDateTime.AddHours(1);
            }
        }
        private void ProcessHistoryQuarterlyChartDownload()
        {
            string filePath = $"coinbase_btc_eur_historic_data_quarterly.csv";
            string data = "DateTime,High,Open,Close,Low,Volume,MA7,RSI14\n";
            //Validate file
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
                WriteFile(filePath, data);
            }
            //Initialise fields
            const int period = 14;
            const int maPeriod = 7;
            int granularity = 900;
            DateTime startingDateTime;
            DateTime endingDateTime;
            decimal change = 0;
            decimal increases = 0;
            decimal decreases = 0;
            Queue<IHistoricRate> maQueue = new Queue<IHistoricRate>();
            //Check if we have an empty file or not
            if (string.IsNullOrWhiteSpace(HistoricChartQuarterlyLastDateTime))
            {
                startingDateTime = new DateTime(2015, 4, 23).Date.ToUniversalTime();
                HistoricChartQuarterlyAverageGain = 0;
                HistoricChartQuarterlyAverageLoss = 0;
                HistoricChartQuarterlyCurrentPeriodCount = 0;
            }
            else
            {
                startingDateTime = DateTime.Parse(HistoricChartQuarterlyLastDateTime);
                //previousHistoricRate = new HistoricRate()
                //{
                //    Close = settingsHistoricChartPreviousHistoricRateClose,
                //    Open = settingsHistoricChartPreviousHistoricRateOpen,
                //    Low = settingsHistoricChartPreviousHistoricRateLow,
                //    High = settingsHistoricChartPreviousHistoricRateHigh,
                //    Volume = settingsHistoricChartPreviousHistoricRateVolume
                //};
            }
            //Begin data parsing
            DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0).ToUniversalTime();
            while (startingDateTime < now)
            {
                endingDateTime = startingDateTime.AddDays(3).ToUniversalTime();
                //Get the latest historic data
                List<IHistoricRate> result = _exchange.GetHistoryRates(null, startingDateTime, endingDateTime, granularity).Result;
                if (result == null || !result.Any()) break;
                result.Reverse();
                //Iterate though the historic data
                foreach (IHistoricRate rate in result)
                {
                    if (rate.DateAndTime >= now)
                        break;
                    //Moving Average 7 days
                    if (maQueue.Count == maPeriod)
                        maQueue.Dequeue();
                    maQueue.Enqueue(rate);
                    //Calculate RSI 14 days
                    if (HistoricChartQuarterlyCurrentPeriodCount > 0)
                    {
                        //change = rate.Close - previousHistoricRate.Close;
                        if (change > 0)
                        {
                            increases += change;
                            if (HistoricChartQuarterlyCurrentPeriodCount > period)
                            {
                                HistoricChartQuarterlyAverageGain = ((HistoricChartQuarterlyAverageGain * (period - 1)) + change) / period;
                                HistoricChartQuarterlyAverageLoss = (HistoricChartQuarterlyAverageLoss * (period - 1)) / period;
                            }
                        }
                        else if (change < 0)
                        {
                            decreases += (change * -1);
                            if (HistoricChartQuarterlyCurrentPeriodCount > period)
                            {
                                HistoricChartQuarterlyAverageGain = (HistoricChartQuarterlyAverageGain * (period - 1)) / period; 
                                HistoricChartQuarterlyAverageLoss = ((HistoricChartQuarterlyAverageLoss * (period - 1)) + (change * -1)) / period;
                            }
                        }
                        if (HistoricChartQuarterlyCurrentPeriodCount >= period)
                        {
                            if (HistoricChartQuarterlyCurrentPeriodCount == period)
                            {
                                HistoricChartQuarterlyAverageGain = increases / period;
                                HistoricChartQuarterlyAverageLoss = decreases / period;
                            }
                            if (HistoricChartQuarterlyCurrentPeriodCount >= period)
                            {
                                RelativeIndexQuarterly = HistoricChartQuarterlyAverageLoss == 0 ? 100 : Math.Round(100 - (100 / (1 + (HistoricChartQuarterlyAverageGain / HistoricChartQuarterlyAverageLoss))), 2);
                            }
                            //Generate data
                            data =
                            $"{rate.DateAndTime}," +
                            $"{rate.High}," +
                            $"{rate.Open}," +
                            $"{rate.Close}," +
                            $"{rate.Low}," +
                            $"{rate.Volume}," +
                            $"{maQueue.Average(x => x.Close)}," +
                            $"{RelativeIndexQuarterly}" +
                            $"\n";
                            WriteFile(filePath, data);
                        }
                    }
                    //previousHistoricRate = rate;
                    //HistoricChartQuarterlyCurrentPeriodCount++;
                }
                if (endingDateTime > now)
                    startingDateTime = now;
                else
                    startingDateTime = endingDateTime.AddHours(1);
            }
        }
        #endregion
        
        public void SaveChanges()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(FileName, json);
        }

        public void WriteFile(string filePath, string data)
        {
            File.AppendAllText(filePath, data);
        }

        public void Load()
        {
            string json = File.ReadAllText(FileName);
            RelativeStrengthIndex relativeStrengthIndex = JsonConvert.DeserializeObject<RelativeStrengthIndex>(json);
            FileName = relativeStrengthIndex.FileName;
        }
    }
}
