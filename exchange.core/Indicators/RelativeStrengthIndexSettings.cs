using System;
using System.Text.Json.Serialization;
using exchange.core.models;

namespace exchange.core.Indicators
{
    [Serializable]
    public class RelativeStrengthIndexSettings
    {
        [JsonPropertyName("relative_index_daily")]
        public decimal RelativeIndexDaily { get; set; }

        [JsonPropertyName("relative_index_hourly")]
        public decimal RelativeIndexHourly { get; set; }

        [JsonPropertyName("relative_index_quarterly")]
        public decimal RelativeIndexQuarterly { get; set; }

        [JsonPropertyName("product")] public Product Product { get; set; }


        [JsonPropertyName("historic_chart_previous_date_time")]
        public string HistoricChartPreviousHistoricDateTime { get; set; }

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


        [JsonPropertyName("historic_chart_previous_date_time_hourly")]
        public string HistoricChartPreviousHistoricDateTimeHourly { get; set; }

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


        [JsonPropertyName("historic_chart_previous_date_time_quarterly")]
        public string HistoricChartPreviousHistoricDateTimeQuarterly { get; set; }

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
    }
}