using System;
using exchange.coinbase.advancedtrade.authentication;

namespace exchange.coinbase.advancedtrade.websockets
{
    public sealed class CoinbaseWebSocketDependencies
    {
        public CoinbaseWebSocketDependencies(
            ICoinbaseWebSocketConnectionFactory connectionFactory = null,
            ICoinbaseClock clock = null,
            ICoinbaseDelay delay = null)
        {
            ConnectionFactory = connectionFactory ?? ClientWebSocketConnectionFactory.Instance;
            Clock = clock ?? SystemCoinbaseClock.Instance;
            Delay = delay ?? SystemCoinbaseDelay.Instance;
        }

        public ICoinbaseWebSocketConnectionFactory ConnectionFactory { get; }

        public ICoinbaseClock Clock { get; }

        public ICoinbaseDelay Delay { get; }
    }
}
