using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.coinbase.advancedtrade.websockets
{
    public interface ICoinbaseMarketDataWebSocket
    {
        Task RunMessagesAsync(
            IEnumerable<string> productIds,
            Func<CoinbaseWebSocketParseResult, CancellationToken, Task> messageHandler,
            CancellationToken cancellationToken);
    }
}
