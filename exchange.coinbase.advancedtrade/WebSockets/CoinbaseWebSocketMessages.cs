using System.Collections.Generic;

namespace exchange.coinbase.advancedtrade.websockets
{
    public abstract class CoinbaseWebSocketMessage
    {
        protected CoinbaseWebSocketMessage(
            string channel,
            string timestamp,
            long sequenceNumber)
        {
            Channel = channel;
            Timestamp = timestamp;
            SequenceNumber = sequenceNumber;
        }

        public string Channel { get; }

        public string Timestamp { get; }

        public long SequenceNumber { get; }
    }

    public sealed class CoinbaseTickerWebSocketMessage : CoinbaseWebSocketMessage
    {
        public CoinbaseTickerWebSocketMessage(
            string channel,
            string timestamp,
            long sequenceNumber,
            IReadOnlyList<CoinbaseTickerWebSocketEvent> events)
            : base(channel, timestamp, sequenceNumber)
        {
            Events = events;
        }

        public IReadOnlyList<CoinbaseTickerWebSocketEvent> Events { get; }
    }

    public sealed class CoinbaseTickerWebSocketEvent
    {
        public string EventType { get; init; }
        public string Type { get; init; }
        public string ProductId { get; init; }
        public string Price { get; init; }
        public string Volume24Hour { get; init; }
        public string Low24Hour { get; init; }
        public string High24Hour { get; init; }
        public string PricePercentageChange24Hour { get; init; }
        public string BestBid { get; init; }
        public string BestBidQuantity { get; init; }
        public string BestAsk { get; init; }
        public string BestAskQuantity { get; init; }
    }

    public sealed class CoinbaseMarketTradesWebSocketMessage : CoinbaseWebSocketMessage
    {
        public CoinbaseMarketTradesWebSocketMessage(
            string channel,
            string timestamp,
            long sequenceNumber,
            IReadOnlyList<CoinbaseMarketTradeWebSocketEvent> events)
            : base(channel, timestamp, sequenceNumber)
        {
            Events = events;
        }

        public IReadOnlyList<CoinbaseMarketTradeWebSocketEvent> Events { get; }
    }

    public sealed class CoinbaseMarketTradeWebSocketEvent
    {
        public string Type { get; init; }
        public string TradeId { get; init; }
        public string ProductId { get; init; }
        public string Price { get; init; }
        public string Size { get; init; }
        public string Side { get; init; }
        public string Time { get; init; }
    }

    public sealed class CoinbaseLevel2WebSocketMessage : CoinbaseWebSocketMessage
    {
        public CoinbaseLevel2WebSocketMessage(
            string channel,
            string timestamp,
            long sequenceNumber,
            IReadOnlyList<CoinbaseLevel2WebSocketEvent> events)
            : base(channel, timestamp, sequenceNumber)
        {
            Events = events;
        }

        public IReadOnlyList<CoinbaseLevel2WebSocketEvent> Events { get; }
    }

    public sealed class CoinbaseLevel2WebSocketEvent
    {
        public string Type { get; init; }
        public string ProductId { get; init; }
        public IReadOnlyList<CoinbaseLevel2WebSocketUpdate> Updates { get; init; }
    }

    public sealed class CoinbaseLevel2WebSocketUpdate
    {
        public string Side { get; init; }
        public string EventTime { get; init; }
        public string PriceLevel { get; init; }
        public string NewQuantity { get; init; }
    }

    public sealed class CoinbaseHeartbeatWebSocketMessage : CoinbaseWebSocketMessage
    {
        public CoinbaseHeartbeatWebSocketMessage(
            string channel,
            string timestamp,
            long sequenceNumber,
            IReadOnlyList<CoinbaseHeartbeatWebSocketEvent> events)
            : base(channel, timestamp, sequenceNumber)
        {
            Events = events;
        }

        public IReadOnlyList<CoinbaseHeartbeatWebSocketEvent> Events { get; }
    }

    public sealed class CoinbaseHeartbeatWebSocketEvent
    {
        public string CurrentTime { get; init; }
        public string HeartbeatCounter { get; init; }
    }

    public sealed class CoinbaseUserOrdersWebSocketMessage : CoinbaseWebSocketMessage
    {
        public CoinbaseUserOrdersWebSocketMessage(
            string channel,
            string timestamp,
            long sequenceNumber,
            IReadOnlyList<CoinbaseUserOrdersWebSocketEvent> events)
            : base(channel, timestamp, sequenceNumber)
        {
            Events = events;
        }

        public IReadOnlyList<CoinbaseUserOrdersWebSocketEvent> Events { get; }
    }

    public sealed class CoinbaseUserOrdersWebSocketEvent
    {
        public string Type { get; init; }
        public IReadOnlyList<CoinbaseUserOrderWebSocketUpdate> Orders { get; init; }
    }

    public sealed class CoinbaseUserOrderWebSocketUpdate
    {
        public string AveragePrice { get; init; }
        public string ClientOrderId { get; init; }
        public string CumulativeQuantity { get; init; }
        public string FilledValue { get; init; }
        public string LeavesQuantity { get; init; }
        public string LimitPrice { get; init; }
        public string OrderId { get; init; }
        public string OrderSide { get; init; }
        public string OrderType { get; init; }
        public bool PostOnly { get; init; }
        public string ProductId { get; init; }
        public string Status { get; init; }
        public string TimeInForce { get; init; }
        public string TotalFees { get; init; }
        public string CreationTime { get; init; }
    }
}
