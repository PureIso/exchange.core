using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using exchange.core.Enums;
using exchange.core.models;
using exchange.core.helpers;
using Timer = System.Timers.Timer;

namespace exchange.core.indicators
{
    public class RelativeStrengthIndex
    {
        public RelativeStrengthIndex(string fileName, Product product)
        {
            RelativeStrengthIndexSettings = Load(fileName, product);
            _updater = new Timer();
            _ioLock = new object();
            _processHistorySemaphoreSlim = new SemaphoreSlim(1, 1);
            FileName = fileName;
            RelativeStrengthIndexSettings.Product = product;
        }

        public Action<Dictionary<string, string>> TechnicalIndicatorInformationBroadcast { get; set; }
        public Action<MessageType, string> ProcessLogBroadcast { get; set; }
        public Func<HistoricCandlesSearch, Task<List<HistoricRate>>> UpdateProductHistoricCandles { get; set; }

        #region Fields

        private readonly Timer _updater;
        private readonly object _ioLock;
        private readonly SemaphoreSlim _processHistorySemaphoreSlim;

        #endregion

        #region Properties

        public string FileName { get; set; }
        public RelativeStrengthIndexSettings RelativeStrengthIndexSettings { get; set; }

        #endregion

        #region Public Methods

        public void EnableRelativeStrengthIndexUpdater()
        {
            _updater.Elapsed += RelativeStrengthIndexUpdateHandlerAsync;
            _updater.Interval = 600000; //10 minutes
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
                string filename = FileName + $"_{RelativeStrengthIndexSettings.Product.ID.ToLower()}_RSI.json";
                if (string.IsNullOrEmpty(FileName))
                    return;
                string json = JsonSerializer.Serialize(RelativeStrengthIndexSettings,
                    new JsonSerializerOptions {WriteIndented = true});
                File.WriteAllText(filename, json);
            }
        }

        private static RelativeStrengthIndexSettings Load(string fileName, Product product)
        {
            try
            {
                string json = File.ReadAllText(fileName + $"_{product.ID.ToLower()}_RSI.json");
                RelativeStrengthIndexSettings relativeStrengthIndexSettings =
                    JsonSerializer.Deserialize<RelativeStrengthIndexSettings>(json);
                return relativeStrengthIndexSettings ?? new RelativeStrengthIndexSettings();
            }
            catch
            {
                return new RelativeStrengthIndexSettings();
            }
        }

        private void RelativeStrengthIndexUpdateHandlerAsync(object source, ElapsedEventArgs e)
        {
            if (string.IsNullOrEmpty(FileName))
                return;
            Task.WhenAll(
                Task.Run(ProcessHistoryDailyChartDownload),
                Task.Run(ProcessHistoryHourlyChartDownload),
                Task.Run(ProcessHistoryQuarterlyChartDownload)
            );
        }

        private void SaveAnalyticData(string filePath, string data)
        {
            lock (_ioLock)
            {
                if (File.Exists(filePath))
                    File.AppendAllText(filePath, data);
            }
        }

