using System;
using System.Collections.Generic;
using System.Text.Json;

namespace exchange.coinbase.advancedtrade.websockets
{
    public sealed class CoinbaseWebSocketMessageParser
    {
        public CoinbaseWebSocketParseResult Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return CoinbaseWebSocketParseResult.Malformed("The Coinbase WebSocket message was empty.");

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return CoinbaseWebSocketParseResult.Malformed(
                        "The Coinbase WebSocket message root must be an object.");

                if (!TryGetRequiredString(root, "channel", out string channel))
                {
                    if (TryGetRequiredString(root, "type", out _))
                        return CoinbaseWebSocketParseResult.Unknown(null);
                    return CoinbaseWebSocketParseResult.Malformed(
                        "The Coinbase WebSocket message did not contain a channel.");
                }
                if (!TryGetRequiredString(root, "timestamp", out string timestamp) ||
                    !root.TryGetProperty("sequence_num", out JsonElement sequenceElement) ||
                    !sequenceElement.TryGetInt64(out long sequenceNumber) ||
                    sequenceNumber < 0 ||
                    !root.TryGetProperty("events", out JsonElement events) ||
                    events.ValueKind != JsonValueKind.Array)
                {
                    return CoinbaseWebSocketParseResult.Malformed(
                        "The Coinbase WebSocket envelope was incomplete.");
                }

