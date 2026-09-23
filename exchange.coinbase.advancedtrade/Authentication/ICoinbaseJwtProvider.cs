using System;
using System.Net.Http;

namespace exchange.coinbase.advancedtrade.authentication
{
    public interface ICoinbaseJwtProvider
    {
        string CreateRestToken(HttpMethod method, Uri requestUri);
        string CreateWebSocketToken();
    }
}
