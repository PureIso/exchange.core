using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using exchange.coinbase.advancedtrade.websockets;
using exchange.core.models;

namespace exchange.coinbase.advancedtrade
{
    public static class AdvancedTradeWebSocketMapper
    {
        public static IReadOnlyList<CoinbaseTickerUpdate> MapTicker(
            CoinbaseTickerWebSocketMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            return message.Events.Select(value =>
            {
                decimal price = ParseDecimal(value.Price, "ticker price");
                decimal percentage = ParseOptionalDecimal(value.PricePercentageChange24Hour);
                decimal open = percentage == -100
                    ? price
                    : price / (1 + (percentage / 100));
                return new CoinbaseTickerUpdate
                {
                    Ticker = new Ticker
                    {
                        ProductID = value.ProductId,
                        Price = value.Price,
                        Bid = value.BestBid,
                        Ask = value.BestAsk,
                        Volume = value.Volume24Hour,
                        Time = message.Timestamp
                    },
                    Feed = new Feed
                    {
                        Type = value.Type,
                        Sequence = message.SequenceNumber,
                        Time = message.Timestamp,
                        ProductID = value.ProductId,
                        Price = value.Price,
                        BestBid = value.BestBid,
                        BestAsk = value.BestAsk
                    },
                    AssetInformation = new AssetInformation
                    {
                        ProductID = value.ProductId,
                        CurrentPrice = price,
                        TwentyFourHourPriceChange = price - open,
                        TwentyFourHourPricePercentageChange = percentage,
                        BestBid = value.BestBid,
                        BestAsk = value.BestAsk
                    }
                };
            }).ToArray();
        }

        public static IReadOnlyList<CoinbaseTradeUpdate> MapMarketTrades(
            CoinbaseMarketTradesWebSocketMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            return message.Events.Select(value =>
            {
                ParseDecimal(value.Price, "market-trade price");
                ParseDecimal(value.Size, "market-trade size");
                if (!DateTimeOffset.TryParse(
                        value.Time,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal,
                        out DateTimeOffset time))
                    throw new InvalidDataException("The Coinbase market-trade time was invalid.");
                return new CoinbaseTradeUpdate
                {
                    Trade = new CoinbaseMarketTrade
                    {
                        TradeId = value.TradeId,
                        ProductId = value.ProductId,
                        Price = value.Price,
                        Size = value.Size,
                        Side = value.Side,
                        Time = time,
                        Exchange = "Coinbase Advanced Trade"
                    },
                    Feed = new Feed
                    {
                        Type = value.Type,
                        Sequence = message.SequenceNumber,
                        Time = value.Time,
                        ProductID = value.ProductId,
                        Price = value.Price,
                        Side = value.Side,
                        LastSize = value.Size
                    }
                };
            }).ToArray();
        }

        public static OrderBook ApplyLevel2(
            OrderBook existing,
            CoinbaseLevel2WebSocketEvent level2Event,
            long sequenceNumber)
        {
            if (level2Event == null)
                throw new ArgumentNullException(nameof(level2Event));
            Dictionary<decimal, string> bids = level2Event.Type == "snapshot"
                ? new Dictionary<decimal, string>()
                : ReadSide(existing?.Bids);
            Dictionary<decimal, string> asks = level2Event.Type == "snapshot"
                ? new Dictionary<decimal, string>()
                : ReadSide(existing?.Asks);

            foreach (CoinbaseLevel2WebSocketUpdate update in level2Event.Updates)
            {
                decimal price = ParseDecimal(update.PriceLevel, "level2 price");
                decimal quantity = ParseDecimal(update.NewQuantity, "level2 quantity");
                Dictionary<decimal, string> side = update.Side switch
                {
                    "bid" => bids,
                    "offer" or "ask" => asks,
                    _ => throw new InvalidDataException("The Coinbase level2 side was invalid.")
                };
                if (quantity == 0)
                    side.Remove(price);
                else
                    side[price] = update.NewQuantity;
            }

            return new OrderBook
            {
                Sequence = sequenceNumber,
                Bids = ToCoreSide(bids.OrderByDescending(value => value.Key)),
                Asks = ToCoreSide(asks.OrderBy(value => value.Key))
            };
        }

