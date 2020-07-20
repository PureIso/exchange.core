using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;
using exchange.core.Enums;
using exchange.core.interfaces;
using exchange.core.models;
using exchange.core.Models;

namespace exchange.core.Indicators
{
    [Serializable]
    public class RelativeStrengthIndex
    {

        #region Fields
        private readonly Timer _updater;
        private object _ioLock = new object();
        #endregion

        #region Properties
        [JsonPropertyName("relative_index_daily")]
        public decimal RelativeIndexDaily { get; set; }
        [JsonPropertyName("relative_index_hourly")]
        public decimal RelativeIndexHourly { get; set; }
        [JsonPropertyName("relative_index_quarterly")]
        public decimal RelativeIndexQuarterly { get; set; }
        [JsonPropertyName("product")]
        public Product Product { get; set; }
        [JsonPropertyName("historic_chart_last_datetime")]
        public string HistoricChartLastDateTime { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_close")]
        public decimal HistoricChartPreviousHistoricRateClose { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_open")]
        public decimal HistoricChartPreviousHistoricRateOpen { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_low")]
        public decimal HistoricChartPreviousHistoricRateLow { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_high")]
        public decimal HistoricChartPreviousHistoricRateHigh { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_volume")]
        public decimal HistoricChartPreviousHistoricRateVolume { get; set; }
        [JsonPropertyName("historic_chart_average_gain")]
        public decimal HistoricChartAverageGain { get; set; }
        [JsonPropertyName("historic_chart_average_loss")]
        public decimal HistoricChartAverageLoss { get; set; }
        [JsonPropertyName("historic_chart_current_period_count")]
        public int HistoricChartCurrentPeriodCount { get; set; }
        [JsonPropertyName("historic_chart_last_datetime_hourly")]
        public string HistoricChartLastDateTimeHourly { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_close_hourly")]
        public decimal HistoricChartPreviousHistoricRateCloseHourly { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_open_hourly")]
        public decimal HistoricChartPreviousHistoricRateOpenHourly { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_low_hourly")]
        public decimal HistoricChartPreviousHistoricRateLowHourly { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_high_hourly")]
        public decimal HistoricChartPreviousHistoricRateHighHourly { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_volume_hourly")]
        public decimal HistoricChartPreviousHistoricRateVolumeHourly { get; set; }
        [JsonPropertyName("historic_chart_average_gain_hourly")]
        public decimal HistoricChartAverageGainHourly { get; set; }
        [JsonPropertyName("historic_chart_average_loss_hourly")]
        public decimal HistoricChartAverageLossHourly { get; set; }
        [JsonPropertyName("historic_chart_current_period_count_hourly")]
        public int HistoricChartCurrentPeriodCountHourly { get; set; }
        [JsonPropertyName("historic_chart_last_datetime_quarterly")]
        public string HistoricChartLastDateTimeQuarterly { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_close_quarterly")]
        public decimal HistoricChartPreviousHistoricRateCloseQuarterly { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_open_quarterly")]
        public decimal HistoricChartPreviousHistoricRateOpenQuarterly { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_low_quarterly")]
        public decimal HistoricChartPreviousHistoricRateLowQuarterly { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_high_quarterly")]
        public decimal HistoricChartPreviousHistoricRateHighQuarterly { get; set; }
        [JsonPropertyName("historic_chart_previous_rate_volume_quarterly")]
        public decimal HistoricChartPreviousHistoricRateVolumeQuarterly { get; set; }
        [JsonPropertyName("historic_chart_average_gain_quarterly")]
        public decimal HistoricChartAverageGainQuarterly { get; set; }
        [JsonPropertyName("historic_chart_average_loss_quarterly")]
        public decimal HistoricChartAverageLossQuarterly { get; set; }
        [JsonPropertyName("historic_chart_current_period_count_quarterly")]
        public int HistoricChartCurrentPeriodCountQuarterly { get; set; }
        [JsonIgnore]
        public string DatabaseFile { get; set; }
        [JsonIgnore]
        public string DatabaseDirectory { get; set; }
        #endregion

        public Action<MessageType, string> ProcessLogBroadcast { get; set; }
        public Func<HistoricCandlesSearch, Task<List<HistoricRate>>> UpdateProductHistoricCandles { get; set; }
        public RelativeStrengthIndex()
        {
            _updater = new Timer();
        }

        #region Public Methods
        public void EnableRelativeStrengthIndexUpdater()
        {
            _updater.Elapsed += RelativeStrengthIndexUpdateHandlerAsync;
            _updater.Interval = 600000;//10 minutes
            _updater.Enabled = true;
            _updater.Start();
            RelativeStrengthIndexUpdateHandlerAsync(null, null);
        }
        public void DisableRelativeStrengthIndexUpdater()
        {
            _updater.Enabled = false;
            _updater.Stop();
            _updater.Elapsed -= RelativeStrengthIndexUpdateHandlerAsync;
        }
        #endregion

        #region Private Methods
        private void Save()
        {
            lock (_ioLock)
            {
                if (string.IsNullOrEmpty(DatabaseFile))
                    return;
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(DatabaseFile, json);
            }
        }
        public static RelativeStrengthIndex Load(string fileName)
        {
            try
            {
                string json = File.ReadAllText(fileName);
                RelativeStrengthIndex indicator = JsonSerializer.Deserialize<RelativeStrengthIndex>(json);
                if (indicator == null)
                    return new RelativeStrengthIndex();
                indicator.DatabaseFile = fileName;
                return indicator;
            }
            catch
            {
                return new RelativeStrengthIndex();
            }
        }
        private void RelativeStrengthIndexUpdateHandlerAsync(object source, ElapsedEventArgs e)
        {
            Task.WhenAll(
                Task.Run(ProcessHistoryChartDownload),
                Task.Run(ProcessHistoryHourlyChartDownload),
                Task.Run(ProcessHistoryQuarterlyChartDownload));
        }
        private void SaveAnalyticData(string filePath, string data)
        {
            File.AppendAllText(filePath, data);
        }
        
        private async void ProcessHistoryChartDownload()
        {
            string fileName = DatabaseDirectory + "\\coinbase_btc_eur_historic_data.csv";
            string data = "DateTime,High,Open,Close,Low,Volume,MA7,RSI14\n";
            //Validate file
            if (!File.Exists(fileName))
            {
                File.Create(fileName).Close();
                SaveAnalyticData(fileName, data);
            }
            //Initialise fields
            const int period = 14;
            const int maPeriod = 7;
            int granularity = 86400;
            DateTime startingDateTime;
            HistoricRate previousHistoricRate = new HistoricRate()
            {
                Close = 0,
                Open = 0,
                Low = 0,
                High = 0,
                Volume = 0
            };
            RelativeIndexDaily = -1;
            decimal increases = 0;
            decimal decreases = 0;
            Queue<HistoricRate> maQueue = new Queue<HistoricRate>();
            //Check if we have an empty file or not
            if (string.IsNullOrWhiteSpace(HistoricChartLastDateTime))
            {
                startingDateTime = new DateTime(2015, 4, 23).Date.ToUniversalTime();
                HistoricChartAverageGain = 0;
                HistoricChartAverageLoss = 0;
                HistoricChartCurrentPeriodCount = 0;
            }
            else
            {
                startingDateTime = DateTime.Parse(HistoricChartLastDateTime);
                previousHistoricRate = new HistoricRate
                {
                    Close = HistoricChartPreviousHistoricRateClose,
                    Open = HistoricChartPreviousHistoricRateOpen,
                    Low = HistoricChartPreviousHistoricRateLow,
                    High = HistoricChartPreviousHistoricRateHigh,
                    Volume = HistoricChartPreviousHistoricRateVolume
                };
            }

            //Begin data parsing
            DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            while (startingDateTime.Date < now)
            {
                DateTime endingDateTime = startingDateTime.AddMonths(6).ToUniversalTime();
                HistoricCandlesSearch historicCandlesSearch = new HistoricCandlesSearch();
                historicCandlesSearch.Symbol = Product.ID;
                historicCandlesSearch.StartingDateTime = startingDateTime;
                historicCandlesSearch.EndingDateTime = endingDateTime;
                historicCandlesSearch.Granularity = (Granularity)granularity;
                //Get the latest historic data
                List<HistoricRate> result = await UpdateProductHistoricCandles(historicCandlesSearch);
                if (!result.Any() && startingDateTime == new DateTime(2015, 4, 23).Date.ToUniversalTime())
                {
                    HistoricChartLastDateTime = DateTime.Now.AddHours(-2).Date.ToUniversalTime().ToString(CultureInfo.InvariantCulture);
                    Save();
                }
                if (!result.Any()) 
                    break;
                result.Reverse();
                //Iterate though the historic data
                foreach (HistoricRate rate in result)
                {
                    if (rate.DateAndTime.Date >= now)
                        break;
                    //Moving Average 7 days
                    if (maQueue.Count == maPeriod)
                        maQueue.Dequeue();
                    maQueue.Enqueue(rate);
                    //Calculate RSI 14 days
                    if (HistoricChartCurrentPeriodCount > 0)
                    {
                        decimal change = rate.Close - previousHistoricRate.Close;
                        if (change > 0)
                        {
                            increases += change;
                            if (HistoricChartCurrentPeriodCount > period)
                            {
                                HistoricChartAverageGain = ((HistoricChartAverageGain * (period - 1)) + change) / period;
                                HistoricChartAverageLoss = (HistoricChartAverageLoss * (period - 1)) / period;
                            }
                        }
                        else if (change < 0)
                        {
                            decreases += (change * -1);
                            if (HistoricChartCurrentPeriodCount > period)
                            {
                                HistoricChartAverageGain = (HistoricChartAverageGain * (period - 1)) / period;
                                HistoricChartAverageLoss = ((HistoricChartAverageLoss * (period - 1)) + (change * -1)) / period;
                            }
                        }
                        if (HistoricChartCurrentPeriodCount >= period)
                        {
                            if (HistoricChartCurrentPeriodCount == period)
                            {
                                HistoricChartAverageGain = increases / period;
                                HistoricChartAverageLoss = decreases / period;
                            }
                            if (HistoricChartCurrentPeriodCount >= period)
                            {
                                RelativeIndexDaily = HistoricChartAverageLoss == 0 ? 100 : Math.Round(100 - (100 / (1 + (HistoricChartAverageGain / HistoricChartAverageLoss))), 2);
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
                            $"{RelativeIndexDaily}" +
                            $"\n";
                            SaveAnalyticData(fileName, data);
                        }
                    }
                    previousHistoricRate = rate;
                    HistoricChartCurrentPeriodCount++;
                }
                startingDateTime = endingDateTime.Date > now ? now : endingDateTime.AddDays(1);
            }
            HistoricChartPreviousHistoricRateClose = previousHistoricRate.Close;
            HistoricChartPreviousHistoricRateOpen = previousHistoricRate.Open;
            HistoricChartPreviousHistoricRateLow = previousHistoricRate.Low;
            HistoricChartPreviousHistoricRateHigh = previousHistoricRate.High;
            HistoricChartPreviousHistoricRateVolume = previousHistoricRate.Volume;
            Save();
        }
        private async void ProcessHistoryHourlyChartDownload()
        {
            string fileName = DatabaseDirectory + "\\coinbase_btc_eur_historic_data_hourly.csv";
            string data = "DateTime,High,Open,Close,Low,Volume,MA7,RSI14\n";
            //Validate file
            if (!File.Exists(fileName))
            {
                File.Create(fileName).Close();
                SaveAnalyticData(fileName, data);
            }
            //Initialise fields
            const int period = 14;
            const int maPeriod = 7;
            int granularity = 3600;
            DateTime startingDateTime;
            HistoricRate previousHistoricRate = new HistoricRate()
            {
                Close = 0,
                Open = 0,
                Low = 0,
                High = 0,
                Volume = 0
            };
            //RelativeIndexHourly = -1;
            decimal increases = 0;
            decimal decreases = 0;
            Queue<HistoricRate> maQueue = new Queue<HistoricRate>();
            //Check if we have an empty file or not
            if (string.IsNullOrWhiteSpace(HistoricChartLastDateTimeHourly))
            {
                startingDateTime = new DateTime(2015, 4, 23).Date.ToUniversalTime();
                HistoricChartAverageGainHourly = 0;
                HistoricChartAverageLossHourly = 0;
                HistoricChartCurrentPeriodCountHourly = 0;
            }
            else
            {
                startingDateTime = DateTime.Parse(HistoricChartLastDateTimeHourly);
                previousHistoricRate = new HistoricRate()
                {
                    Close = HistoricChartPreviousHistoricRateCloseHourly,
                    Open = HistoricChartPreviousHistoricRateOpenHourly,
                    Low = HistoricChartPreviousHistoricRateLowHourly,
                    High = HistoricChartPreviousHistoricRateHighHourly,
                    Volume = HistoricChartPreviousHistoricRateVolumeHourly
                };
            }
            //Begin data parsing
            DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0).ToUniversalTime();
            while (startingDateTime < now)
            {
                DateTime endingDateTime = startingDateTime.AddDays(6).ToUniversalTime();
                //Get the latest historic data
                HistoricCandlesSearch historicCandlesSearch = new HistoricCandlesSearch();
                historicCandlesSearch.Symbol = Product.ID;
                historicCandlesSearch.StartingDateTime = startingDateTime;
                historicCandlesSearch.EndingDateTime = endingDateTime;
                historicCandlesSearch.Granularity = (Granularity)granularity;
                //Get the latest historic data
                List<HistoricRate> result = await UpdateProductHistoricCandles(historicCandlesSearch);
                if (!result.Any() && startingDateTime == new DateTime(2015, 4, 23).Date.ToUniversalTime())
                {
                    HistoricChartLastDateTimeHourly = DateTime.Now.AddHours(-2).Date.ToUniversalTime().ToString(CultureInfo.InvariantCulture);
                    Save();
                }
                if (!result.Any())
                    break;
                result.Reverse();
                //Iterate though the historic data
                foreach (HistoricRate rate in result)
                {
                    if (rate.DateAndTime >= now)
                        break;
                    //Moving Average 7 days
                    if (maQueue.Count == maPeriod)
                        maQueue.Dequeue();
                    maQueue.Enqueue(rate);
                    //Calculate RSI 14 days
                    if (HistoricChartCurrentPeriodCountHourly > 0)
                    {
                        decimal change = rate.Close - previousHistoricRate.Close;
                        if (change > 0)
                        {
                            increases += change;
                            if (HistoricChartCurrentPeriodCountHourly > period)
                            {
                                HistoricChartAverageGainHourly = ((HistoricChartAverageGainHourly * (period - 1)) + change) / period;
                                HistoricChartAverageLossHourly = (HistoricChartAverageLossHourly * (period - 1)) / period;
                            }
                        }
                        else if (change < 0)
                        {
                            decreases += (change * -1);
                            if (HistoricChartCurrentPeriodCountHourly > period)
                            {
                                HistoricChartAverageGainHourly = (HistoricChartAverageGainHourly * (period - 1)) / period;
                                HistoricChartAverageLossHourly = ((HistoricChartAverageLossHourly * (period - 1)) + (change * -1)) / period;
                            }
                        }
                        if (HistoricChartCurrentPeriodCountHourly >= period)
                        {
                            if (HistoricChartCurrentPeriodCountHourly == period)
                            {
                                HistoricChartAverageGainHourly = increases / period;
                                HistoricChartAverageLossHourly = decreases / period;
                            }
                            if (HistoricChartCurrentPeriodCountHourly >= period)
                            {
                                RelativeIndexHourly = HistoricChartAverageLossHourly == 0 ? 100 : Math.Round(100 - (100 / (1 + (HistoricChartAverageGainHourly / HistoricChartAverageLossHourly))), 2);
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
                            SaveAnalyticData(fileName, data);
                        }
                    }
                    previousHistoricRate = rate;
                    HistoricChartCurrentPeriodCountHourly++;
                }
                startingDateTime = endingDateTime > now ? now : endingDateTime.AddHours(1);
            }
            HistoricChartPreviousHistoricRateCloseHourly = previousHistoricRate.Close;
            HistoricChartPreviousHistoricRateOpenHourly = previousHistoricRate.Open;
            HistoricChartPreviousHistoricRateLowHourly = previousHistoricRate.Low;
            HistoricChartPreviousHistoricRateHighHourly = previousHistoricRate.High;
            HistoricChartPreviousHistoricRateVolumeHourly = previousHistoricRate.Volume;
            Save();
        }
        private async void ProcessHistoryQuarterlyChartDownload()
        {
            string fileName = DatabaseDirectory + "\\coinbase_btc_eur_historic_data_quarterly.csv";
            string data = "DateTime,High,Open,Close,Low,Volume,MA7,RSI14\n";
            //Validate file
            if (!File.Exists(fileName))
            {
                File.Create(fileName).Close();
                SaveAnalyticData(fileName, data);
            }
            //Initialise fields
            const int period = 14;
            const int maPeriod = 7;
            const int granularity = 900;
            DateTime startingDateTime;
            HistoricRate previousHistoricRate = new HistoricRate()
            {
                Close = 0,
                Open = 0,
                Low = 0,
                High = 0,
                Volume = 0
            };
            decimal increases = 0;
            decimal decreases = 0;
            Queue<HistoricRate> maQueue = new Queue<HistoricRate>();
            //Check if we have an empty file or not
            if (string.IsNullOrWhiteSpace(HistoricChartLastDateTime))
            {
                startingDateTime = new DateTime(2015, 4, 23).Date.ToUniversalTime();
                HistoricChartAverageGainQuarterly = 0;
                HistoricChartAverageLossQuarterly = 0;
                HistoricChartCurrentPeriodCountQuarterly = 0;
            }
            else
            {
                startingDateTime = DateTime.Parse(HistoricChartLastDateTimeQuarterly);
                previousHistoricRate = new HistoricRate()
                {
                    Close = HistoricChartPreviousHistoricRateCloseQuarterly,
                    Open = HistoricChartPreviousHistoricRateOpenQuarterly,
                    Low = HistoricChartPreviousHistoricRateLowQuarterly,
                    High = HistoricChartPreviousHistoricRateHighQuarterly,
                    Volume = HistoricChartPreviousHistoricRateVolumeQuarterly
                };
            }
            //Begin data parsing
            DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0).ToUniversalTime();
            while (startingDateTime < now)
            {
                DateTime endingDateTime = startingDateTime.AddDays(3).ToUniversalTime();
                //Get the latest historic data
                HistoricCandlesSearch historicCandlesSearch = new HistoricCandlesSearch();
                historicCandlesSearch.Symbol = Product.ID;
                historicCandlesSearch.StartingDateTime = startingDateTime;
                historicCandlesSearch.EndingDateTime = endingDateTime;
                historicCandlesSearch.Granularity = (Granularity)granularity;
                //Get the latest historic data
                List<HistoricRate> result = await UpdateProductHistoricCandles(historicCandlesSearch);
                if (!result.Any() && startingDateTime == new DateTime(2015, 4, 23).Date.ToUniversalTime())
                {
                    HistoricChartLastDateTimeQuarterly = DateTime.Now.AddHours(-2).Date.ToUniversalTime().ToString(CultureInfo.InvariantCulture);
                    Save();
                }
                if (!result.Any()) break;
                result.Reverse();
                //Iterate though the historic data
                foreach (HistoricRate rate in result)
                {
                    if (rate.DateAndTime >= now)
                        break;
                    //Moving Average 7 days
                    if (maQueue.Count == maPeriod)
                        maQueue.Dequeue();
                    maQueue.Enqueue(rate);
                    //Calculate RSI 14 days
                    if (HistoricChartCurrentPeriodCountQuarterly > 0)
                    {
                        decimal change = rate.Close - previousHistoricRate.Close;
                        if (change > 0)
                        {
                            increases += change;
                            if (HistoricChartCurrentPeriodCountQuarterly > period)
                            {
                                HistoricChartAverageGainQuarterly = ((HistoricChartAverageGainQuarterly * (period - 1)) + change) / period;
                                HistoricChartAverageLossQuarterly = (HistoricChartAverageLossQuarterly * (period - 1)) / period;
                            }
                        }
                        else if (change < 0)
                        {
                            decreases += (change * -1);
                            if (HistoricChartCurrentPeriodCountQuarterly > period)
                            {
                                HistoricChartAverageGainQuarterly = (HistoricChartAverageGainQuarterly * (period - 1)) / period;
                                HistoricChartAverageLossQuarterly = ((HistoricChartAverageLossQuarterly * (period - 1)) + (change * -1)) / period;
                            }
                        }
                        if (HistoricChartCurrentPeriodCountQuarterly >= period)
                        {
                            if (HistoricChartCurrentPeriodCountQuarterly == period)
                            {
                                HistoricChartAverageGainQuarterly = increases / period;
                                HistoricChartAverageLossQuarterly = decreases / period;
                            }
                            if (HistoricChartCurrentPeriodCountQuarterly >= period)
                            {
                                RelativeIndexQuarterly = HistoricChartAverageLossQuarterly == 0 ? 100 : Math.Round(100 - (100 / (1 + (HistoricChartAverageGainQuarterly / HistoricChartAverageLossQuarterly))), 2);
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
                            SaveAnalyticData(fileName, data);
                        }
                    }
                    previousHistoricRate = rate;
                    HistoricChartCurrentPeriodCountQuarterly++;
                }
                startingDateTime = endingDateTime > now ? now : endingDateTime.AddHours(1);
            }
            HistoricChartPreviousHistoricRateCloseQuarterly = previousHistoricRate.Close;
            HistoricChartPreviousHistoricRateOpenQuarterly = previousHistoricRate.Open; 
            HistoricChartPreviousHistoricRateLowQuarterly = previousHistoricRate.Low; 
            HistoricChartPreviousHistoricRateHighQuarterly = previousHistoricRate.High;
            HistoricChartPreviousHistoricRateVolumeQuarterly = previousHistoricRate.Volume;
            Save();
        }
        #endregion
    }
}
 