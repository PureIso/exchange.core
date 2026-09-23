using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace exchange.coinbase.advancedtrade.authentication
{
    public sealed class CoinbaseJwtAuthenticationHandler : DelegatingHandler
    {
        private readonly ICoinbaseJwtProvider _jwtProvider;

        public CoinbaseJwtAuthenticationHandler(ICoinbaseJwtProvider jwtProvider)
        {
            _jwtProvider = jwtProvider ?? throw new ArgumentNullException(nameof(jwtProvider));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.RequestUri == null)
                throw new InvalidOperationException("An authenticated Coinbase request requires a URI.");

            string token = _jwtProvider.CreateRestToken(request.Method, request.RequestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                throw new HttpRequestException(
                    "The authenticated Coinbase request failed.",
                    null,
                    exception.StatusCode);
            }
            finally
            {
                request.Headers.Authorization = null;
            }
        }
    }
}
