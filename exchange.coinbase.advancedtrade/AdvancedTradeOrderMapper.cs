using System;
using System.Globalization;
using System.Text.Json;
using exchange.coinbase.advancedtrade.models;
using exchange.core.models;

namespace exchange.coinbase.advancedtrade
{
    internal static class AdvancedTradeOrderMapper
    {
        public static Order ToCoreOrder(AdvancedTradeOrder source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            RequireValue(source.OrderId, "order_id");
            RequireValue(source.ProductId, "product_id");

            JsonElement configuration = GetActiveConfiguration(source.OrderConfiguration);
            string baseSize = GetString(configuration, "base_size");
            string quoteSize = GetString(configuration, "quote_size");
            string size = source.SizeInQuote
                ? FirstValue(quoteSize, baseSize, source.FilledSize)
                : FirstValue(baseSize, quoteSize, source.FilledSize);
            string price = FirstValue(
                GetString(configuration, "limit_price"),
                source.AverageFilledPrice);

            decimal.TryParse(size, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal quantity);
            return new Order
            {
                ID = source.OrderId,
                ProductID = source.ProductId,
                Side = Normalize(source.Side),
                Type = Normalize(source.OrderType),
                Size = size,
                Fund = quoteSize,
                Price = price,
                PostOnly = GetBoolean(configuration, "post_only"),
                TimeInForce = Normalize(source.TimeInForce),
                CreatedAt = source.CreatedTime,
                FillFees = source.TotalFees,
                FilledSize = source.FilledSize,
                ExecutedValue = source.FilledValue,
                Status = Normalize(source.Status),
                Settled = source.Settled,
                Quantity = quantity
            };
        }

        private static JsonElement GetActiveConfiguration(JsonElement configuration)
        {
            if (configuration.ValueKind != JsonValueKind.Object)
                return default;
            foreach (JsonProperty property in configuration.EnumerateObject())
                if (property.Value.ValueKind == JsonValueKind.Object)
                    return property.Value;
            return default;
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propertyName, out JsonElement value))
                return null;
            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        private static bool GetBoolean(JsonElement element, string propertyName)
        {
            return element.ValueKind == JsonValueKind.Object &&
                   element.TryGetProperty(propertyName, out JsonElement value) &&
                   value.ValueKind == JsonValueKind.True;
        }

        private static string FirstValue(params string[] values)
        {
            foreach (string value in values)
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            return null;
        }

        private static string Normalize(string value)
        {
            return value?.ToLowerInvariant();
        }

        private static void RequireValue(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new JsonException($"A Coinbase order response did not contain {fieldName}.");
        }
    }
}
