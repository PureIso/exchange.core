using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    public sealed class CoinbaseAdvancedTradeClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly CoinbaseAdvancedTradeOptions _options;

        public CoinbaseAdvancedTradeClient(HttpClient httpClient, CoinbaseAdvancedTradeOptions options = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? new CoinbaseAdvancedTradeOptions();
            _options.Validate();
        }

        public async Task<IReadOnlyList<Product>> ListSpotProductsAsync(
            CancellationToken cancellationToken = default)
        {
            List<Product> products = new List<Product>();
            HashSet<string> visitedCursors = new HashSet<string>(StringComparer.Ordinal);
            string cursor = null;

            do
            {
                AdvancedTradeProductsResponse page = await GetProductPageAsync(cursor, cancellationToken);
                products.AddRange((page.Products ?? new List<AdvancedTradeProduct>())
                    .Select(AdvancedTradeProductMapper.ToCoreProduct));

                if (page.Pagination?.HasNext != true)
                    break;

                cursor = page.Pagination.NextCursor;
                if (string.IsNullOrWhiteSpace(cursor) || !visitedCursors.Add(cursor))
                    throw new InvalidDataException("Coinbase returned an invalid or repeated product cursor.");
            } while (true);

            return products;
        }

        public async Task<Product> GetProductAsync(
            string productId,
            CancellationToken cancellationToken = default)
        {
            AdvancedTradeProduct product = await GetProductResponseAsync(productId, cancellationToken);
            return AdvancedTradeProductMapper.ToCoreProduct(product);
        }

        public async Task<Statistics> GetTwentyFourHourStatisticsAsync(
            string productId,
            CancellationToken cancellationToken = default)
        {
            AdvancedTradeProduct product = await GetProductResponseAsync(productId, cancellationToken);
            return AdvancedTradeProductMapper.ToCoreStatistics(product);
        }

        public async Task<CoinbaseMarketTradesSnapshot> GetMarketTradesAsync(
            string productId,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), "Trade limit must be greater than zero.");

            Uri requestUri = BuildUri(
                $"market/products/{EscapeProductId(productId)}/ticker?limit={limit}");
            AdvancedTradeMarketTradesResponse response =
                await GetJsonAsync<AdvancedTradeMarketTradesResponse>(requestUri, cancellationToken);

            return new CoinbaseMarketTradesSnapshot
            {
                BestBid = response.BestBid,
                BestAsk = response.BestAsk,
                Trades = (response.Trades ?? new List<AdvancedTradeMarketTrade>())
                    .Select(ToMarketTrade)
                    .ToArray()
            };
        }

        public async Task<OrderBook> GetProductBookAsync(
            string productId,
            int limit = 50,
            CancellationToken cancellationToken = default)
        {
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), "Order-book limit must be greater than zero.");

            Uri requestUri = BuildUri(
                $"market/product_book?product_id={Uri.EscapeDataString(RequireProductId(productId))}&limit={limit}");
            AdvancedTradeProductBookResponse response =
                await GetJsonAsync<AdvancedTradeProductBookResponse>(requestUri, cancellationToken);
            if (response.PriceBook == null)
                throw new JsonException("Coinbase returned a product-book response without pricebook.");

            return new OrderBook
            {
                Bids = ToCorePriceLevels(response.PriceBook.Bids),
                Asks = ToCorePriceLevels(response.PriceBook.Asks)
            };
        }

        public async Task<IReadOnlyList<HistoricRate>> GetCandlesAsync(
            HistoricCandlesSearch search,
            CancellationToken cancellationToken = default)
        {
            if (search == null)
                throw new ArgumentNullException(nameof(search));

            string productId = EscapeProductId(search.Symbol);
            long start = ToUnixSeconds(search.StartingDateTime);
            long end = ToUnixSeconds(search.EndingDateTime);
            if (start >= end)
                throw new ArgumentException("Candle start time must be earlier than end time.", nameof(search));

            int bucketSeconds = checked((int)search.Granularity);
            string granularity = ToCoinbaseGranularity(search.Granularity);
            Dictionary<long, HistoricRate> ratesByStart = new Dictionary<long, HistoricRate>();

            for (long windowStart = start; windowStart < end;)
            {
                long windowEnd = Math.Min(end, checked(windowStart + (long)bucketSeconds * 350));
                int limit = (int)Math.Min(350, Math.Ceiling((windowEnd - windowStart) / (double)bucketSeconds));
                Uri requestUri = BuildUri(
                    $"market/products/{productId}/candles?start={windowStart}&end={windowEnd}" +
                    $"&granularity={granularity}&limit={limit}");
                AdvancedTradeCandlesResponse response =
                    await GetJsonAsync<AdvancedTradeCandlesResponse>(requestUri, cancellationToken);

                foreach (AdvancedTradeCandle candle in response.Candles ?? new List<AdvancedTradeCandle>())
                {
                    long candleStart = ParseUnixSeconds(candle.Start, "start");
                    if (candleStart >= start && candleStart < end)
                        ratesByStart[candleStart] = ToHistoricRate(candle, candleStart);
                }

                windowStart = windowEnd;
            }

            return ratesByStart
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Value)
                .ToArray();
        }

        private async Task<AdvancedTradeProductsResponse> GetProductPageAsync(
            string cursor,
            CancellationToken cancellationToken)
        {
            return await GetJsonAsync<AdvancedTradeProductsResponse>(BuildProductPageUri(cursor), cancellationToken);
        }

        private async Task<AdvancedTradeProduct> GetProductResponseAsync(
            string productId,
            CancellationToken cancellationToken)
        {
            Uri requestUri = BuildUri($"market/products/{EscapeProductId(productId)}");
            return await GetJsonAsync<AdvancedTradeProduct>(requestUri, cancellationToken);
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

        private Uri BuildProductPageUri(string cursor)
        {
            string query = $"product_type=SPOT&limit={_options.ProductPageSize}";
            if (!string.IsNullOrWhiteSpace(cursor))
                query += $"&cursor={Uri.EscapeDataString(cursor)}";
            return BuildUri($"market/products?{query}");
        }

        private Uri BuildUri(string relativeUri)
        {
            string baseUri = _options.RestBaseUri.AbsoluteUri.TrimEnd('/') + "/";
            return new Uri(new Uri(baseUri), relativeUri);
        }

        private static string EscapeProductId(string productId)
        {
            return Uri.EscapeDataString(RequireProductId(productId));
        }

        private static string RequireProductId(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException("A Coinbase product ID is required.", nameof(productId));
            return productId;
        }

        private static CoinbaseMarketTrade ToMarketTrade(AdvancedTradeMarketTrade source)
        {
            if (!DateTimeOffset.TryParse(
                    source.Time,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset time))
                throw new JsonException("Coinbase returned a market trade with an invalid time.");

            return new CoinbaseMarketTrade
            {
                TradeId = source.TradeId,
                ProductId = source.ProductId,
                Price = source.Price,
                Size = source.Size,
                Time = time,
                Side = source.Side,
                Exchange = source.Exchange
            };
        }

        private static ArrayList[] ToCorePriceLevels(IReadOnlyCollection<AdvancedTradePriceLevel> levels)
        {
            if (levels == null)
                return Array.Empty<ArrayList>();

            return levels.Select(level => new ArrayList
            {
                JsonSerializer.SerializeToElement(level.Price),
                JsonSerializer.SerializeToElement(level.Size),
                JsonSerializer.SerializeToElement(0)
            }).ToArray();
        }

        private static long ToUnixSeconds(DateTime value)
        {
            return new DateTimeOffset(value.ToUniversalTime()).ToUnixTimeSeconds();
        }

        private static string ToCoinbaseGranularity(Granularity granularity)
        {
            return granularity switch
            {
                Granularity.FiveMinutes => "FIVE_MINUTE",
                Granularity.Fifteen => "FIFTEEN_MINUTE",
                Granularity.OneHour => "ONE_HOUR",
                Granularity.OneDay => "ONE_DAY",
                _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity,
                    "The candle granularity is not supported by Coinbase Advanced Trade.")
            };
        }

        private static HistoricRate ToHistoricRate(AdvancedTradeCandle candle, long start)
        {
            return new HistoricRate
            {
                DateAndTime = DateTimeOffset.FromUnixTimeSeconds(start).UtcDateTime,
                Low = ParseDecimal(candle.Low, "low"),
                High = ParseDecimal(candle.High, "high"),
                Open = ParseDecimal(candle.Open, "open"),
                Close = ParseDecimal(candle.Close, "close"),
                Volume = ParseDecimal(candle.Volume, "volume")
            };
        }

        private static long ParseUnixSeconds(string value, string fieldName)
        {
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result))
                return result;
            throw new JsonException($"Coinbase returned an invalid candle {fieldName}.");
        }

        private static decimal ParseDecimal(string value, string fieldName)
        {
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal result))
                return result;
            throw new JsonException($"Coinbase returned an invalid candle {fieldName}.");
        }
    }
}
