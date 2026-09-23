using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase.advancedtrade.websockets;
using exchange.core.enums;
using exchange.core.implementations;
using exchange.core.interfaces;
using exchange.core.models;

namespace exchange.coinbase.advancedtrade
{
    public sealed class CoinbaseAdvancedTrade : AbstractExchangePlugin, IDisposable
    {
        private readonly CoinbaseAdvancedTradeClient _client;
        private readonly CoinbaseAdvancedTradeAuthenticatedClient _authenticatedClient;
        private readonly HttpClient _ownedHttpClient;
        private readonly ICoinbaseMarketDataWebSocket _marketDataWebSocket;
        private readonly ICoinbaseUserOrderWebSocket _userOrderWebSocket;
        private readonly SemaphoreSlim _feedGate = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _feedCancellation;
        private Task _feedTask;
        private Task _userOrderFeedTask;
        private bool _disposed;

        public CoinbaseAdvancedTrade()
        {
            _ownedHttpClient = new HttpClient();
            _client = new CoinbaseAdvancedTradeClient(_ownedHttpClient);
            _marketDataWebSocket = new CoinbaseAdvancedTradeMarketDataWebSocket();
            SetMetadata();
        }

        public CoinbaseAdvancedTrade(
            CoinbaseAdvancedTradeClient client,
            CoinbaseAdvancedTradeAuthenticatedClient authenticatedClient = null,
            ICoinbaseMarketDataWebSocket marketDataWebSocket = null,
            ICoinbaseUserOrderWebSocket userOrderWebSocket = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _authenticatedClient = authenticatedClient;
            _marketDataWebSocket = marketDataWebSocket
                ?? new CoinbaseAdvancedTradeMarketDataWebSocket();
            _userOrderWebSocket = userOrderWebSocket;
            SetMetadata();
        }

        public Dictionary<string, CoinbaseMarketTradesSnapshot> MarketTrades { get; private set; }

        public Dictionary<string, OrderBook> ProductOrderBooks { get; private set; }

        public override async Task<bool> InitAsync(IExchangeSettings exchangeSettings)
        {
            if (exchangeSettings == null)
                throw new ArgumentNullException(nameof(exchangeSettings));

            InitializeState(exchangeSettings);
            try
            {
                await UpdateProductsAsync();
                return true;
            }
            catch (Exception exception)
            {
                ProcessLogBroadcast?.Invoke(
                    ApplicationName,
                    MessageType.Error,
                    $"Method: InitAsync\r\nError: {exception.Message}");
                return false;
            }
        }

        public async Task<List<Product>> UpdateProductsAsync()
        {
            Products = (await _client.ListSpotProductsAsync()).ToList();
            return Products;
        }

        public async Task<List<Account>> UpdateAccountsAsync(string accountUuid = null)
        {
            CoinbaseAdvancedTradeAuthenticatedClient authenticatedClient = RequireAuthenticatedClient();

            if (string.IsNullOrWhiteSpace(accountUuid))
                Accounts = (await authenticatedClient.ListAccountsAsync()).ToList();
            else
                Accounts = new List<Account> { await authenticatedClient.GetAccountAsync(accountUuid) };

            AccountInfo = Accounts
                .Where(account => decimal.TryParse(
                    account.Balance,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out decimal balance) && balance > 0)
                .GroupBy(account => account.Currency, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(account =>
                        decimal.Parse(account.Balance, NumberStyles.Float, CultureInfo.InvariantCulture)),
                    StringComparer.Ordinal);

            if (NotifyAccountInfo != null)
                await NotifyAccountInfo(ApplicationName, AccountInfo);
            if (NotifyMainCurrency != null)
                await NotifyMainCurrency(ApplicationName, MainCurrency);
            return Accounts;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            string productId = RequireProductId(product);
            Product updatedProduct = await _client.GetProductAsync(productId);
            Products ??= new List<Product>();
            int existingIndex = Products.FindIndex(existing =>
                string.Equals(existing.ID, productId, StringComparison.Ordinal));
            if (existingIndex >= 0)
                Products[existingIndex] = updatedProduct;
            else
                Products.Add(updatedProduct);
            SelectedProduct = updatedProduct;
            return updatedProduct;
        }