                return channel switch
                {
                    "ticker" => ParseTicker(channel, timestamp, sequenceNumber, events),
                    "market_trades" => ParseMarketTrades(channel, timestamp, sequenceNumber, events),
                    "l2_data" or "level2" => ParseLevel2(channel, timestamp, sequenceNumber, events),
                    "heartbeats" => ParseHeartbeats(channel, timestamp, sequenceNumber, events),
                    "user" => ParseUserOrders(channel, timestamp, sequenceNumber, events),
                    _ => CoinbaseWebSocketParseResult.Unknown(channel)
                };
            }
            catch (JsonException)
            {
                return CoinbaseWebSocketParseResult.Malformed(
                    "The Coinbase WebSocket message was not valid JSON.");
            }
        }

        private static CoinbaseWebSocketParseResult ParseTicker(
            string channel,
            string timestamp,
            long sequenceNumber,
            JsonElement events)
        {
            List<CoinbaseTickerWebSocketEvent> parsed = new List<CoinbaseTickerWebSocketEvent>();
            bool supportedEventFound = false;
            foreach (JsonElement currentEvent in events.EnumerateArray())
            {
                if (!IsSupportedMarketEvent(currentEvent, out string eventType))
                    continue;
                supportedEventFound = true;
                if (!currentEvent.TryGetProperty("tickers", out JsonElement tickers) ||
                    tickers.ValueKind != JsonValueKind.Array)
                    return MalformedChannel(channel);
                foreach (JsonElement ticker in tickers.EnumerateArray())
                {
                    if (!TryGetRequiredString(ticker, "product_id", out string productId) ||
                        !TryGetRequiredString(ticker, "price", out string price))
                        return MalformedChannel(channel);
                    parsed.Add(new CoinbaseTickerWebSocketEvent
                    {
                        EventType = eventType,
                        Type = GetOptionalString(ticker, "type"),
                        ProductId = productId,
                        Price = price,
                        Volume24Hour = GetOptionalString(ticker, "volume_24_h"),
                        Low24Hour = GetOptionalString(ticker, "low_24_h"),
                        High24Hour = GetOptionalString(ticker, "high_24_h"),
                        PricePercentageChange24Hour = GetOptionalString(
                            ticker,
                            "price_percent_chg_24_h"),
                        BestBid = GetOptionalString(ticker, "best_bid"),
                        BestBidQuantity = GetOptionalString(ticker, "best_bid_quantity"),
                        BestAsk = GetOptionalString(ticker, "best_ask"),
                        BestAskQuantity = GetOptionalString(ticker, "best_ask_quantity")
                    });
                }
            }
            return supportedEventFound
                ? CoinbaseWebSocketParseResult.Parsed(
                    new CoinbaseTickerWebSocketMessage(channel, timestamp, sequenceNumber, parsed))
                : CoinbaseWebSocketParseResult.Unknown(channel);
        }

        private static CoinbaseWebSocketParseResult ParseMarketTrades(
            string channel,
            string timestamp,
            long sequenceNumber,
            JsonElement events)
        {
            List<CoinbaseMarketTradeWebSocketEvent> parsed =
                new List<CoinbaseMarketTradeWebSocketEvent>();
            bool supportedEventFound = false;
            foreach (JsonElement currentEvent in events.EnumerateArray())
            {
                if (!IsSupportedMarketEvent(currentEvent, out string eventType))
                    continue;
                supportedEventFound = true;
                if (!currentEvent.TryGetProperty("trades", out JsonElement trades) ||
                    trades.ValueKind != JsonValueKind.Array)
                    return MalformedChannel(channel);
                foreach (JsonElement trade in trades.EnumerateArray())
                {
                    if (!TryGetRequiredString(trade, "trade_id", out string tradeId) ||
                        !TryGetRequiredString(trade, "product_id", out string productId) ||
                        !TryGetRequiredString(trade, "price", out string price) ||
                        !TryGetRequiredString(trade, "size", out string size))
                        return MalformedChannel(channel);
                    parsed.Add(new CoinbaseMarketTradeWebSocketEvent
                    {
                        Type = eventType,
                        TradeId = tradeId,
                        ProductId = productId,
                        Price = price,
                        Size = size,
                        Side = GetOptionalString(trade, "side"),
                        Time = GetOptionalString(trade, "time")
                    });
                }
            }
            return supportedEventFound
                ? CoinbaseWebSocketParseResult.Parsed(
                    new CoinbaseMarketTradesWebSocketMessage(
                        channel,
                        timestamp,
                        sequenceNumber,
                        parsed))
                : CoinbaseWebSocketParseResult.Unknown(channel);
        }

        private static CoinbaseWebSocketParseResult ParseLevel2(
            string channel,
            string timestamp,
            long sequenceNumber,
            JsonElement events)
        {
            List<CoinbaseLevel2WebSocketEvent> parsed = new List<CoinbaseLevel2WebSocketEvent>();
            bool supportedEventFound = false;
            foreach (JsonElement currentEvent in events.EnumerateArray())
            {
                if (!IsSupportedMarketEvent(currentEvent, out string eventType))
                    continue;
                supportedEventFound = true;
                if (!TryGetRequiredString(currentEvent, "product_id", out string productId) ||
                    !currentEvent.TryGetProperty("updates", out JsonElement updates) ||
                    updates.ValueKind != JsonValueKind.Array)
                    return MalformedChannel(channel);
                List<CoinbaseLevel2WebSocketUpdate> parsedUpdates =
                    new List<CoinbaseLevel2WebSocketUpdate>();
                foreach (JsonElement update in updates.EnumerateArray())
                {
                    if (!TryGetRequiredString(update, "side", out string side) ||
                        !TryGetRequiredString(update, "price_level", out string priceLevel) ||
                        !TryGetRequiredString(update, "new_quantity", out string newQuantity))
                        return MalformedChannel(channel);
                    parsedUpdates.Add(new CoinbaseLevel2WebSocketUpdate
                    {
                        Side = side,
                        EventTime = GetOptionalString(update, "event_time"),
                        PriceLevel = priceLevel,
                        NewQuantity = newQuantity
                    });
                }
                parsed.Add(new CoinbaseLevel2WebSocketEvent
                {
                    Type = eventType,
                    ProductId = productId,
                    Updates = parsedUpdates
                });
            }
            return supportedEventFound
                ? CoinbaseWebSocketParseResult.Parsed(
                    new CoinbaseLevel2WebSocketMessage(channel, timestamp, sequenceNumber, parsed))
                : CoinbaseWebSocketParseResult.Unknown(channel);
        }

        private static CoinbaseWebSocketParseResult ParseHeartbeats(
            string channel,
            string timestamp,
            long sequenceNumber,
            JsonElement events)
        {
            List<CoinbaseHeartbeatWebSocketEvent> parsed =
                new List<CoinbaseHeartbeatWebSocketEvent>();
            foreach (JsonElement currentEvent in events.EnumerateArray())
            {
                string counter = GetScalarString(currentEvent, "heartbeat_counter");
                if (string.IsNullOrWhiteSpace(counter))
                    return MalformedChannel(channel);
                parsed.Add(new CoinbaseHeartbeatWebSocketEvent
                {
                    CurrentTime = GetOptionalString(currentEvent, "current_time"),
                    HeartbeatCounter = counter
                });
            }
            return CoinbaseWebSocketParseResult.Parsed(
                new CoinbaseHeartbeatWebSocketMessage(channel, timestamp, sequenceNumber, parsed));
        }

        private static CoinbaseWebSocketParseResult ParseUserOrders(
            string channel,
            string timestamp,
            long sequenceNumber,
            JsonElement events)
        {
            List<CoinbaseUserOrdersWebSocketEvent> parsed =
                new List<CoinbaseUserOrdersWebSocketEvent>();
            bool supportedEventFound = false;
            foreach (JsonElement currentEvent in events.EnumerateArray())
            {
                string eventType = GetOptionalString(currentEvent, "type");
                if (eventType is not ("snapshot" or "update" or "patch"))
                    continue;
                supportedEventFound = true;
                if (!currentEvent.TryGetProperty("orders", out JsonElement orders) ||
                    orders.ValueKind != JsonValueKind.Array)
                    return MalformedChannel(channel);
                List<CoinbaseUserOrderWebSocketUpdate> parsedOrders =
                    new List<CoinbaseUserOrderWebSocketUpdate>();
                foreach (JsonElement order in orders.EnumerateArray())
                {
                    if (!TryGetRequiredString(order, "order_id", out string orderId) ||
                        !TryGetRequiredString(order, "product_id", out string productId) ||
                        !TryGetRequiredString(order, "status", out string status) ||
                        !TryGetBoolean(order, "post_only", out bool postOnly))
                        return MalformedChannel(channel);
                    parsedOrders.Add(new CoinbaseUserOrderWebSocketUpdate
                    {
                        AveragePrice = GetOptionalString(order, "avg_price"),
                        ClientOrderId = GetOptionalString(order, "client_order_id"),
                        CumulativeQuantity = GetOptionalString(order, "cumulative_quantity"),
                        FilledValue = GetOptionalString(order, "filled_value"),
                        LeavesQuantity = GetOptionalString(order, "leaves_quantity"),
                        LimitPrice = GetOptionalString(order, "limit_price"),
                        OrderId = orderId,
                        OrderSide = GetOptionalString(order, "order_side"),
                        OrderType = GetOptionalString(order, "order_type"),
                        PostOnly = postOnly,
                        ProductId = productId,
                        Status = status,
                        TimeInForce = GetOptionalString(order, "time_in_force"),
                        TotalFees = GetOptionalString(order, "total_fees"),
                        CreationTime = GetOptionalString(order, "creation_time")
                    });
                }
                parsed.Add(new CoinbaseUserOrdersWebSocketEvent
                {
                    Type = eventType,
                    Orders = parsedOrders
                });
            }
            return supportedEventFound
                ? CoinbaseWebSocketParseResult.Parsed(
                    new CoinbaseUserOrdersWebSocketMessage(
                        channel,
                        timestamp,
                        sequenceNumber,
                        parsed))
                : CoinbaseWebSocketParseResult.Unknown(channel);
        }

        private static bool IsSupportedMarketEvent(JsonElement currentEvent, out string eventType)
        {
            eventType = GetOptionalString(currentEvent, "type");
            return eventType == "snapshot" || eventType == "update";
        }

        private static CoinbaseWebSocketParseResult MalformedChannel(string channel)
        {
            return CoinbaseWebSocketParseResult.Malformed(
                $"The Coinbase {channel} WebSocket event was incomplete.");
        }

        private static bool TryGetRequiredString(
            JsonElement element,
            string propertyName,
            out string value)
        {
            value = GetOptionalString(element, propertyName);
            return !string.IsNullOrWhiteSpace(value);
        }

        private static string GetOptionalString(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propertyName, out JsonElement value) ||
                value.ValueKind != JsonValueKind.String)
                return null;
            return value.GetString();
        }

        private static string GetScalarString(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propertyName, out JsonElement value))
                return null;
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            };
        }

        private static bool TryGetBoolean(
            JsonElement element,
            string propertyName,
            out bool value)
        {
            value = false;
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propertyName, out JsonElement property))
                return true;
            if (property.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                value = property.GetBoolean();
                return true;
            }
            return property.ValueKind == JsonValueKind.String &&
                bool.TryParse(property.GetString(), out value);
        }
    }
}
