using System.IO;

namespace exchange.coinbase.advancedtrade.websockets
{
    public sealed class CoinbaseWebSocketResynchronizationException : IOException
    {
        public CoinbaseWebSocketResynchronizationException()
            : base("Coinbase WebSocket resynchronization was requested.")
        {
        }
    }
}