        public async Task<CoinbaseMarketTradesSnapshot> UpdateMarketTradesAsync(
            Product product,
            int limit = 100)
        {
            string productId = RequireProductId(product);
            CoinbaseMarketTradesSnapshot snapshot = await _client.GetMarketTradesAsync(productId, limit);
            MarketTrades ??= new Dictionary<string, CoinbaseMarketTradesSnapshot>(StringComparer.Ordinal);
            MarketTrades[productId] = snapshot;
            SelectedProduct = product;

            CoinbaseMarketTrade latestTrade = snapshot.Trades.FirstOrDefault();
            if (latestTrade != null && decimal.TryParse(
                    latestTrade.Price,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out decimal price))
            {
                CurrentPrices ??= new Dictionary<string, decimal>(StringComparer.Ordinal);
                CurrentPrices[productId] = price;
                if (NotifyCurrentPrices != null)
                    await NotifyCurrentPrices(ApplicationName, CurrentPrices);
            }

            return snapshot;
        }

        public async Task<OrderBook> UpdateProductOrderBookAsync(Product product, int level = 50)
        {
            OrderBook = await _client.GetProductBookAsync(RequireProductId(product), level);
            SelectedProduct = product;
            return OrderBook;
        }

        public override async Task<List<HistoricRate>> UpdateProductHistoricCandlesAsync(
            HistoricCandlesSearch historicCandlesSearch)
        {
            HistoricRates = (await _client.GetCandlesAsync(historicCandlesSearch)).ToList();
            return HistoricRates;
        }

        public override async Task<Statistics> TwentyFourHoursRollingStatsAsync(Product product)
        {
            string productId = RequireProductId(product);
            Statistics statistics = await _client.GetTwentyFourHourStatisticsAsync(productId);
            Statistics ??= new Dictionary<string, Statistics>(StringComparer.Ordinal);
            Statistics[productId] = statistics;
            SelectedProduct = product;
            return statistics;
        }

