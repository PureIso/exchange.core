namespace exchange.coinbase.advancedtrade.authentication
{
    public interface ICoinbaseCredentialSource
    {
        CoinbaseApiCredentials GetCredentials();
    }
}
