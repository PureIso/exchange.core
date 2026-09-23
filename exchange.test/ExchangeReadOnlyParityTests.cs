using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using exchange.core.models;
using exchange.core.parity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class ExchangeReadOnlyParityTests
    {
        private static readonly JsonSerializerOptions SerializerOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        [TestMethod]
        public void SanitizedLegacyAndAdvancedTradeFixtures_AreEquivalentWithinTolerances()
        {
            ExchangeReadOnlySnapshot legacy = LoadFixture("legacy-normalized.json");
            ExchangeReadOnlySnapshot replacement = LoadFixture("advanced-trade-normalized.json");

            ExchangeParityReport report =
                new ExchangeReadOnlyParityComparator().Compare(legacy, replacement);

            Assert.IsTrue(report.IsEquivalent);
            Assert.IsEmpty(report.BlockingFindings);
            Assert.HasCount(5, report.Findings);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "account:ledger:IntentionallyRemoved",
                    "fill:trade_id:Unsupported",
                    "live_asset:legacy_derived_indicators_and_aggregates:Unsupported",
                    "order:self_trade_prevention:Unsupported",
                    "ticker:trade_id:Unsupported"
                },
                report.Findings.Select(finding =>
                    $"{finding.Scope}:{finding.Field}:{finding.Disposition}").ToArray());
            Assert.IsTrue(report.Findings.All(finding =>
                !string.IsNullOrWhiteSpace(finding.Detail)));
        }

        [TestMethod]
        public void Comparator_ReportsMissingAndMismatchedNormalizedState()
        {
            ExchangeReadOnlySnapshot legacy = LoadFixture("legacy-normalized.json");
            ExchangeReadOnlySnapshot replacement = LoadFixture("advanced-trade-normalized.json");
            replacement.AccountBalances.Remove("USD");
            replacement.Orders[0].Status = "CANCELLED";
            replacement.Tickers[0].Price = "65100.00";

            ExchangeParityReport report =
                new ExchangeReadOnlyParityComparator().Compare(legacy, replacement);

            Assert.IsFalse(report.IsEquivalent);
            Assert.HasCount(3, report.BlockingFindings);
            Assert.IsTrue(report.BlockingFindings.Any(finding =>
                finding.Scope == "account_balance" &&
                finding.Key == "USD" &&
                finding.Disposition == ExchangeParityDisposition.MissingReplacement));
            Assert.IsTrue(report.BlockingFindings.Any(finding =>
                finding.Scope == "order" &&
                finding.Key == "order-001" &&
                finding.Field == "status"));
            Assert.IsTrue(report.BlockingFindings.Any(finding =>
                finding.Scope == "ticker" &&
                finding.Key == "BTC-USD" &&
                finding.Field == "price"));
        }

        [TestMethod]
        public void Comparator_ReportsInvalidNumbersInsteadOfTreatingThemAsEqual()
        {
            ExchangeReadOnlySnapshot legacy = LoadFixture("legacy-normalized.json");
            ExchangeReadOnlySnapshot replacement = LoadFixture("advanced-trade-normalized.json");
            replacement.Products[0].BaseMinSize = "not-a-number";

            ExchangeParityReport report =
                new ExchangeReadOnlyParityComparator().Compare(legacy, replacement);

            ExchangeParityFinding finding = report.BlockingFindings.Single(item =>
                item.Scope == "product" && item.Field == "base_min_size");
            Assert.AreEqual(ExchangeParityDisposition.Mismatch, finding.Disposition);
            StringAssert.Contains(finding.Detail, "invariant decimals");
        }

        [TestMethod]
        public void Comparator_RejectsDuplicateStableKeys()
        {
            ExchangeReadOnlySnapshot legacy = LoadFixture("legacy-normalized.json");
            ExchangeReadOnlySnapshot replacement = LoadFixture("advanced-trade-normalized.json");
            replacement.Products.Add(new Product { ID = "BTC-USD" });

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                new ExchangeReadOnlyParityComparator().Compare(legacy, replacement));

            StringAssert.Contains(exception.Message, "Duplicate product parity key");
        }

        [TestMethod]
        public void Comparator_MatchesMultiplePartialFillsByNearestTimestamp()
        {
            DateTime start = new DateTime(2026, 8, 15, 10, 0, 1, 100, DateTimeKind.Utc);
            ExchangeReadOnlySnapshot legacy = new ExchangeReadOnlySnapshot
            {
                Fills =
                {
                    CreateFill("0.10", start),
                    CreateFill("0.20", start.AddMilliseconds(400))
                }
            };
            ExchangeReadOnlySnapshot replacement = new ExchangeReadOnlySnapshot
            {
                Fills =
                {
                    CreateFill("0.20", start.AddMilliseconds(450)),
                    CreateFill("0.10", start.AddMilliseconds(50))
                }
            };

            ExchangeParityReport report =
                new ExchangeReadOnlyParityComparator().Compare(legacy, replacement);

            Assert.IsTrue(report.IsEquivalent);
            Assert.IsEmpty(report.BlockingFindings);
        }

        [TestMethod]
        public void Comparator_UsesRelativeToleranceOnlyForLiveMarketPrices()
        {
            ExchangeReadOnlySnapshot legacy = new ExchangeReadOnlySnapshot
            {
                Tickers = { new Ticker { ProductID = "BTC-USD", Price = "65000" } }
            };
            ExchangeReadOnlySnapshot replacement = new ExchangeReadOnlySnapshot
            {
                Tickers = { new Ticker { ProductID = "BTC-USD", Price = "65005" } }
            };
            ExchangeReadOnlyParityComparator comparator =
                new ExchangeReadOnlyParityComparator();

            ExchangeParityReport withinTolerance = comparator.Compare(legacy, replacement);
            replacement.Tickers[0].Price = "65010";
            ExchangeParityReport outsideTolerance = comparator.Compare(legacy, replacement);

            Assert.IsTrue(withinTolerance.IsEquivalent);
            Assert.IsFalse(outsideTolerance.IsEquivalent);
            Assert.IsTrue(outsideTolerance.BlockingFindings.Any(finding =>
                finding.Scope == "ticker" && finding.Field == "price"));
        }

        private static Fill CreateFill(string size, DateTime time)
        {
            return new Fill
            {
                ProductID = "BTC-USD",
                OrderID = "order-partial",
                Side = "buy",
                Price = "65000",
                Size = size,
                Fee = "0.01",
                Settled = true,
                Time = time
            };
        }

        private static ExchangeReadOnlySnapshot LoadFixture(string name)
        {
            string path = Path.Combine(
                FindRepositoryRoot(),
                "exchange.test",
                "Fixtures",
                "Parity",
                name);
            return JsonSerializer.Deserialize<ExchangeReadOnlySnapshot>(
                File.ReadAllText(path),
                SerializerOptions)!;
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "exchange.core.sln")))
                    return current.FullName;
                current = current.Parent;
            }
            throw new DirectoryNotFoundException(
                "The Exchange Core repository root could not be located.");
        }
    }
}