        public override async Task ChangeFeed(List<Product> products)
        {
            ThrowIfDisposed();
            await _feedGate.WaitAsync();
            try
            {
                await StopFeedCoreAsync();
                SubscribeProducts = (products ?? new List<Product>())
                    .Where(product => !string.IsNullOrWhiteSpace(product?.ID))
                    .GroupBy(product => product.ID, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .OrderBy(product => product.ID, StringComparer.Ordinal)
                    .ToList();
                if (SubscribeProducts.Count == 0)
                {
                    if (DelegateNotifySubscriptionStatus != null)
                        await DelegateNotifySubscriptionStatus(ApplicationName, false);
                    return;
                }

                _feedCancellation = new CancellationTokenSource();
                if (DelegateNotifySubscriptionStatus != null)
                    await DelegateNotifySubscriptionStatus(ApplicationName, true);
                string[] productIds = SubscribeProducts.Select(product => product.ID).ToArray();
                _feedTask = ProcessFeedAsync(productIds, _feedCancellation.Token);
                _userOrderFeedTask = _userOrderWebSocket == null
                    ? null
                    : ProcessUserOrderFeedAsync(productIds, _feedCancellation.Token);
            }
            finally
            {
                _feedGate.Release();
            }
        }

        public override async Task<bool> CloseFeed()
        {
            await _feedGate.WaitAsync();
            try
            {
                await StopFeedCoreAsync();
                SubscribeProducts = new List<Product>();
                return true;
            }
            finally
            {
                _feedGate.Release();
            }
        }

        public override bool InitIndicatorsAsync(List<Product> products)
        {
            throw new NotSupportedException(
                "Advanced Trade indicators require the WebSocket feed and are not implemented.");
        }

        public override Task<List<Order>> CancelAllOrdersAsync(Product product)
        {
            return UnsupportedAsync<List<Order>>("Advanced Trade authenticated order cancellation is not implemented.");
        }

        public override Task<List<Order>> CancelOrderAsync(Order order)
        {
            return UnsupportedAsync<List<Order>>("Advanced Trade authenticated order cancellation is not implemented.");
        }

        public override async Task<List<Order>> UpdateOrdersAsync(Product product = null)
        {
            CoinbaseAdvancedTradeAuthenticatedClient authenticatedClient = RequireAuthenticatedClient();
            Orders = (await authenticatedClient.ListOrdersAsync(product?.ID)).ToList();
            if (NotifyOrders != null)
                await NotifyOrders(ApplicationName, Orders);
            return Orders;
        }

        public override async Task<List<Fill>> UpdateFillsAsync(Product product)
        {
            CoinbaseAdvancedTradeAuthenticatedClient authenticatedClient = RequireAuthenticatedClient();
            Fills = (await authenticatedClient.ListFillsAsync(product?.ID)).ToList();
            if (NotifyFills != null)
                await NotifyFills(ApplicationName, Fills);
            return Fills;
        }

        public override Task<Order> PostOrdersAsync(Order order)
        {
            return UnsupportedAsync<Order>("Advanced Trade authenticated order placement is not implemented.");
        }

        public override void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _feedCancellation?.Cancel();
            try
            {
                Task.WhenAll(new[] { _feedTask, _userOrderFeedTask }
                        .Where(task => task != null))
                    .GetAwaiter()
                    .GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            _feedCancellation?.Dispose();
            _feedGate.Dispose();
            _ownedHttpClient?.Dispose();
            base.Dispose();
        }

        private void SetMetadata()
        {
            ApplicationName = "Coinbase Advanced Trade";
            Description = "Coinbase Advanced Trade adapter (public REST, read-only accounts, orders, and fills).";
            Author = "exchange.core";
            Version = "0.1.0";
        }

        private void InitializeState(IExchangeSettings exchangeSettings)
        {
            AccountInfo = new Dictionary<string, decimal>(StringComparer.Ordinal);
            Accounts = new List<Account>();
            AssetInformation = new Dictionary<string, AssetInformation>(StringComparer.Ordinal);
            CurrentPrices = new Dictionary<string, decimal>(StringComparer.Ordinal);
            Fills = new List<Fill>();
            HistoricRates = new List<HistoricRate>();
            MarketTrades = new Dictionary<string, CoinbaseMarketTradesSnapshot>(StringComparer.Ordinal);
            ProductOrderBooks = new Dictionary<string, OrderBook>(StringComparer.Ordinal);
            OrderBook = new OrderBook();
            Orders = new List<Order>();
            Products = new List<Product>();
            Statistics = new Dictionary<string, Statistics>(StringComparer.Ordinal);
            SubscribeProducts = new List<Product>();
            SubscribedPrices = new Dictionary<string, decimal>(StringComparer.Ordinal);
            Tickers = new List<Ticker>();
            TestMode = exchangeSettings.TestMode;
            IndicatorSaveDataPath = exchangeSettings.IndicatorDirectoryPath;
            INIFilePath = exchangeSettings.INIDirectoryPath;
            MainCurrency = exchangeSettings.MainCurrency;
        }

        private static string RequireProductId(Product product)
        {
            if (string.IsNullOrWhiteSpace(product?.ID))
                throw new ArgumentException("A product with a Coinbase product ID is required.", nameof(product));
            return product.ID;
        }

        private CoinbaseAdvancedTradeAuthenticatedClient RequireAuthenticatedClient()
        {
            return _authenticatedClient ?? throw new NotSupportedException(
                "Coinbase Advanced Trade authenticated REST has not been configured.");
        }

        private async Task ProcessFeedAsync(
            string[] productIds,
            CancellationToken cancellationToken)
        {
            try
            {
                await _marketDataWebSocket.RunMessagesAsync(
                    productIds,
                    HandleWebSocketMessageAsync,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                ProcessLogBroadcast?.Invoke(
                    ApplicationName,
                    MessageType.Error,
                    exception is CoinbaseWebSocketReconnectException
                        ? "Method: ProcessFeedAsync\r\nError: Coinbase market-data WebSocket reconnect attempts were exhausted."
                        : "Method: ProcessFeedAsync\r\nError: Coinbase market-data WebSocket stopped unexpectedly.");
            }
            finally
            {
                if (DelegateNotifySubscriptionStatus != null)
                    await DelegateNotifySubscriptionStatus(ApplicationName, false);
            }
        }

        private async Task ProcessUserOrderFeedAsync(
            string[] productIds,
            CancellationToken cancellationToken)
        {
            try
            {
                await _userOrderWebSocket.RunMessagesAsync(
                    productIds,
                    HandleUserOrderWebSocketMessageAsync,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                ProcessLogBroadcast?.Invoke(
                    ApplicationName,
                    MessageType.Error,
                    exception is CoinbaseWebSocketReconnectException
                        ? "Method: ProcessUserOrderFeedAsync\r\nError: Coinbase user-order WebSocket reconnect attempts were exhausted."
                        : "Method: ProcessUserOrderFeedAsync\r\nError: Coinbase user-order WebSocket stopped unexpectedly.");
            }
        }

        private async Task HandleUserOrderWebSocketMessageAsync(
            CoinbaseWebSocketParseResult result,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Status is CoinbaseWebSocketParseStatus.Incomplete or
                CoinbaseWebSocketParseStatus.Unknown or
                CoinbaseWebSocketParseStatus.OutOfOrder)
                return;
            if (result.Status == CoinbaseWebSocketParseStatus.ResynchronizationRequired)
            {
                ProcessLogBroadcast?.Invoke(
                    ApplicationName,
                    MessageType.Error,
                    "Method: HandleUserOrderWebSocketMessageAsync\r\nError: Coinbase user-order sequence gap detected; reconnect requested.");
                return;
            }
            if (result.Status == CoinbaseWebSocketParseStatus.Malformed)
            {
                ProcessLogBroadcast?.Invoke(
                    ApplicationName,
                    MessageType.Error,
                    "Method: HandleUserOrderWebSocketMessageAsync\r\nError: Coinbase user-order message was malformed and was ignored.");
                return;
            }
            if (result.Message is not CoinbaseUserOrdersWebSocketMessage message)
                return;

            try
            {
                await ApplyUserOrdersAsync(message);
            }
            catch (System.IO.InvalidDataException)
            {
                ProcessLogBroadcast?.Invoke(
                    ApplicationName,
                    MessageType.Error,
                    "Method: HandleUserOrderWebSocketMessageAsync\r\nError: Coinbase user-order message could not be mapped and was ignored.");
            }
        }

        private async Task ApplyUserOrdersAsync(CoinbaseUserOrdersWebSocketMessage message)
        {
            Orders ??= new List<Order>();
            if (message.Events.Any(value => value.Type == "snapshot"))
                Orders = new List<Order>();
            foreach (CoinbaseUserOrderUpdate update in
                     AdvancedTradeWebSocketMapper.MapUserOrders(message))
            {
                int index = Orders.FindIndex(value =>
                    string.Equals(value.ID, update.Order.ID, StringComparison.Ordinal));
                if (index >= 0)
                    Orders[index] = update.Order;
                else
                    Orders.Add(update.Order);
            }
            if (NotifyOrders != null)
                await NotifyOrders(ApplicationName, Orders);
        }

        private async Task HandleWebSocketMessageAsync(
            CoinbaseWebSocketParseResult result,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Status is CoinbaseWebSocketParseStatus.Incomplete or
                CoinbaseWebSocketParseStatus.Unknown or
                CoinbaseWebSocketParseStatus.OutOfOrder)
                return;
            if (result.Status == CoinbaseWebSocketParseStatus.ResynchronizationRequired)
            {
                InvalidateOrderBooks(result.AffectedProductIds);
                ProcessLogBroadcast?.Invoke(
                    ApplicationName,
                    MessageType.Error,
                    "Method: HandleWebSocketMessageAsync\r\nError: Coinbase WebSocket sequence gap detected; resynchronization requested.");
                return;
            }
            if (result.Status == CoinbaseWebSocketParseStatus.Malformed)
            {
                ProcessLogBroadcast?.Invoke(
                    ApplicationName,
                    MessageType.Error,
                    "Method: HandleWebSocketMessageAsync\r\nError: Coinbase WebSocket message was malformed and was ignored.");
                return;
            }

            try
            {
                switch (result.Message)
                {
                    case CoinbaseTickerWebSocketMessage ticker:
                        await ApplyTickerAsync(ticker);
                        break;
                    case CoinbaseMarketTradesWebSocketMessage trades:
                        await ApplyMarketTradesAsync(trades);
                        break;
                    case CoinbaseLevel2WebSocketMessage level2:
                        await ApplyLevel2Async(level2);
                        break;
                    case CoinbaseHeartbeatWebSocketMessage:
                        break;
                }
            }
            catch (Exception exception) when (exception is FormatException or System.IO.InvalidDataException)
            {
                ProcessLogBroadcast?.Invoke(
                    ApplicationName,
                    MessageType.Error,
                    "Method: HandleWebSocketMessageAsync\r\nError: Coinbase WebSocket message could not be mapped and was ignored.");
            }
        }

        private void InvalidateOrderBooks(IReadOnlyList<string> productIds)
        {
            foreach (string productId in productIds)
            {
                ProductOrderBooks?.Remove(productId);
                if (AssetInformation != null &&
                    AssetInformation.TryGetValue(productId, out AssetInformation asset))
                {
                    asset.BestBid = null;
                    asset.BestAsk = null;
                    asset.BidPriceAndSize = null;
                    asset.AskPriceAndSize = null;
                }
            }
            OrderBook = null;
        }

        private async Task ApplyTickerAsync(CoinbaseTickerWebSocketMessage message)
        {
            Tickers ??= new List<Ticker>();
            CurrentPrices ??= new Dictionary<string, decimal>(StringComparer.Ordinal);
            SubscribedPrices ??= new Dictionary<string, decimal>(StringComparer.Ordinal);
            AssetInformation ??= new Dictionary<string, AssetInformation>(StringComparer.Ordinal);
            foreach (CoinbaseTickerUpdate update in AdvancedTradeWebSocketMapper.MapTicker(message))
            {
                Tickers.RemoveAll(value => value.ProductID == update.Ticker.ProductID);
                Tickers.Add(update.Ticker);
                CurrentPrices[update.Ticker.ProductID] = update.AssetInformation.CurrentPrice;
                SubscribedPrices[update.Ticker.ProductID] = update.AssetInformation.CurrentPrice;
                MergeAssetInformation(update.AssetInformation);
                update.Feed.CurrentPrices = CurrentPrices;
                CurrentFeed = update.Feed;
            }
            if (NotifyCurrentPrices != null)
                await NotifyCurrentPrices(ApplicationName, CurrentPrices);
            if (NotifyAssetInformation != null)
                await NotifyAssetInformation(ApplicationName, AssetInformation);
        }

        private async Task ApplyMarketTradesAsync(CoinbaseMarketTradesWebSocketMessage message)
        {
            CurrentPrices ??= new Dictionary<string, decimal>(StringComparer.Ordinal);
            SubscribedPrices ??= new Dictionary<string, decimal>(StringComparer.Ordinal);
            AssetInformation ??= new Dictionary<string, AssetInformation>(StringComparer.Ordinal);
            MarketTrades ??= new Dictionary<string, CoinbaseMarketTradesSnapshot>(StringComparer.Ordinal);
            IReadOnlyList<CoinbaseTradeUpdate> updates =
                AdvancedTradeWebSocketMapper.MapMarketTrades(message);
            foreach (IGrouping<string, CoinbaseTradeUpdate> group in
                     updates.GroupBy(value => value.Trade.ProductId, StringComparer.Ordinal))
            {
                CoinbaseTradeUpdate latest = group.Last();
                decimal price = decimal.Parse(
                    latest.Trade.Price,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
                CurrentPrices[group.Key] = price;
                SubscribedPrices[group.Key] = price;
                AssetInformation asset = GetAssetInformation(group.Key);
                asset.CurrentPrice = price;
                if (Enum.TryParse(latest.Trade.Side, true, out OrderSide side))
                    asset.OrderSide = side;
                MarketTrades[group.Key] = new CoinbaseMarketTradesSnapshot
                {
                    Trades = group.Select(value => value.Trade).ToArray(),
                    BestBid = asset.BestBid,
                    BestAsk = asset.BestAsk
                };
                latest.Feed.CurrentPrices = CurrentPrices;
                CurrentFeed = latest.Feed;
            }
            if (NotifyCurrentPrices != null)
                await NotifyCurrentPrices(ApplicationName, CurrentPrices);
            if (NotifyAssetInformation != null)
                await NotifyAssetInformation(ApplicationName, AssetInformation);
        }

        private async Task ApplyLevel2Async(CoinbaseLevel2WebSocketMessage message)
        {
            ProductOrderBooks ??= new Dictionary<string, OrderBook>(StringComparer.Ordinal);
            AssetInformation ??= new Dictionary<string, AssetInformation>(StringComparer.Ordinal);
            foreach (CoinbaseLevel2WebSocketEvent level2Event in message.Events)
            {
                ProductOrderBooks.TryGetValue(level2Event.ProductId, out OrderBook existing);
                OrderBook updated = AdvancedTradeWebSocketMapper.ApplyLevel2(
                    existing,
                    level2Event,
                    message.SequenceNumber);
                ProductOrderBooks[level2Event.ProductId] = updated;
                OrderBook = updated;
                AssetInformation asset = GetAssetInformation(level2Event.ProductId);
                asset.BestBid = GetBookValue(updated.Bids, 0);
                asset.BestAsk = GetBookValue(updated.Asks, 0);
                asset.BidPriceAndSize = ToPriceAndSize(updated.Bids);
                asset.AskPriceAndSize = ToPriceAndSize(updated.Asks);
            }
            if (NotifyAssetInformation != null)
                await NotifyAssetInformation(ApplicationName, AssetInformation);
        }

        private void MergeAssetInformation(AssetInformation update)
        {
            AssetInformation current = GetAssetInformation(update.ProductID);
            current.CurrentPrice = update.CurrentPrice;
            current.TwentyFourHourPriceChange = update.TwentyFourHourPriceChange;
            current.TwentyFourHourPricePercentageChange =
                update.TwentyFourHourPricePercentageChange;
            current.BestBid = update.BestBid;
            current.BestAsk = update.BestAsk;
        }

        private AssetInformation GetAssetInformation(string productId)
        {
            if (!AssetInformation.TryGetValue(productId, out AssetInformation asset))
            {
                asset = new AssetInformation { ProductID = productId };
                AssetInformation[productId] = asset;
            }
            return asset;
        }

        private static string GetBookValue(System.Collections.ArrayList[] side, int index)
        {
            if (side == null || side.Length == 0 || side[0] == null || side[0].Count <= index)
                return null;
            return Convert.ToString(side[0][index], CultureInfo.InvariantCulture);
        }

        private static List<PriceAndSize> ToPriceAndSize(System.Collections.ArrayList[] side)
        {
            return (side ?? Array.Empty<System.Collections.ArrayList>())
                .Where(level => level != null && level.Count >= 2)
                .Select(level => new PriceAndSize
                {
                    Price = decimal.Parse(
                        Convert.ToString(level[0], CultureInfo.InvariantCulture),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture),
                    Size = decimal.Parse(
                        Convert.ToString(level[1], CultureInfo.InvariantCulture),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture)
                })
                .ToList();
        }

        private async Task StopFeedCoreAsync()
        {
            CancellationTokenSource cancellation = _feedCancellation;
            Task feedTask = _feedTask;
            Task userOrderFeedTask = _userOrderFeedTask;
            _feedCancellation = null;
            _feedTask = null;
            _userOrderFeedTask = null;
            if (cancellation == null)
                return;
            cancellation.Cancel();
            try
            {
                await Task.WhenAll(new[] { feedTask, userOrderFeedTask }
                    .Where(task => task != null));
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                cancellation.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CoinbaseAdvancedTrade));
        }

        private static Task UnsupportedAsync(string message)
        {
            return Task.FromException(new NotSupportedException(message));
        }

        private static Task<T> UnsupportedAsync<T>(string message)
        {
            return Task.FromException<T>(new NotSupportedException(message));
        }
    }
}
