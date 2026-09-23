using System;
using System.Collections.Generic;

namespace exchange.coinbase.advancedtrade
{
    public sealed class CoinbaseAdvancedTradeOptions
    {
        public Uri RestBaseUri { get; init; } = new Uri("https://api.coinbase.com/api/v3/brokerage/");

        public Uri MarketDataWebSocketUri { get; init; } =
            new Uri("wss://advanced-trade-ws.coinbase.com");

        public Uri UserOrderWebSocketUri { get; init; } =
            new Uri("wss://advanced-trade-ws-user.coinbase.com");

        public IReadOnlyList<TimeSpan> WebSocketReconnectDelays { get; init; } =
            new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            };

        public TimeSpan WebSocketReconnectResetAfter { get; init; } = TimeSpan.FromMinutes(1);

        public int WebSocketReceiveBufferSize { get; init; } = 16 * 1024;

        public int WebSocketMaximumMessageSize { get; init; } = 1024 * 1024;

        public IReadOnlyList<string> WebSocketMarketDataChannels { get; init; } =
            new[] { "ticker", "market_trades", "level2" };

        public int ProductPageSize { get; init; } = 100;

        public int AccountPageSize { get; init; } = 250;

        public int OrderPageSize { get; init; } = 100;

        public int FillPageSize { get; init; } = 100;

        public string ApiKeyName { get; init; }

        public string PrivateKeyPem { get; init; }

        internal void Validate()
        {
            if (RestBaseUri == null || !RestBaseUri.IsAbsoluteUri)
                throw new ArgumentException("The Coinbase REST base URI must be absolute.", nameof(RestBaseUri));
            if (RestBaseUri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("The Coinbase REST base URI must use HTTPS.", nameof(RestBaseUri));
            if (ProductPageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(ProductPageSize), "The product page size must be positive.");
            if (AccountPageSize is <= 0 or > 250)
                throw new ArgumentOutOfRangeException(
                    nameof(AccountPageSize),
                    "The account page size must be between 1 and Coinbase's maximum of 250.");
            if (OrderPageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(OrderPageSize), "The order page size must be positive.");
            if (FillPageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(FillPageSize), "The fill page size must be positive.");
        }

        internal void ValidateAuthentication()
        {
            if (string.IsNullOrWhiteSpace(ApiKeyName))
                throw new ArgumentException("The Coinbase API key name is required.", nameof(ApiKeyName));
            if (string.IsNullOrWhiteSpace(PrivateKeyPem))
                throw new ArgumentException("The Coinbase private key is required.", nameof(PrivateKeyPem));
        }

        internal void ValidateWebSocket()
        {
            if (MarketDataWebSocketUri == null || !MarketDataWebSocketUri.IsAbsoluteUri)
                throw new ArgumentException(
                    "The Coinbase market-data WebSocket URI must be absolute.",
                    nameof(MarketDataWebSocketUri));
            if (MarketDataWebSocketUri.Scheme != "wss")
                throw new ArgumentException(
                    "The Coinbase market-data WebSocket URI must use WSS.",
                    nameof(MarketDataWebSocketUri));
            if (WebSocketReconnectDelays == null || WebSocketReconnectDelays.Count == 0)
                throw new ArgumentException(
                    "At least one WebSocket reconnect delay is required.",
                    nameof(WebSocketReconnectDelays));
            foreach (TimeSpan delay in WebSocketReconnectDelays)
                if (delay < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(
                        nameof(WebSocketReconnectDelays),
                        "WebSocket reconnect delays cannot be negative.");
            if (WebSocketReconnectResetAfter <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(
                    nameof(WebSocketReconnectResetAfter),
                    "The WebSocket reconnect reset interval must be positive.");
            if (WebSocketReceiveBufferSize <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(WebSocketReceiveBufferSize),
                    "The WebSocket receive buffer size must be positive.");
            if (WebSocketMaximumMessageSize < WebSocketReceiveBufferSize)
                throw new ArgumentOutOfRangeException(
                    nameof(WebSocketMaximumMessageSize),
                    "The WebSocket maximum message size cannot be smaller than the receive buffer.");
            if (WebSocketMarketDataChannels == null || WebSocketMarketDataChannels.Count == 0)
                throw new ArgumentException(
                    "At least one Coinbase market-data channel is required.",
                    nameof(WebSocketMarketDataChannels));
            HashSet<string> channels = new HashSet<string>(StringComparer.Ordinal);
            foreach (string channel in WebSocketMarketDataChannels)
                if (channel is not ("ticker" or "market_trades" or "level2") ||
                    !channels.Add(channel))
                    throw new ArgumentException(
                        "Coinbase market-data channels must be unique supported public channels.",
                        nameof(WebSocketMarketDataChannels));
        }

        internal void ValidateUserOrderWebSocket()
        {
            if (UserOrderWebSocketUri == null || !UserOrderWebSocketUri.IsAbsoluteUri)
                throw new ArgumentException(
                    "The Coinbase user-order WebSocket URI must be absolute.",
                    nameof(UserOrderWebSocketUri));
            if (UserOrderWebSocketUri.Scheme != "wss")
                throw new ArgumentException(
                    "The Coinbase user-order WebSocket URI must use WSS.",
                    nameof(UserOrderWebSocketUri));
            if (WebSocketReconnectDelays == null || WebSocketReconnectDelays.Count == 0)
                throw new ArgumentException(
                    "At least one WebSocket reconnect delay is required.",
                    nameof(WebSocketReconnectDelays));
            foreach (TimeSpan delay in WebSocketReconnectDelays)
                if (delay < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(
                        nameof(WebSocketReconnectDelays),
                        "WebSocket reconnect delays cannot be negative.");
            if (WebSocketReconnectResetAfter <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(
                    nameof(WebSocketReconnectResetAfter),
                    "The WebSocket reconnect reset interval must be positive.");
            if (WebSocketReceiveBufferSize <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(WebSocketReceiveBufferSize),
                    "The WebSocket receive buffer size must be positive.");
            if (WebSocketMaximumMessageSize < WebSocketReceiveBufferSize)
                throw new ArgumentOutOfRangeException(
                    nameof(WebSocketMaximumMessageSize),
                    "The WebSocket maximum message size cannot be smaller than the receive buffer.");
        }
    }
}
