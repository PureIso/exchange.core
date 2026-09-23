using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.coinbase.advancedtrade.websockets
{
    public interface ICoinbaseUserOrderWebSocket
    {
        Task RunMessagesAsync(
            IEnumerable<string> productIds,
            Func<CoinbaseWebSocketParseResult, CancellationToken, Task> messageHandler,
            CancellationToken cancellationToken);

        Task RunAsync(
            IEnumerable<string> productIds,
            Func<CoinbaseWebSocketFrame, CancellationToken, Task> frameHandler,
            CancellationToken cancellationToken);
    }
}
