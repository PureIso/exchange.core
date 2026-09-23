namespace exchange.coinbase.advancedtrade.websockets
{
    public interface ICoinbaseWebSocketConnectionFactory
    {
        ICoinbaseWebSocketConnection Create();
    }
}
