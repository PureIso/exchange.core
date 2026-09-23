using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using exchange.core.models;

namespace exchange.core.parity
{
    public sealed class ExchangeReadOnlyParityComparator
    {
        private readonly ExchangeParityOptions _options;

        public ExchangeReadOnlyParityComparator(ExchangeParityOptions options = null)
        {
            _options = options ?? new ExchangeParityOptions();
            if (_options.PriceTolerance < 0 || _options.QuantityTolerance < 0 ||
                _options.LivePriceAbsoluteTolerance < 0 ||
                _options.LivePriceRelativeTolerance < 0 ||
                _options.PercentageTolerance < 0 ||
                _options.TimestampTolerance < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    "Parity tolerances cannot be negative.");
        }

        public ExchangeParityReport Compare(
            ExchangeReadOnlySnapshot legacy,
            ExchangeReadOnlySnapshot replacement)
        {
            if (legacy == null)
                throw new ArgumentNullException(nameof(legacy));
            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            List<ExchangeParityFinding> findings = new List<ExchangeParityFinding>();
            CompareProducts(legacy.Products, replacement.Products, findings);
            CompareBalances(legacy.AccountBalances, replacement.AccountBalances, findings);
            CompareOrders(legacy.Orders, replacement.Orders, findings);
            CompareFills(legacy.Fills, replacement.Fills, findings);
            CompareTickers(legacy.Tickers, replacement.Tickers, findings);
            CompareLiveAssets(legacy.LiveAssets, replacement.LiveAssets, findings);
            AddDocumentedExclusions(findings);
            return new ExchangeParityReport(findings);
        }

        private void CompareProducts(
            IEnumerable<Product> legacy,
            IEnumerable<Product> replacement,
            List<ExchangeParityFinding> findings)
        {
            CompareKeyed(
                "product",
                legacy,
                replacement,
                product => product?.ID,
                findings,
                (key, left, right) =>
                {
                    Text("product", key, "base_currency", left.BaseCurrency, right.BaseCurrency, findings);
                    Text("product", key, "quote_currency", left.QuoteCurrency, right.QuoteCurrency, findings);
                    Number("product", key, "base_min_size", left.BaseMinSize, right.BaseMinSize, _options.QuantityTolerance, findings);
                    Number("product", key, "base_max_size", left.BaseMaxSize, right.BaseMaxSize, _options.QuantityTolerance, findings);
                    Number("product", key, "quote_increment", left.QuoteIncrement, right.QuoteIncrement, _options.QuantityTolerance, findings);
                });
        }

        private void CompareBalances(
            IReadOnlyDictionary<string, decimal> legacy,
            IReadOnlyDictionary<string, decimal> replacement,
            List<ExchangeParityFinding> findings)
        {
            CompareDictionaries(
                "account_balance",
                legacy ?? new Dictionary<string, decimal>(),
                replacement ?? new Dictionary<string, decimal>(),
                findings,
                (key, left, right) => Number(
                    "account_balance",
                    key,
                    "balance",
                    left,
                    right,
                    _options.QuantityTolerance,
                    findings));
        }

        private void CompareOrders(
            IEnumerable<Order> legacy,
            IEnumerable<Order> replacement,
            List<ExchangeParityFinding> findings)
        {
            CompareKeyed(
                "order",
                legacy,
                replacement,
                order => order?.ID,
                findings,
                (key, left, right) =>
                {
                    Text("order", key, "product_id", left.ProductID, right.ProductID, findings);
                    Text("order", key, "side", left.Side, right.Side, findings, true);
                    Text("order", key, "type", left.Type, right.Type, findings, true);
                    Text("order", key, "status", Status(left), Status(right), findings, true);
                    Text("order", key, "time_in_force", left.TimeInForce, right.TimeInForce, findings, true);
                    Boolean("order", key, "post_only", left.PostOnly, right.PostOnly, findings);
                    Boolean("order", key, "settled", left.Settled, right.Settled, findings);
                    Number("order", key, "size", left.Size, right.Size, _options.QuantityTolerance, findings);
                    Number("order", key, "price", left.Price, right.Price, _options.PriceTolerance, findings);
                    Number("order", key, "filled_size", left.FilledSize, right.FilledSize, _options.QuantityTolerance, findings);
                    Number("order", key, "fill_fees", left.FillFees, right.FillFees, _options.QuantityTolerance, findings);
                    Number("order", key, "executed_value", left.ExecutedValue, right.ExecutedValue, _options.PriceTolerance, findings);
                    Timestamp("order", key, "created_at", left.CreatedAt, right.CreatedAt, findings);
                });
        }

