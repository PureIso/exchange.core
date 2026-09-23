using System;
using System.IO;

namespace exchange.coinbase.advancedtrade.websockets
{
    public sealed class CoinbaseWebSocketReconnectException : IOException
    {
        public CoinbaseWebSocketReconnectException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
