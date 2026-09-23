using System;
using System.Globalization;
using System.Text.Json;
using exchange.coinbase.advancedtrade.models;
using exchange.core.models;

namespace exchange.coinbase.advancedtrade
{
    internal static class AdvancedTradeFillMapper
    {
        public static Fill ToCoreFill(AdvancedTradeFill source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            RequireValue(source.TradeId, "trade_id");
            RequireValue(source.OrderId, "order_id");
            RequireValue(source.ProductId, "product_id");
            if (!DateTimeOffset.TryParse(
                    source.TradeTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset tradeTime))
                throw new JsonException("Coinbase returned a fill with an invalid trade_time.");

            int.TryParse(source.TradeId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int legacyTradeId);
            return new Fill
            {
                TradeID = legacyTradeId,
                ExternalTradeID = source.TradeId,
                ProductID = source.ProductId,
                Price = source.Price,
                Size = source.Size,
                OrderID = source.OrderId,
                Side = source.Side?.ToLowerInvariant(),
                Fee = source.Commission,
                Settled = true,
                Time = tradeTime.UtcDateTime
            };
        }

        private static void RequireValue(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new JsonException($"A Coinbase fill response did not contain {fieldName}.");
        }
    }
}
