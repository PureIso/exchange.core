namespace exchange.coinbase.advancedtrade.authentication
{
    public interface ICoinbaseNonceProvider
    {
        string CreateNonce();
    }
}