        private void CompareFills(
            IEnumerable<Fill> legacy,
            IEnumerable<Fill> replacement,
            List<ExchangeParityFinding> findings)
        {
            List<Fill> remaining = (replacement ?? Enumerable.Empty<Fill>()).ToList();
            foreach (Fill left in legacy ?? Enumerable.Empty<Fill>())
            {
                if (left == null || string.IsNullOrWhiteSpace(left.OrderID) ||
                    string.IsNullOrWhiteSpace(left.ProductID))
                    throw new InvalidOperationException("A fill parity item has no stable business key.");
                Fill right = remaining
                    .Where(candidate => candidate != null &&
                        string.Equals(candidate.OrderID, left.OrderID, StringComparison.Ordinal) &&
                        string.Equals(candidate.ProductID, left.ProductID, StringComparison.Ordinal))
                    .OrderBy(candidate =>
                        (candidate.Time.ToUniversalTime() - left.Time.ToUniversalTime()).Duration())
                    .FirstOrDefault(candidate =>
                        (candidate.Time.ToUniversalTime() - left.Time.ToUniversalTime()).Duration() <=
                        _options.TimestampTolerance);
                string key = FillBusinessKey(left);
                if (right == null)
                {
                    Missing("fill", key, ExchangeParityDisposition.MissingReplacement, findings);
                    continue;
                }
                remaining.Remove(right);
                Text("fill", key, "product_id", left.ProductID, right.ProductID, findings);
                Text("fill", key, "order_id", left.OrderID, right.OrderID, findings);
                Text("fill", key, "side", left.Side, right.Side, findings, true);
                Boolean("fill", key, "settled", left.Settled, right.Settled, findings);
                Number("fill", key, "price", left.Price, right.Price, _options.PriceTolerance, findings);
                Number("fill", key, "size", left.Size, right.Size, _options.QuantityTolerance, findings);
                Number("fill", key, "fee", left.Fee, right.Fee, _options.QuantityTolerance, findings);
                Date("fill", key, "created_at", left.Time, right.Time, findings);
            }
            foreach (Fill right in remaining)
            {
                if (right == null || string.IsNullOrWhiteSpace(right.OrderID) ||
                    string.IsNullOrWhiteSpace(right.ProductID))
                    throw new InvalidOperationException("A fill parity item has no stable business key.");
                Missing(
                    "fill",
                    FillBusinessKey(right),
                    ExchangeParityDisposition.MissingLegacy,
                    findings);
            }
        }

        private void CompareTickers(
            IEnumerable<Ticker> legacy,
            IEnumerable<Ticker> replacement,
            List<ExchangeParityFinding> findings)
        {
            CompareKeyed(
                "ticker",
                legacy,
                replacement,
                ticker => ticker?.ProductID,
                findings,
                (key, left, right) =>
                {
                    MarketNumber("ticker", key, "price", left.Price, right.Price, findings);
                    OptionalMarketNumber("ticker", key, "bid", left.Bid, right.Bid, findings);
                    OptionalMarketNumber("ticker", key, "ask", left.Ask, right.Ask, findings);
                    OptionalNumber("ticker", key, "size", left.Size, right.Size, _options.QuantityTolerance, findings);
                });
        }

        private void CompareLiveAssets(
            IEnumerable<AssetInformation> legacy,
            IEnumerable<AssetInformation> replacement,
            List<ExchangeParityFinding> findings)
        {
            CompareKeyed(
                "live_asset",
                legacy,
                replacement,
                asset => asset?.ProductID,
                findings,
                (key, left, right) =>
                {
                    MarketNumber("live_asset", key, "current_price", left.CurrentPrice, right.CurrentPrice, findings);
                    OptionalMarketNumber("live_asset", key, "best_bid", left.BestBid, right.BestBid, findings);
                    OptionalMarketNumber("live_asset", key, "best_ask", left.BestAsk, right.BestAsk, findings);
                    MarketNumber("live_asset", key, "twenty_four_hour_price_change", left.TwentyFourHourPriceChange, right.TwentyFourHourPriceChange, findings);
                    Number("live_asset", key, "twenty_four_hour_price_percentage_change", left.TwentyFourHourPricePercentageChange, right.TwentyFourHourPricePercentageChange, _options.PercentageTolerance, findings);
                });
        }