        private async void ProcessHistoryDailyChartDownload()
        {
            await _processHistorySemaphoreSlim.WaitAsync();
            try
            {
                string fileName = FileName + $"_{RelativeStrengthIndexSettings.Product.ID.ToLower()}_1D.csv";
                string data = "DateTime,High,Open,Close,Low,Volume,MA7,RSI14\n";
                //Validate file
                if (!File.Exists(fileName))
                {
                    if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                    File.Create(fileName).Close();
                    SaveAnalyticData(fileName, data);
                }

                //Initialise fields
                const int period = 14;
                const int maPeriod = 7;
                int granularity = 86400;
                DateTime startingDateTime;
                HistoricRate previousHistoricRate = null;
                decimal increases = 0;
                decimal decreases = 0;
                Queue<HistoricRate> maQueue = new Queue<HistoricRate>();
                //Check if we have an empty file or not
                if (string.IsNullOrWhiteSpace(RelativeStrengthIndexSettings.HistoricChartPreviousHistoricDateTime))
                {
                    startingDateTime = new DateTime(2015, 4, 23).Date;
                    RelativeStrengthIndexSettings.HistoricChartAverageGain = 0;
                    RelativeStrengthIndexSettings.HistoricChartAverageLoss = 0;
                    RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCount = 0;
                    RelativeStrengthIndexSettings.RelativeIndexDaily = -1;
                }
                else
                {
                    startingDateTime = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricDateTime.ToDateTime()
                        .AddDays(1);
                    previousHistoricRate = new HistoricRate
                    {
                        Close = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateClose,
                        Open = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpen,
                        Low = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateLow,
                        High = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateHigh,
                        Volume = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateVolume,
                        DateAndTime = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricDateTime.ToDateTime()
                    };
                }

                //Begin data parsing
                DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                while (startingDateTime < now)
                {
                    DateTime endingDateTime = startingDateTime.AddMonths(3).ToUniversalTime();
                    //Get the latest historic data
                    HistoricCandlesSearch historicCandlesSearch = new HistoricCandlesSearch
                    {
                        Symbol = RelativeStrengthIndexSettings.Product.ID,
                        StartingDateTime = startingDateTime,
                        EndingDateTime = endingDateTime,
                        Granularity = (Granularity) granularity
                    };
                    //Prevent overloaded calls by delaying for 1 second before call
                    await Task.Delay(1000);
                    //Get the latest historic data
                    List<HistoricRate> result = await UpdateProductHistoricCandles(historicCandlesSearch);
                    if (!result.Any())
                    {
                        //in the case that no data is available but current search date is not current date
                        startingDateTime = new DateTime(startingDateTime.Year, startingDateTime.Month, 1, 0, 0, 0)
                            .AddMonths(1);
                        continue;
                    }

                    //Iterate though the historic data
                    foreach (HistoricRate rate in result)
                    {
                        if (rate.DateAndTime.Date >= now.Date)
                            break;
                        if (previousHistoricRate != null && previousHistoricRate.DateAndTime.ToString("dd/MM/yyyy") ==
                            rate.DateAndTime.ToString("dd/MM/yyyy"))
                            continue;
                        //Moving Average 7 days
                        if (maQueue.Count == maPeriod)
                            maQueue.Dequeue();
                        maQueue.Enqueue(rate);
                        //Calculate RSI 14 days
                        if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCount > 0)
                        {
                            decimal change = rate.Close - previousHistoricRate.Close;
                            if (change > 0)
                            {
                                increases += change;
                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCount > period)
                                {
                                    RelativeStrengthIndexSettings.HistoricChartAverageGain =
                                        (RelativeStrengthIndexSettings.HistoricChartAverageGain * (period - 1) +
                                         change) / period;
                                    RelativeStrengthIndexSettings.HistoricChartAverageLoss =
                                        RelativeStrengthIndexSettings.HistoricChartAverageLoss * (period - 1) / period;
                                }
                            }
                            else if (change < 0)
                            {
                                decreases += change * -1;
                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCount > period)
                                {
                                    RelativeStrengthIndexSettings.HistoricChartAverageGain =
                                        RelativeStrengthIndexSettings.HistoricChartAverageGain * (period - 1) / period;
                                    RelativeStrengthIndexSettings.HistoricChartAverageLoss =
                                        (RelativeStrengthIndexSettings.HistoricChartAverageLoss * (period - 1) +
                                         change * -1) / period;
                                }
                            }

                            if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCount >= period)
                            {
                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCount == period)
                                {
                                    RelativeStrengthIndexSettings.HistoricChartAverageGain = increases / period;
                                    RelativeStrengthIndexSettings.HistoricChartAverageLoss = decreases / period;
                                }

                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCount >= period)
                                    RelativeStrengthIndexSettings.RelativeIndexDaily =
                                        RelativeStrengthIndexSettings.HistoricChartAverageLoss == 0
                                            ? 100
                                            : Math.Round(
                                                100 - 100 /
                                                (1 + RelativeStrengthIndexSettings.HistoricChartAverageGain /
                                                    RelativeStrengthIndexSettings.HistoricChartAverageLoss), 2);
                                //Generate data
                                data =
                                    $"{rate.DateAndTime}," +
                                    $"{rate.High}," +
                                    $"{rate.Open}," +
                                    $"{rate.Close}," +
                                    $"{rate.Low}," +
                                    $"{rate.Volume}," +
                                    $"{maQueue.Average(x => x.Close)}," +
                                    $"{RelativeStrengthIndexSettings.RelativeIndexDaily}" +
                                    "\n";
                                SaveAnalyticData(fileName, data);
                            }
                        }

                        previousHistoricRate = rate;
                        RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCount++;
                    }

                    startingDateTime = previousHistoricRate.DateAndTime.AddDays(1);
                    if (previousHistoricRate != null)
                    {
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateClose =
                            previousHistoricRate.Close;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpen = previousHistoricRate.Open;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateLow = previousHistoricRate.Low;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateHigh = previousHistoricRate.High;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateVolume =
                            previousHistoricRate.Volume;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricDateTime =
                            previousHistoricRate.DateAndTime.ToString();
                        Dictionary<string, string> indicatorInformation = new Dictionary<string, string>
                        {
                            ["RSI-15MIN"] =
                                RelativeStrengthIndexSettings.RelativeIndexQuarterly.ToString(CultureInfo
                                    .InvariantCulture),
                            ["RSI-1HOUR"] =
                                RelativeStrengthIndexSettings.RelativeIndexHourly
                                    .ToString(CultureInfo.InvariantCulture),
                            ["RSI-1DAY"] =
                                RelativeStrengthIndexSettings.RelativeIndexDaily.ToString(CultureInfo.InvariantCulture),
                            ["OPEN-15MIN"] =
                                RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpenQuarterly.ToString(
                                    CultureInfo.InvariantCulture),
                            ["OPEN-1HOUR"] =
                                RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpenHourly.ToString(
                                    CultureInfo.InvariantCulture),
                            ["OPEN-1DAY"] =
                                RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpen.ToString(CultureInfo
                                    .InvariantCulture)
                        };
                        TechnicalIndicatorInformationBroadcast?.Invoke(indicatorInformation);
                        Save();
                    }
                }
            }
            catch (Exception ex)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: ProcessHistoryHourlyChartDownload\r\nException Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                _processHistorySemaphoreSlim.Release();
            }
        }

        private async void ProcessHistoryHourlyChartDownload()
        {
            await _processHistorySemaphoreSlim.WaitAsync();
            try
            {
                string fileName = FileName + $"_{RelativeStrengthIndexSettings.Product.ID.ToLower()}_1H.csv";
                string data = "DateTime,High,Open,Close,Low,Volume,MA7,RSI14\n";
                //Validate file
                if (!File.Exists(fileName))
                {
                    if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                    File.Create(fileName).Close();
                    SaveAnalyticData(fileName, data);
                }

                //Initialise fields
                const int period = 14;
                const int maPeriod = 7;
                int granularity = 3600;
                DateTime startingDateTime;
                HistoricRate previousHistoricRate = null;
                decimal increases = 0;
                decimal decreases = 0;
                Queue<HistoricRate> maQueue = new Queue<HistoricRate>();
                //Check if we have an empty file or not
                if (string.IsNullOrWhiteSpace(RelativeStrengthIndexSettings.HistoricChartPreviousHistoricDateTimeHourly)
                )
                {
                    startingDateTime = new DateTime(2015, 4, 23).Date;
                    RelativeStrengthIndexSettings.HistoricChartAverageGainHourly = 0;
                    RelativeStrengthIndexSettings.HistoricChartAverageLossHourly = 0;
                    RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountHourly = 0;
                    RelativeStrengthIndexSettings.RelativeIndexHourly = -1;
                }
                else
                {
                    startingDateTime = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricDateTimeHourly
                        .ToDateTime().AddHours(1);
                    previousHistoricRate = new HistoricRate
                    {
                        Close = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateCloseHourly,
                        Open = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpenHourly,
                        Low = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateLowHourly,
                        High = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateHighHourly,
                        Volume = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateVolumeHourly,
                        DateAndTime = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricDateTimeHourly
                            .ToDateTime()
                    };
                }

                //Begin data parsing
                DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour,
                    0, 0);
                //int remainingAttempts = 10;
                while (startingDateTime < now)
                {
                    DateTime endingDateTime = startingDateTime.AddDays(3);
                    //Get the latest historic data
                    HistoricCandlesSearch historicCandlesSearch = new HistoricCandlesSearch
                    {
                        Symbol = RelativeStrengthIndexSettings.Product.ID,
                        StartingDateTime = startingDateTime,
                        EndingDateTime = endingDateTime,
                        Granularity = (Granularity) granularity
                    };
                    //Prevent overloaded calls by delaying for 1 second before call
                    await Task.Delay(1000);
                    //Get the latest historic data
                    List<HistoricRate> result = await UpdateProductHistoricCandles(historicCandlesSearch);
                    if (!result.Any())
                    {
                        //in the case that no data is available but current search date is not current date
                        startingDateTime = new DateTime(startingDateTime.Year, startingDateTime.Month, 1, 0, 0, 0)
                            .AddMonths(1);
                        continue;
                    }

                    //Iterate though the historic data
                    foreach (HistoricRate rate in result)
                    {
                        if (rate.DateAndTime >= now)
                            break;
                        if (previousHistoricRate != null &&
                            previousHistoricRate.DateAndTime.ToString("dd/MM/yyyy HH") ==
                            rate.DateAndTime.ToString("dd/MM/yyyy HH"))
                            continue;
                        //Moving Average 7 days
                        if (maQueue.Count == maPeriod)
                            maQueue.Dequeue();
                        maQueue.Enqueue(rate);
                        //Calculate RSI 14 days
                        if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountHourly > 0)
                        {
                            decimal change = rate.Close - previousHistoricRate.Close;
                            if (change > 0)
                            {
                                increases += change;
                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountHourly > period)
                                {
                                    RelativeStrengthIndexSettings.HistoricChartAverageGainHourly =
                                        (RelativeStrengthIndexSettings.HistoricChartAverageGainHourly * (period - 1) +
                                         change) / period;
                                    RelativeStrengthIndexSettings.HistoricChartAverageLossHourly =
                                        RelativeStrengthIndexSettings.HistoricChartAverageLossHourly * (period - 1) /
                                        period;
                                }
                            }
                            else if (change < 0)
                            {
                                decreases += change * -1;
                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountHourly > period)
                                {
                                    RelativeStrengthIndexSettings.HistoricChartAverageGainHourly =
                                        RelativeStrengthIndexSettings.HistoricChartAverageGainHourly * (period - 1) /
                                        period;
                                    RelativeStrengthIndexSettings.HistoricChartAverageLossHourly =
                                        (RelativeStrengthIndexSettings.HistoricChartAverageLossHourly * (period - 1) +
                                         change * -1) / period;
                                }
                            }

                            if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountHourly >= period)
                            {
                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountHourly == period)
                                {
                                    RelativeStrengthIndexSettings.HistoricChartAverageGainHourly = increases / period;
                                    RelativeStrengthIndexSettings.HistoricChartAverageLossHourly = decreases / period;
                                }

                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountHourly >= period)
                                    RelativeStrengthIndexSettings.RelativeIndexHourly =
                                        RelativeStrengthIndexSettings.HistoricChartAverageLossHourly == 0
                                            ? 100
                                            : Math.Round(
                                                100 - 100 /
                                                (1 + RelativeStrengthIndexSettings.HistoricChartAverageGainHourly /
                                                    RelativeStrengthIndexSettings.HistoricChartAverageLossHourly), 2);
                                //Generate data
                                data =
                                    $"{rate.DateAndTime}," +
                                    $"{rate.High}," +
                                    $"{rate.Open}," +
                                    $"{rate.Close}," +
                                    $"{rate.Low}," +
                                    $"{rate.Volume}," +
                                    $"{maQueue.Average(x => x.Close)}," +
                                    $"{RelativeStrengthIndexSettings.RelativeIndexHourly}" +
                                    "\n";
                                SaveAnalyticData(fileName, data);
                            }
                        }

                        previousHistoricRate = rate;
                        RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountHourly++;
                    }

                    startingDateTime = previousHistoricRate.DateAndTime.AddHours(1);
                    if (previousHistoricRate != null)
                    {
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateCloseHourly =
                            previousHistoricRate.Close;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpenHourly =
                            previousHistoricRate.Open;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateLowHourly =
                            previousHistoricRate.Low;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateHighHourly =
                            previousHistoricRate.High;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateVolumeHourly =
                            previousHistoricRate.Volume;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricDateTimeHourly =
                            previousHistoricRate.DateAndTime.ToString();

                        Dictionary<string, string> indicatorInformation = new Dictionary<string, string>
                        {
                            ["RSI-15MIN"] =
                                RelativeStrengthIndexSettings.RelativeIndexQuarterly.ToString(CultureInfo
                                    .InvariantCulture),
                            ["RSI-1HOUR"] =
                                RelativeStrengthIndexSettings.RelativeIndexHourly
                                    .ToString(CultureInfo.InvariantCulture),
                            ["RSI-1DAY"] =
                                RelativeStrengthIndexSettings.RelativeIndexDaily.ToString(CultureInfo.InvariantCulture),
                            ["OPEN-15MIN"] =
                                RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpenQuarterly.ToString(
                                    CultureInfo.InvariantCulture),
                            ["OPEN-1HOUR"] =
                                RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpenHourly.ToString(
                                    CultureInfo.InvariantCulture),
                            ["OPEN-1DAY"] =
                                RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpen.ToString(CultureInfo
                                    .InvariantCulture)
                        };
                        TechnicalIndicatorInformationBroadcast?.Invoke(indicatorInformation);
                        Save();
                    }
                }
            }
            catch (Exception e)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: ProcessHistoryDailyChartDownload\r\nException Stack Trace: {e.StackTrace}");
            }
            finally
            {
                _processHistorySemaphoreSlim.Release();
            }
        }

        private async void ProcessHistoryQuarterlyChartDownload()
        {
            await _processHistorySemaphoreSlim.WaitAsync();
            try
            {
                string fileName = FileName + $"_{RelativeStrengthIndexSettings.Product.ID.ToLower()}_15M.csv";
                string data = "DateTime,High,Open,Close,Low,Volume,MA7,RSI14\n";
                //Validate file
                if (!File.Exists(fileName))
                {
                    if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                    File.Create(fileName).Close();
                    SaveAnalyticData(fileName, data);
                }

                //Initialise fields
                const int period = 14;
                const int maPeriod = 7;
                const int granularity = 900;
                DateTime startingDateTime;
                HistoricRate previousHistoricRate = null;
                decimal increases = 0;
                decimal decreases = 0;
                Queue<HistoricRate> maQueue = new Queue<HistoricRate>();
                //Check if we have an empty file or not
                if (string.IsNullOrWhiteSpace(RelativeStrengthIndexSettings
                    .HistoricChartPreviousHistoricDateTimeQuarterly))
                {
                    startingDateTime = new DateTime(2015, 4, 23).Date;
                    RelativeStrengthIndexSettings.HistoricChartAverageGainQuarterly = 0;
                    RelativeStrengthIndexSettings.HistoricChartAverageLossQuarterly = 0;
                    RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountQuarterly = 0;
                    RelativeStrengthIndexSettings.RelativeIndexQuarterly = -1;
                }
                else
                {
                    startingDateTime = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricDateTimeQuarterly
                        .ToDateTime().AddMinutes(15);
                    previousHistoricRate = new HistoricRate
                    {
                        Close = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateCloseQuarterly,
                        Open = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpenQuarterly,
                        Low = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateLowQuarterly,
                        High = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateHighQuarterly,
                        Volume = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateVolumeQuarterly,
                        DateAndTime = RelativeStrengthIndexSettings.HistoricChartPreviousHistoricDateTimeQuarterly
                            .ToDateTime()
                    };
                }

                //Begin data parsing
                DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour,
                    DateTime.Now.Minute, 0);
                while (startingDateTime < now)
                {
                    DateTime endingDateTime = startingDateTime.AddDays(2);
                    //Get the latest historic data
                    HistoricCandlesSearch historicCandlesSearch = new HistoricCandlesSearch
                    {
                        Symbol = RelativeStrengthIndexSettings.Product.ID,
                        StartingDateTime = startingDateTime,
                        EndingDateTime = endingDateTime,
                        Granularity = (Granularity) granularity
                    };
                    //Prevent overloaded calls by delaying for 1 second before call
                    await Task.Delay(1000);
                    //Get the latest historic data
                    List<HistoricRate> result = await UpdateProductHistoricCandles(historicCandlesSearch);
                    if (!result.Any())
                    {
                        //in the case that no data is available but current search date is not current date
                        startingDateTime = new DateTime(startingDateTime.Year, startingDateTime.Month, 1, 0, 0, 0)
                            .AddMonths(1);
                        continue;
                    }

                    //Iterate though the historic data
                    foreach (HistoricRate rate in result)
                    {
                        if (rate.DateAndTime >= now)
                            break;
                        if (previousHistoricRate != null &&
                            previousHistoricRate.DateAndTime.ToString("dd/MM/yyyy HH:mm") ==
                            rate.DateAndTime.ToString("dd/MM/yyyy HH:mm"))
                            continue;
                        //Moving Average 7 days
                        if (maQueue.Count == maPeriod)
                            maQueue.Dequeue();
                        maQueue.Enqueue(rate);
                        //Calculate RSI 14 days
                        if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountQuarterly > 0)
                        {
                            decimal change = rate.Close - previousHistoricRate.Close;
                            if (change > 0)
                            {
                                increases += change;
                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountQuarterly > period)
                                {
                                    RelativeStrengthIndexSettings.HistoricChartAverageGainQuarterly =
                                        (RelativeStrengthIndexSettings.HistoricChartAverageGainQuarterly *
                                            (period - 1) + change) / period;
                                    RelativeStrengthIndexSettings.HistoricChartAverageLossQuarterly =
                                        RelativeStrengthIndexSettings.HistoricChartAverageLossQuarterly * (period - 1) /
                                        period;
                                }
                            }
                            else if (change < 0)
                            {
                                decreases += change * -1;
                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountQuarterly > period)
                                {
                                    RelativeStrengthIndexSettings.HistoricChartAverageGainQuarterly =
                                        RelativeStrengthIndexSettings.HistoricChartAverageGainQuarterly * (period - 1) /
                                        period;
                                    RelativeStrengthIndexSettings.HistoricChartAverageLossQuarterly =
                                        (RelativeStrengthIndexSettings.HistoricChartAverageLossQuarterly *
                                            (period - 1) + change * -1) / period;
                                }
                            }

                            if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountQuarterly >= period)
                            {
                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountQuarterly == period)
                                {
                                    RelativeStrengthIndexSettings.HistoricChartAverageGainQuarterly =
                                        increases / period;
                                    RelativeStrengthIndexSettings.HistoricChartAverageLossQuarterly =
                                        decreases / period;
                                }

                                if (RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountQuarterly >= period)
                                    RelativeStrengthIndexSettings.RelativeIndexQuarterly =
                                        RelativeStrengthIndexSettings.HistoricChartAverageLossQuarterly == 0
                                            ? 100
                                            : Math.Round(
                                                100 - 100 /
                                                (1 + RelativeStrengthIndexSettings.HistoricChartAverageGainQuarterly /
                                                    RelativeStrengthIndexSettings.HistoricChartAverageLossQuarterly),
                                                2);
                                //Generate data
                                data =
                                    $"{rate.DateAndTime}," +
                                    $"{rate.High}," +
                                    $"{rate.Open}," +
                                    $"{rate.Close}," +
                                    $"{rate.Low}," +
                                    $"{rate.Volume}," +
                                    $"{maQueue.Average(x => x.Close)}," +
                                    $"{RelativeStrengthIndexSettings.RelativeIndexQuarterly}" +
                                    "\n";
                                SaveAnalyticData(fileName, data);
                            }
                        }

                        previousHistoricRate = rate;
                        RelativeStrengthIndexSettings.HistoricChartCurrentPeriodCountQuarterly++;
                    }

                    startingDateTime = previousHistoricRate.DateAndTime.AddMinutes(15);
                    if (previousHistoricRate != null)
                    {
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateCloseQuarterly =
                            previousHistoricRate.Close;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpenQuarterly =
                            previousHistoricRate.Open;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateLowQuarterly =
                            previousHistoricRate.Low;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateHighQuarterly =
                            previousHistoricRate.High;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateVolumeQuarterly =
                            previousHistoricRate.Volume;
                        RelativeStrengthIndexSettings.HistoricChartPreviousHistoricDateTimeQuarterly =
                            previousHistoricRate.DateAndTime.ToString();
                        Dictionary<string, string> indicatorInformation = new Dictionary<string, string>
                        {
                            ["RSI-15MIN"] =
                                RelativeStrengthIndexSettings.RelativeIndexQuarterly.ToString(CultureInfo
                                    .InvariantCulture),
                            ["RSI-1HOUR"] =
                                RelativeStrengthIndexSettings.RelativeIndexHourly
                                    .ToString(CultureInfo.InvariantCulture),
                            ["RSI-1DAY"] =
                                RelativeStrengthIndexSettings.RelativeIndexDaily.ToString(CultureInfo.InvariantCulture),
                            ["OPEN-15MIN"] =
                                RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpenQuarterly.ToString(
                                    CultureInfo.InvariantCulture),
                            ["OPEN-1HOUR"] =
                                RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpenHourly.ToString(
                                    CultureInfo.InvariantCulture),
                            ["OPEN-1DAY"] =
                                RelativeStrengthIndexSettings.HistoricChartPreviousHistoricRateOpen.ToString(CultureInfo
                                    .InvariantCulture)
                        };
                        TechnicalIndicatorInformationBroadcast?.Invoke(indicatorInformation);
                        Save();
                    }
                }
            }
            catch (Exception ex)
            {
                ProcessLogBroadcast?.Invoke(MessageType.Error,
                    $"Method: ProcessHistoryHourlyChartDownload\r\nException Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                _processHistorySemaphoreSlim.Release();
            }
        }

        #endregion
    }
}