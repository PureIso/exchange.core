using System;

namespace exchange.coinbase.advancedtrade.authentication
{
    public interface ICoinbaseClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
