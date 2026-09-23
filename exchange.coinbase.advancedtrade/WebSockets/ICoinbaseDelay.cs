using System;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.coinbase.advancedtrade.websockets
{
    public interface ICoinbaseDelay
    {
        Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken);
    }

    public sealed class SystemCoinbaseDelay : ICoinbaseDelay
    {
        public static SystemCoinbaseDelay Instance { get; } = new SystemCoinbaseDelay();

        private SystemCoinbaseDelay()
        {
        }

        public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (delay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(delay), "The reconnect delay cannot be negative.");
            return Task.Delay(delay, cancellationToken);
        }
    }
}
