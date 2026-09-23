using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase.advancedtrade.models;
using exchange.core.models;

namespace exchange.coinbase.advancedtrade
{
    public sealed class CoinbaseAdvancedTradeAuthenticatedClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly CoinbaseAdvancedTradeOptions _options;

        public CoinbaseAdvancedTradeAuthenticatedClient(
            HttpClient httpClient,
            CoinbaseAdvancedTradeOptions options = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? new CoinbaseAdvancedTradeOptions();
            _options.Validate();
        }

        public async Task<IReadOnlyList<Account>> ListAccountsAsync(
            CancellationToken cancellationToken = default)
        {
            List<Account> accounts = new List<Account>();
            HashSet<string> visitedCursors = new HashSet<string>(StringComparer.Ordinal);
            string cursor = null;

            do
            {
                string query = $"limit={_options.AccountPageSize}";
                if (!string.IsNullOrWhiteSpace(cursor))
                    query += $"&cursor={Uri.EscapeDataString(cursor)}";
                AdvancedTradeAccountsResponse page = await GetJsonAsync<AdvancedTradeAccountsResponse>(
                    BuildUri($"accounts?{query}"),
                    cancellationToken);
                accounts.AddRange((page.Accounts ?? new List<AdvancedTradeAccount>())
                    .Select(AdvancedTradeAccountMapper.ToCoreAccount));

                if (!page.HasNext)
                    break;
                cursor = page.Cursor;
                if (string.IsNullOrWhiteSpace(cursor) || !visitedCursors.Add(cursor))
                    throw new InvalidDataException("Coinbase returned an invalid or repeated account cursor.");
            } while (true);

            return accounts;
        }

        public Task<CoinbaseApiKeyPermissions> GetApiKeyPermissionsAsync(
            CancellationToken cancellationToken = default)
        {
            return GetJsonAsync<CoinbaseApiKeyPermissions>(
                BuildUri("key_permissions"),
                cancellationToken);
        }

        public async Task<Account> GetAccountAsync(
            string accountUuid,
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(accountUuid, out Guid parsedUuid))
                throw new ArgumentException("A valid Coinbase account UUID is required.", nameof(accountUuid));

            AdvancedTradeAccountResponse response = await GetJsonAsync<AdvancedTradeAccountResponse>(
                BuildUri($"accounts/{parsedUuid:D}"),
                cancellationToken);
            if (response.Account == null)
                throw new JsonException("Coinbase returned an account response without account.");
            return AdvancedTradeAccountMapper.ToCoreAccount(response.Account);
        }

        public async Task<IReadOnlyList<Order>> ListOrdersAsync(
            string productId = null,
            CancellationToken cancellationToken = default)
        {
            List<Order> orders = new List<Order>();
            HashSet<string> visitedCursors = new HashSet<string>(StringComparer.Ordinal);
            string cursor = null;

            do
            {
                string query = $"limit={_options.OrderPageSize}";
                if (!string.IsNullOrWhiteSpace(productId))
                    query += $"&product_ids={Uri.EscapeDataString(productId)}";
                if (!string.IsNullOrWhiteSpace(cursor))
                    query += $"&cursor={Uri.EscapeDataString(cursor)}";
                AdvancedTradeOrdersResponse page = await GetJsonAsync<AdvancedTradeOrdersResponse>(
                    BuildUri($"orders/historical/batch?{query}"),
                    cancellationToken);
                ThrowIfProofTokenRequired(page.ProofTokenRequired);
                orders.AddRange((page.Orders ?? new List<AdvancedTradeOrder>())
                    .Select(AdvancedTradeOrderMapper.ToCoreOrder));

                if (!page.HasNext)
                    break;
                cursor = ValidateNextCursor(page.Cursor, visitedCursors, "order");
            } while (true);

            return orders;
        }

        public async Task<IReadOnlyList<Fill>> ListFillsAsync(
            string productId = null,
            CancellationToken cancellationToken = default)
        {
            List<Fill> fills = new List<Fill>();
            HashSet<string> visitedCursors = new HashSet<string>(StringComparer.Ordinal);
            string cursor = null;

            do
            {
                string query = $"limit={_options.FillPageSize}";
                if (!string.IsNullOrWhiteSpace(productId))
                    query += $"&product_ids={Uri.EscapeDataString(productId)}";
                if (!string.IsNullOrWhiteSpace(cursor))
                    query += $"&cursor={Uri.EscapeDataString(cursor)}";
                AdvancedTradeFillsResponse page = await GetJsonAsync<AdvancedTradeFillsResponse>(
                    BuildUri($"orders/historical/fills?{query}"),
                    cancellationToken);
                ThrowIfProofTokenRequired(page.ProofTokenRequired);
                fills.AddRange((page.Fills ?? new List<AdvancedTradeFill>())
                    .Select(AdvancedTradeFillMapper.ToCoreFill));

                if (string.IsNullOrWhiteSpace(page.Cursor))
                    break;
                cursor = ValidateNextCursor(page.Cursor, visitedCursors, "fill");
            } while (true);

            return fills;
        }

        private async Task<T> GetJsonAsync<T>(Uri requestUri, CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            using HttpResponseMessage response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            T result = await JsonSerializer.DeserializeAsync<T>(
                responseStream,
                SerializerOptions,
                cancellationToken);
            return result ?? throw new JsonException("Coinbase returned an empty JSON response.");
        }

        private Uri BuildUri(string relativeUri)
        {
            string baseUri = _options.RestBaseUri.AbsoluteUri.TrimEnd('/') + "/";
            return new Uri(new Uri(baseUri), relativeUri);
        }

        private static string ValidateNextCursor(
            string cursor,
            ISet<string> visitedCursors,
            string resourceName)
        {
            if (string.IsNullOrWhiteSpace(cursor) || !visitedCursors.Add(cursor))
                throw new InvalidDataException(
                    $"Coinbase returned an invalid or repeated {resourceName} cursor.");
            return cursor;
        }

        private static void ThrowIfProofTokenRequired(bool proofTokenRequired)
        {
            if (proofTokenRequired)
                throw new NotSupportedException(
                    "Coinbase requires an EU SCA proof token for this history request; proof tokens are not implemented.");
        }
    }
}
