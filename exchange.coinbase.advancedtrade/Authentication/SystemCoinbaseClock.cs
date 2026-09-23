using System;

namespace exchange.coinbase.advancedtrade.authentication
{
    internal sealed class SystemCoinbaseClock : ICoinbaseClock
    {
        public static SystemCoinbaseClock Instance { get; } = new SystemCoinbaseClock();

        private SystemCoinbaseClock()
        {
        }

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
