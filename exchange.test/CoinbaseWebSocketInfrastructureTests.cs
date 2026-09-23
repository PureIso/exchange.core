using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase.advancedtrade.authentication;
using exchange.coinbase.advancedtrade.websockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseWebSocketInfrastructureTests
    {
        [TestMethod]
        public void Dependencies_DefaultToProductionBoundaries()
        {
            CoinbaseWebSocketDependencies dependencies = new CoinbaseWebSocketDependencies();

            Assert.AreSame(ClientWebSocketConnectionFactory.Instance, dependencies.ConnectionFactory);
            Assert.AreSame(SystemCoinbaseDelay.Instance, dependencies.Delay);
            Assert.IsNotNull(dependencies.Clock);
        }

        [TestMethod]
        public void Dependencies_RetainInjectedBoundaries()
        {
            Mock<ICoinbaseWebSocketConnectionFactory> connectionFactory = new Mock<ICoinbaseWebSocketConnectionFactory>();
            FixedClock clock = new FixedClock(new DateTimeOffset(2026, 8, 15, 10, 0, 0, TimeSpan.Zero));
            Mock<ICoinbaseDelay> delay = new Mock<ICoinbaseDelay>();

            CoinbaseWebSocketDependencies dependencies = new CoinbaseWebSocketDependencies(
                connectionFactory.Object,
                clock,
                delay.Object);

            Assert.AreSame(connectionFactory.Object, dependencies.ConnectionFactory);
            Assert.AreSame(clock, dependencies.Clock);
            Assert.AreSame(delay.Object, dependencies.Delay);
        }

        [TestMethod]
        public void ConnectionFactory_ReturnsInjectedConnection()
        {
            Mock<ICoinbaseWebSocketConnection> connection =
                new Mock<ICoinbaseWebSocketConnection>(MockBehavior.Strict);
            ClientWebSocketConnectionFactory factory = new ClientWebSocketConnectionFactory(
                () => connection.Object);

            Assert.AreSame(connection.Object, factory.Create());
        }

        [TestMethod]
        public void ConnectionFactory_DefaultCreatesIndependentClosedConnections()
        {
            using ICoinbaseWebSocketConnection first =
                ClientWebSocketConnectionFactory.Instance.Create();
            using ICoinbaseWebSocketConnection second =
                ClientWebSocketConnectionFactory.Instance.Create();

            Assert.AreEqual(WebSocketState.None, first.State);
            Assert.AreEqual(WebSocketState.None, second.State);
            Assert.AreNotSame(first, second);
        }

        [TestMethod]
        public void ConnectionFactory_RejectsMissingSocket()
        {
            ClientWebSocketConnectionFactory factory = new ClientWebSocketConnectionFactory(() => null);

            Assert.Throws<InvalidOperationException>(() => factory.Create());
        }

        [TestMethod]
        public async Task Delay_ObservesCancellationAndRejectsNegativeDuration()
        {
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                SystemCoinbaseDelay.Instance.WaitAsync(TimeSpan.FromSeconds(1), cancellation.Token));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SystemCoinbaseDelay.Instance.WaitAsync(TimeSpan.FromMilliseconds(-1), CancellationToken.None));
        }

        private sealed class FixedClock : ICoinbaseClock
        {
            public FixedClock(DateTimeOffset utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTimeOffset UtcNow { get; }
        }
    }
}