        private void MarketNumber(
            string scope,
            string key,
            string field,
            string legacy,
            string replacement,
            List<ExchangeParityFinding> findings)
        {
            if (!decimal.TryParse(legacy, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal left) ||
                !decimal.TryParse(replacement, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal right))
            {
                Mismatch(scope, key, field, legacy, replacement, findings, "Both values must be invariant decimals.");
                return;
            }
            MarketNumber(scope, key, field, left, right, findings);
        }

        private void MarketNumber(
            string scope,
            string key,
            string field,
            decimal legacy,
            decimal replacement,
            List<ExchangeParityFinding> findings)
        {
            decimal relative = Math.Max(Math.Abs(legacy), Math.Abs(replacement)) *
                _options.LivePriceRelativeTolerance;
            Number(
                scope,
                key,
                field,
                legacy,
                replacement,
                Math.Max(_options.LivePriceAbsoluteTolerance, relative),
                findings);
        }

        private void OptionalMarketNumber(
            string scope,
            string key,
            string field,
            string legacy,
            string replacement,
            List<ExchangeParityFinding> findings)
        {
            if (string.IsNullOrWhiteSpace(legacy) && string.IsNullOrWhiteSpace(replacement))
                return;
            MarketNumber(scope, key, field, legacy, replacement, findings);
        }

        private static void AddDocumentedExclusions(List<ExchangeParityFinding> findings)
        {
            Exclude(findings, "fill", "trade_id", ExchangeParityDisposition.Unsupported,
                "Coinbase Pro integer trade IDs and Advanced Trade string/UUID identifiers are not directly comparable.");
            Exclude(findings, "ticker", "trade_id", ExchangeParityDisposition.Unsupported,
                "The shared ticker integer field cannot preserve Advanced Trade string identifiers.");
            Exclude(findings, "order", "self_trade_prevention", ExchangeParityDisposition.Unsupported,
                "Advanced Trade read-only orders do not expose the legacy STP field in the same contract.");
            Exclude(findings, "account", "ledger", ExchangeParityDisposition.IntentionallyRemoved,
                "Advanced Trade has no direct legacy ledger equivalent; App v2 or removal needs a product decision.");
            Exclude(findings, "live_asset", "legacy_derived_indicators_and_aggregates", ExchangeParityDisposition.Unsupported,
                "RSI, volume heuristics, and aggregate balances are adapter-derived and excluded from API parity.");
        }

        private static void Exclude(
            List<ExchangeParityFinding> findings,
            string scope,
            string field,
            ExchangeParityDisposition disposition,
            string detail)
        {
            findings.Add(new ExchangeParityFinding
            {
                Scope = scope,
                Key = "*",
                Field = field,
                Disposition = disposition,
                Detail = detail
            });
        }

        private static string FillBusinessKey(Fill fill)
        {
            return $"{fill.OrderID}|{fill.ProductID}|{fill.Time.ToUniversalTime():O}";
        }

        private static void Missing(
            string scope,
            string key,
            ExchangeParityDisposition disposition,
            List<ExchangeParityFinding> findings)
        {
            findings.Add(new ExchangeParityFinding
            {
                Scope = scope,
                Key = key,
                Field = "item",
                Disposition = disposition
            });
        }

        private static string Status(Order order)
        {
            string value = order?.Status?.Trim().ToLowerInvariant();
            return order?.Settled == true && value is "done" or "completed" or "filled"
                ? "filled"
                : value;
        }

