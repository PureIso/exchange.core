using System;
using System.Collections.Generic;
using System.Linq;
using exchange.core.implementations;
using exchange.core.models;
using exchange.core.parity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class ExchangeParitySoakTests
    {
        [TestMethod]
        public void SnapshotCapture_CopiesSupportedPluginState()
        {
            SnapshotPlugin plugin = new SnapshotPlugin
            {
                Products = new List<Product>
                {
                    new Product { ID = "BTC-USD", BaseCurrency = "BTC", QuoteCurrency = "USD" }
                },
                AccountInfo = new Dictionary<string, decimal> { ["BTC"] = 1.25m },
                Orders = new List<Order>
                {
                    new Order { ID = "order-1", ProductID = "BTC-USD", Price = "65000" }
                },
                Fills = new List<Fill>
                {
                    new Fill { OrderID = "order-1", ProductID = "BTC-USD", Price = "65000" }
                },
                Tickers = new List<Ticker>
                {
                    new Ticker { ProductID = "BTC-USD", Price = "65000" }
                },
                AssetInformation = new Dictionary<string, AssetInformation>
                {
                    ["BTC-USD"] = new AssetInformation
                    {
                        ProductID = "BTC-USD",
                        CurrentPrice = 65000m
                    }
                }
            };

            ExchangeReadOnlySnapshot snapshot = ExchangePluginSnapshotCapture.Capture(plugin);
            plugin.Products[0].ID = "changed";
            plugin.AccountInfo["BTC"] = 0m;
            plugin.Orders[0].Price = "0";
            plugin.Fills[0].Price = "0";
            plugin.Tickers[0].Price = "0";
            plugin.AssetInformation["BTC-USD"].CurrentPrice = 0m;

            Assert.AreEqual("BTC-USD", snapshot.Products.Single().ID);
            Assert.AreEqual(1.25m, snapshot.AccountBalances["BTC"]);
            Assert.AreEqual("65000", snapshot.Orders.Single().Price);
            Assert.AreEqual("65000", snapshot.Fills.Single().Price);
            Assert.AreEqual("65000", snapshot.Tickers.Single().Price);
            Assert.AreEqual(65000m, snapshot.LiveAssets.Single().CurrentPrice);
        }

        [TestMethod]
        public void SoakSession_RecordsCountsWithoutRetainingPayloads()
        {
            ExchangeParitySoakSession session = new ExchangeParitySoakSession();
            ExchangeReadOnlySnapshot legacy = new ExchangeReadOnlySnapshot
            {
                AccountBalances = new Dictionary<string, decimal> { ["BTC"] = 1m }
            };
            ExchangeReadOnlySnapshot equal = new ExchangeReadOnlySnapshot
            {
                AccountBalances = new Dictionary<string, decimal> { ["BTC"] = 1m }
            };
            ExchangeReadOnlySnapshot different = new ExchangeReadOnlySnapshot
            {
                AccountBalances = new Dictionary<string, decimal> { ["BTC"] = 2m }
            };
            DateTimeOffset start = new DateTimeOffset(2026, 8, 16, 10, 0, 0, TimeSpan.Zero);

            ExchangeParitySoakObservation first = session.Observe(start, legacy, equal);
            ExchangeParitySoakObservation second = session.Observe(
                start.AddMinutes(1),
                legacy,
                different);
            ExchangeParitySoakObservation failure = session.RecordCollectionFailure(
                start.AddMinutes(2),
                ExchangeParityCollectionFailureCategory.ReplacementCapture);

            Assert.AreEqual(0, first.BlockingFindingCount);
            Assert.AreEqual(1, second.BlockingFindingCount);
            Assert.IsTrue(failure.CollectionFailed);
            Assert.AreEqual(
                ExchangeParityCollectionFailureCategory.ReplacementCapture,
                failure.FailureCategory);
            Assert.HasCount(3, session.Observations);
        }

        [TestMethod]
        public void Evaluator_PassesOnlyWhenAllConfiguredThresholdsAreMet()
        {
            DateTimeOffset start = new DateTimeOffset(2026, 8, 16, 10, 0, 0, TimeSpan.Zero);
            ExchangeParitySoakEvaluator evaluator = new ExchangeParitySoakEvaluator(
                new ExchangeParitySoakCriteria
                {
                    RequiredSamples = 3,
                    RequiredDuration = TimeSpan.FromMinutes(10),
                    MaximumBlockingSamples = 1,
                    MaximumConsecutiveBlockingSamples = 1,
                    MaximumCollectionFailures = 1
                });
            ExchangeParitySoakObservation[] observations =
            {
                Observation(start, 0),
                Observation(start.AddMinutes(5), 1),
                Failure(start.AddMinutes(7)),
                Observation(start.AddMinutes(10), 0)
            };

            ExchangeParitySoakReport report = evaluator.Evaluate(observations);

            Assert.IsTrue(report.Passed);
            Assert.AreEqual(4, report.ObservationCount);
            Assert.AreEqual(3, report.SampleCount);
            Assert.AreEqual(TimeSpan.FromMinutes(10), report.ObservedDuration);
            Assert.AreEqual(1, report.BlockingSampleCount);
            Assert.AreEqual(1, report.MaximumConsecutiveBlockingSamples);
            Assert.AreEqual(1, report.CollectionFailureCount);
            Assert.IsEmpty(report.RollbackReasons);
        }

        [TestMethod]
        public void Evaluator_EmitsStableRollbackReasonsForEveryExceededGate()
        {
            DateTimeOffset start = new DateTimeOffset(2026, 8, 16, 10, 0, 0, TimeSpan.Zero);
            ExchangeParitySoakEvaluator evaluator = new ExchangeParitySoakEvaluator(
                new ExchangeParitySoakCriteria
                {
                    RequiredSamples = 3,
                    RequiredDuration = TimeSpan.FromHours(1),
                    MaximumBlockingSamples = 0,
                    MaximumConsecutiveBlockingSamples = 0,
                    MaximumCollectionFailures = 0
                });

            ExchangeParitySoakReport report = evaluator.Evaluate(new[]
            {
                Observation(start, 1),
                Observation(start.AddMinutes(1), 2),
                Failure(start.AddMinutes(2))
            });

            Assert.IsFalse(report.Passed);
            CollectionAssert.AreEqual(
                new[]
                {
                    "insufficient_samples",
                    "insufficient_duration",
                    "blocking_parity_threshold_exceeded",
                    "consecutive_parity_threshold_exceeded",
                    "collection_failure_threshold_exceeded"
                },
                report.RollbackReasons.ToArray());
        }

        [TestMethod]
        public void Evaluator_RejectsInvalidCriteria()
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ExchangeParitySoakEvaluator(new ExchangeParitySoakCriteria
                {
                    RequiredSamples = 0
                }));

            StringAssert.Contains(exception.Message, "At least one soak sample");
        }

        private static ExchangeParitySoakObservation Observation(
            DateTimeOffset timestamp,
            int blockingFindings)
        {
            return new ExchangeParitySoakObservation
            {
                Timestamp = timestamp,
                BlockingFindingCount = blockingFindings
            };
        }

        private static ExchangeParitySoakObservation Failure(DateTimeOffset timestamp)
        {
            return new ExchangeParitySoakObservation
            {
                Timestamp = timestamp,
                CollectionFailed = true,
                FailureCategory = ExchangeParityCollectionFailureCategory.ObservationTimeout
            };
        }

        private sealed class SnapshotPlugin : AbstractExchangePlugin
        {
        }
    }
}
