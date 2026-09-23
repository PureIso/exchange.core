using System;
using System.Globalization;
using System.Text.Json;
using exchange.coinbase.advancedtrade.models;
using exchange.core.models;

namespace exchange.coinbase.advancedtrade
{
    internal static class AdvancedTradeProductMapper
    {
        public static Product ToCoreProduct(AdvancedTradeProduct source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(source.ProductId))
                throw new JsonException("A Coinbase product response did not contain product_id.");

            return new Product
            {
                ID = source.ProductId,
                BaseCurrency = source.BaseCurrencyId,
                QuoteCurrency = source.QuoteCurrencyId,
                BaseMinSize = source.BaseMinSize,
                BaseMaxSize = source.BaseMaxSize,
                QuoteIncrement = source.QuoteIncrement
            };
        }

        public static Statistics ToCoreStatistics(AdvancedTradeProduct source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            decimal currentPrice = ParseRequiredDecimal(source.Price, "price");
            decimal percentageChange = ParsePercentage(source.PricePercentageChange24Hour);
            decimal changeFactor = 1m + percentageChange / 100m;
            if (changeFactor == 0m)
                throw new JsonException(
                    "Coinbase returned a 24-hour price change from which an opening price cannot be derived.");

            return new Statistics
            {
                Open = (currentPrice / changeFactor).ToString(CultureInfo.InvariantCulture),
                High = RequireValue(source.High24Hour, "high_24h"),
                Low = RequireValue(source.Low24Hour, "low_24h"),
                Volume = RequireValue(source.Volume24Hour, "volume_24h"),
                Last = source.Price,
                Volume30Day = null
            };
        }

        private static decimal ParseRequiredDecimal(string value, string fieldName)
        {
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal result))
                return result;
            throw new JsonException($"Coinbase returned an invalid {fieldName} value.");
        }

        private static decimal ParsePercentage(string value)
        {
            string normalizedValue = value?.Trim();
            if (normalizedValue?.EndsWith('%') == true)
                normalizedValue = normalizedValue.Substring(0, normalizedValue.Length - 1);
            return ParseRequiredDecimal(normalizedValue, "price_percentage_change_24h");
        }

        private static string RequireValue(string value, string fieldName)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
            throw new JsonException($"Coinbase returned an empty {fieldName} value.");
        }
    }
}