        private static void Text(
            string scope,
            string key,
            string field,
            string legacy,
            string replacement,
            List<ExchangeParityFinding> findings,
            bool ignoreCase = false)
        {
            if (!string.Equals(
                    legacy,
                    replacement,
                    ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                Mismatch(scope, key, field, legacy, replacement, findings);
        }

        private static void Boolean(
            string scope,
            string key,
            string field,
            bool legacy,
            bool replacement,
            List<ExchangeParityFinding> findings)
        {
            if (legacy != replacement)
                Mismatch(scope, key, field, legacy.ToString(), replacement.ToString(), findings);
        }

        private static void Number(
            string scope,
            string key,
            string field,
            decimal legacy,
            decimal replacement,
            decimal tolerance,
            List<ExchangeParityFinding> findings)
        {
            if (Math.Abs(legacy - replacement) > tolerance)
                Mismatch(
                    scope,
                    key,
                    field,
                    legacy.ToString(CultureInfo.InvariantCulture),
                    replacement.ToString(CultureInfo.InvariantCulture),
                    findings);
        }

        private static void Number(
            string scope,
            string key,
            string field,
            string legacy,
            string replacement,
            decimal tolerance,
            List<ExchangeParityFinding> findings)
        {
            if (!decimal.TryParse(legacy, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal left) ||
                !decimal.TryParse(replacement, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal right))
            {
                Mismatch(scope, key, field, legacy, replacement, findings, "Both values must be invariant decimals.");
                return;
            }
            Number(scope, key, field, left, right, tolerance, findings);
        }

        private static void OptionalNumber(
            string scope,
            string key,
            string field,
            string legacy,
            string replacement,
            decimal tolerance,
            List<ExchangeParityFinding> findings)
        {
            if (string.IsNullOrWhiteSpace(legacy) && string.IsNullOrWhiteSpace(replacement))
                return;
            Number(scope, key, field, legacy, replacement, tolerance, findings);
        }

        private void Timestamp(
            string scope,
            string key,
            string field,
            string legacy,
            string replacement,
            List<ExchangeParityFinding> findings)
        {
            if (!DateTimeOffset.TryParse(legacy, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset left) ||
                !DateTimeOffset.TryParse(replacement, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset right))
            {
                Mismatch(scope, key, field, legacy, replacement, findings, "Both values must be ISO-compatible timestamps.");
                return;
            }
            if ((left - right).Duration() > _options.TimestampTolerance)
                Mismatch(scope, key, field, legacy, replacement, findings);
        }

        private void Date(
            string scope,
            string key,
            string field,
            DateTime legacy,
            DateTime replacement,
            List<ExchangeParityFinding> findings)
        {
            if ((legacy.ToUniversalTime() - replacement.ToUniversalTime()).Duration() > _options.TimestampTolerance)
                Mismatch(scope, key, field, legacy.ToString("O"), replacement.ToString("O"), findings);
        }

        private static void Mismatch(
            string scope,
            string key,
            string field,
            string legacy,
            string replacement,
            List<ExchangeParityFinding> findings,
            string detail = null)
        {
            findings.Add(new ExchangeParityFinding
            {
                Scope = scope,
                Key = key,
                Field = field,
                Disposition = ExchangeParityDisposition.Mismatch,
                LegacyValue = legacy,
                ReplacementValue = replacement,
                Detail = detail
            });
        }

        private static void CompareKeyed<T>(
            string scope,
            IEnumerable<T> legacy,
            IEnumerable<T> replacement,
            Func<T, string> keySelector,
            List<ExchangeParityFinding> findings,
            Action<string, T, T> compare)
        {
            Dictionary<string, T> left = Index(scope, legacy, keySelector);
            Dictionary<string, T> right = Index(scope, replacement, keySelector);
            CompareDictionaries(scope, left, right, findings, compare);
        }

        private static Dictionary<string, T> Index<T>(
            string scope,
            IEnumerable<T> values,
            Func<T, string> keySelector)
        {
            Dictionary<string, T> result = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (T value in values ?? Enumerable.Empty<T>())
            {
                string key = keySelector(value);
                if (string.IsNullOrWhiteSpace(key))
                    throw new InvalidOperationException($"A {scope} parity item has no stable key.");
                if (!result.TryAdd(key, value))
                    throw new InvalidOperationException($"Duplicate {scope} parity key '{key}'.");
            }
            return result;
        }

        private static void CompareDictionaries<T>(
            string scope,
            IReadOnlyDictionary<string, T> legacy,
            IReadOnlyDictionary<string, T> replacement,
            List<ExchangeParityFinding> findings,
            Action<string, T, T> compare)
        {
            foreach (string key in legacy.Keys.Union(replacement.Keys, StringComparer.Ordinal)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                bool hasLegacy = legacy.TryGetValue(key, out T left);
                bool hasReplacement = replacement.TryGetValue(key, out T right);
                if (!hasLegacy || !hasReplacement)
                {
                    findings.Add(new ExchangeParityFinding
                    {
                        Scope = scope,
                        Key = key,
                        Field = "item",
                        Disposition = hasLegacy
                            ? ExchangeParityDisposition.MissingReplacement
                            : ExchangeParityDisposition.MissingLegacy
                    });
                    continue;
                }
                compare(key, left, right);
            }
        }
    }
}