        public static IReadOnlyList<CoinbaseUserOrderUpdate> MapUserOrders(
            CoinbaseUserOrdersWebSocketMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            return message.Events.SelectMany(currentEvent =>
                currentEvent.Orders.Select(source => new CoinbaseUserOrderUpdate
                {
                    EventType = currentEvent.Type,
                    Order = MapUserOrder(source)
                })).ToArray();
        }

        private static Order MapUserOrder(CoinbaseUserOrderWebSocketUpdate source)
        {
            if (string.IsNullOrWhiteSpace(source.OrderId) ||
                string.IsNullOrWhiteSpace(source.ProductId) ||
                string.IsNullOrWhiteSpace(source.Status))
                throw new InvalidDataException(
                    "The Coinbase user-order update was incomplete.");

            decimal filled = ParseOptionalStrictDecimal(
                source.CumulativeQuantity,
                "user-order cumulative quantity");
            decimal leaves = ParseOptionalStrictDecimal(
                source.LeavesQuantity,
                "user-order leaves quantity");
            ParseOptionalStrictDecimal(source.FilledValue, "user-order filled value");
            ParseOptionalStrictDecimal(source.TotalFees, "user-order total fees");
            ParseOptionalStrictDecimal(source.LimitPrice, "user-order limit price");
            ParseOptionalStrictDecimal(source.AveragePrice, "user-order average price");
            decimal total = filled + leaves;
            string price = FirstNonZero(source.LimitPrice, source.AveragePrice);
            string status = source.Status.ToLowerInvariant();

            return new Order
            {
                ID = source.OrderId,
                ProductID = source.ProductId,
                Side = source.OrderSide?.ToLowerInvariant(),
                Type = source.OrderType?.ToLowerInvariant(),
                Size = total.ToString(CultureInfo.InvariantCulture),
                Price = price,
                PostOnly = source.PostOnly,
                TimeInForce = source.TimeInForce?.ToLowerInvariant(),
                CreatedAt = source.CreationTime,
                FillFees = source.TotalFees,
                FilledSize = source.CumulativeQuantity,
                ExecutedValue = source.FilledValue,
                Status = status,
                Settled = status is "filled" or "cancelled" or "expired" or "failed",
                Quantity = total
            };
        }

        private static Dictionary<decimal, string> ReadSide(ArrayList[] values)
        {
            Dictionary<decimal, string> result = new Dictionary<decimal, string>();
            foreach (ArrayList level in values ?? Array.Empty<ArrayList>())
            {
                if (level == null || level.Count < 2)
                    continue;
                string priceText = Convert.ToString(level[0], CultureInfo.InvariantCulture);
                string quantityText = Convert.ToString(level[1], CultureInfo.InvariantCulture);
                result[ParseDecimal(priceText, "order-book price")] = quantityText;
            }
            return result;
        }

        private static ArrayList[] ToCoreSide(
            IEnumerable<KeyValuePair<decimal, string>> values)
        {
            return values.Select(value => new ArrayList
            {
                value.Key.ToString(CultureInfo.InvariantCulture),
                value.Value
            }).ToArray();
        }

        private static decimal ParseDecimal(string value, string fieldName)
        {
            if (!decimal.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out decimal result))
                throw new InvalidDataException($"The Coinbase {fieldName} was invalid.");
            return result;
        }

        private static decimal ParseOptionalDecimal(string value)
        {
            return decimal.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out decimal result)
                ? result
                : 0;
        }

        private static decimal ParseOptionalStrictDecimal(string value, string fieldName)
        {
            return string.IsNullOrWhiteSpace(value) ? 0 : ParseDecimal(value, fieldName);
        }

        private static string FirstNonZero(params string[] values)
        {
            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                if (decimal.TryParse(
                        value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out decimal number) && number != 0)
                    return value;
            }
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }
    }

    public sealed class CoinbaseTickerUpdate
    {
        public Ticker Ticker { get; init; }
        public Feed Feed { get; init; }
        public AssetInformation AssetInformation { get; init; }
    }

    public sealed class CoinbaseTradeUpdate
    {
        public CoinbaseMarketTrade Trade { get; init; }
        public Feed Feed { get; init; }
    }

    public sealed class CoinbaseUserOrderUpdate
    {
        public string EventType { get; init; }
        public Order Order { get; init; }
    }
}
