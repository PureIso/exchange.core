using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using exchange.core.implementations;
using exchange.core.indicators;
using exchange.core.models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace exchange.test
{
    [TestClass]
    public class RelativeStrengthIndexLifecycleTests
    {
        [TestMethod]
        public async Task Updater_CancelsPromptlyAndCanRestartWithoutBackgroundCalls()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "exchange-core-rsi-" + Guid.NewGuid().ToString("N"));
            string filePrefix = Path.Combine(directory, "indicator");
            RelativeStrengthIndex indicator = new RelativeStrengthIndex(
                filePrefix,
                new Product { ID = "BTC-EUR" },
                requestDelay: TimeSpan.FromDays(1),
                updateInterval: TimeSpan.FromDays(1));
            int historicCalls = 0;
            int errorLogs = 0;
            indicator.UpdateProductHistoricCandles = _ =>
            {
                Interlocked.Increment(ref historicCalls);
                return Task.FromResult(new List<HistoricRate>());
            };
            indicator.ProcessLogBroadcast = (messageType, message) =>
                Interlocked.Increment(ref errorLogs);

            try
            {
                indicator.EnableRelativeStrengthIndexUpdater();
                indicator.EnableRelativeStrengthIndexUpdater();
                await Task.Delay(50);

                Stopwatch stopwatch = Stopwatch.StartNew();
                await indicator.DisableRelativeStrengthIndexUpdaterAsync()
                    .WaitAsync(TimeSpan.FromSeconds(2));
                stopwatch.Stop();

                Assert.IsLessThan(TimeSpan.FromSeconds(1), stopwatch.Elapsed);
                Assert.AreEqual(0, historicCalls);
                Assert.AreEqual(0, errorLogs);

                await indicator.DisableRelativeStrengthIndexUpdaterAsync();
                indicator.EnableRelativeStrengthIndexUpdater();
                await Task.Delay(50);
                await indicator.DisableRelativeStrengthIndexUpdaterAsync()
                    .WaitAsync(TimeSpan.FromSeconds(2));

                Assert.AreEqual(0, historicCalls);
                Assert.AreEqual(0, errorLogs);
            }
            finally
            {
                await indicator.DisableRelativeStrengthIndexUpdaterAsync();
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
        }

        [TestMethod]
        public async Task PluginCloseFeed_AwaitsIndicatorShutdown()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "exchange-core-rsi-close-" + Guid.NewGuid().ToString("N"));
            RelativeStrengthIndex indicator = new RelativeStrengthIndex(
                Path.Combine(directory, "indicator"),
                new Product { ID = "BTC-EUR" },
                requestDelay: TimeSpan.FromDays(1),
                updateInterval: TimeSpan.FromDays(1));
            int historicCalls = 0;
            indicator.UpdateProductHistoricCandles = _ =>
            {
                Interlocked.Increment(ref historicCalls);
                return Task.FromResult(new List<HistoricRate>());
            };
            TestExchangePlugin plugin = new TestExchangePlugin
            {
                RelativeStrengthIndices = new List<RelativeStrengthIndex> { indicator }
            };

            try
            {
                indicator.EnableRelativeStrengthIndexUpdater();
                await Task.Delay(50);

                bool closed = await plugin.CloseFeed().WaitAsync(TimeSpan.FromSeconds(2));

                Assert.IsTrue(closed);
                Assert.IsEmpty(plugin.RelativeStrengthIndices);
                Assert.AreEqual(0, historicCalls);
            }
            finally
            {
                await indicator.DisableRelativeStrengthIndexUpdaterAsync();
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
        }

        private sealed class TestExchangePlugin : AbstractExchangePlugin
        {
        }
    }
}
