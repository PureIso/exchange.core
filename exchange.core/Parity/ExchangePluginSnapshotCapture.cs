using System;
using System.Collections.Generic;
using System.Linq;
using exchange.core.implementations;
using exchange.core.models;

namespace exchange.core.parity
{
    public static class ExchangePluginSnapshotCapture
    {
        public static ExchangeReadOnlySnapshot Capture(AbstractExchangePlugin plugin)
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));

            return new ExchangeReadOnlySnapshot
            {
                Products = (plugin.Products ?? new List<Product>())
                    .Select(Clone).ToList(),
                AccountBalances = new Dictionary<string, decimal>(
                    plugin.AccountInfo ?? new Dictionary<string, decimal>(),
                    StringComparer.Ordinal),
                Orders = (plugin.Orders ?? new List<Order>())
                    .Select(Clone).ToList(),
                Fills = (plugin.Fills ?? new List<Fill>())
                    .Select(Clone).ToList(),
                Tickers = (plugin.Tickers ?? new List<Ticker>())
                    .Select(Clone).ToList(),
                LiveAssets = (plugin.AssetInformation ??
                        new Dictionary<string, AssetInformation>())
                    .Values.Select(Clone).ToList()
            };
        }

        private static Product Clone(Product source)
        {
            return new Product
            {
                ID = source.ID,
                BaseCurrency = source.BaseCurrency,
                QuoteCurrency = source.QuoteCurrency,
                BaseMinSize = source.BaseMinSize,
                BaseMaxSize = source.BaseMaxSize,
                QuoteIncrement = source.QuoteIncrement
            };
        }

        private static Order Clone(Order source)
        {
            return new Order
            {
                ID = source.ID,
                Size = source.Size,
                ProductID = source.ProductID,
                Side = source.Side,
                SelfTradePreventionType = source.SelfTradePreventionType,
                Fund = source.Fund,
                Type = source.Type,
                PostOnly = source.PostOnly,
                Price = source.Price,
                TimeInForce = source.TimeInForce,
                CreatedAt = source.CreatedAt,
                FillFees = source.FillFees,
                FilledSize = source.FilledSize,
                ExecutedValue = source.ExecutedValue,
                Status = source.Status,
                Settled = source.Settled,
                Quantity = source.Quantity
            };
        }

        private static Fill Clone(Fill source)
        {
            return new Fill
            {
                TradeID = source.TradeID,
                ExternalTradeID = source.ExternalTradeID,
                ProductID = source.ProductID,
                Price = source.Price,
                Size = source.Size,
                OrderID = source.OrderID,
                Side = source.Side,
                Fee = source.Fee,
                Settled = source.Settled,
                Time = source.Time
            };
        }

        private static Ticker Clone(Ticker source)
        {
            return new Ticker
            {
                TradeID = source.TradeID,
                ProductID = source.ProductID,
                Size = source.Size,
                Price = source.Price,
                Bid = source.Bid,
                Ask = source.Ask,
                Volume = source.Volume,
                Time = source.Time
            };
        }

        private static AssetInformation Clone(AssetInformation source)
        {
            return new AssetInformation
            {
                ProductID = source.ProductID,
                CurrentPrice = source.CurrentPrice,
                BestBid = source.BestBid,
                BestAsk = source.BestAsk,
                TwentyFourHourPriceChange = source.TwentyFourHourPriceChange,
                TwentyFourHourPricePercentageChange =
                    source.TwentyFourHourPricePercentageChange
            };
        }
    }
}
