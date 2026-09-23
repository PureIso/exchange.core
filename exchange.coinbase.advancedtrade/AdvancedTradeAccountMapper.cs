using System;
using System.Globalization;
using System.Text.Json;
using exchange.coinbase.advancedtrade.models;
using exchange.core.models;

namespace exchange.coinbase.advancedtrade
{
    internal static class AdvancedTradeAccountMapper
    {
        public static Account ToCoreAccount(AdvancedTradeAccount source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(source.Uuid))
                throw new JsonException("A Coinbase account response did not contain uuid.");
            if (string.IsNullOrWhiteSpace(source.Currency))
                throw new JsonException("A Coinbase account response did not contain currency.");

            string availableValue = RequireBalance(source.AvailableBalance, "available_balance", source.Currency);
            string holdValue = RequireBalance(source.Hold, "hold", source.Currency);
            decimal available = ParseBalance(availableValue, "available_balance");
            decimal hold = ParseBalance(holdValue, "hold");

            return new Account
            {
                ID = source.Uuid,
                Currency = source.Currency,
                Available = availableValue,
                Hold = holdValue,
                Balance = (available + hold).ToString(CultureInfo.InvariantCulture),
                TradingEnabled = source.Active && source.Ready
            };
        }

        private static string RequireBalance(
            AdvancedTradeBalance balance,
            string fieldName,
            string accountCurrency)
        {
            if (balance == null || string.IsNullOrWhiteSpace(balance.Value))
                throw new JsonException($"A Coinbase account response did not contain {fieldName}.value.");
            if (!string.IsNullOrWhiteSpace(balance.Currency) &&
                !string.Equals(balance.Currency, accountCurrency, StringComparison.Ordinal))
                throw new JsonException($"Coinbase returned a {fieldName} currency mismatch.");
            return balance.Value;
        }

        private static decimal ParseBalance(string value, string fieldName)
        {
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal result))
                return result;
            throw new JsonException($"Coinbase returned an invalid {fieldName}.value.");
        }
    }
}
