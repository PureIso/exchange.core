using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using exchange.core.models;

namespace exchange.core.parity
{
    public sealed class ExchangeReadOnlySnapshot
    {
        [JsonPropertyName("products")]
        public List<Product> Products { get; set; } = new List<Product>();

        [JsonPropertyName("account_balances")]
        public Dictionary<string, decimal> AccountBalances { get; set; } =
            new Dictionary<string, decimal>(StringComparer.Ordinal);

        [JsonPropertyName("orders")]
        public List<Order> Orders { get; set; } = new List<Order>();

        [JsonPropertyName("fills")]
        public List<Fill> Fills { get; set; } = new List<Fill>();

        [JsonPropertyName("tickers")]
        public List<Ticker> Tickers { get; set; } = new List<Ticker>();

        [JsonPropertyName("live_assets")]
        public List<AssetInformation> LiveAssets { get; set; } =
            new List<AssetInformation>();
    }

    public sealed class ExchangeParityOptions
    {
        public decimal PriceTolerance { get; init; } = 0.01m;

        public decimal QuantityTolerance { get; init; } = 0.00000001m;

        public decimal LivePriceAbsoluteTolerance { get; init; } = 0.01m;

        public decimal LivePriceRelativeTolerance { get; init; } = 0.0001m;

        public decimal PercentageTolerance { get; init; } = 0.01m;

        public TimeSpan TimestampTolerance { get; init; } = TimeSpan.FromSeconds(2);
    }

    public enum ExchangeParityDisposition
    {
        Mismatch,
        MissingLegacy,
        MissingReplacement,
        Unsupported,
        IntentionallyRemoved
    }

    public sealed class ExchangeParityFinding
    {
        public string Scope { get; init; }

        public string Key { get; init; }

        public string Field { get; init; }

        public ExchangeParityDisposition Disposition { get; init; }

        public string LegacyValue { get; init; }

        public string ReplacementValue { get; init; }

        public string Detail { get; init; }

        [JsonIgnore]
        public bool IsBlocking => Disposition is
            ExchangeParityDisposition.Mismatch or
            ExchangeParityDisposition.MissingLegacy or
            ExchangeParityDisposition.MissingReplacement;
    }

    public sealed class ExchangeParityReport
    {
        public ExchangeParityReport(IEnumerable<ExchangeParityFinding> findings)
        {
            Findings = (findings ?? throw new ArgumentNullException(nameof(findings)))
                .OrderBy(finding => finding.Scope, StringComparer.Ordinal)
                .ThenBy(finding => finding.Key, StringComparer.Ordinal)
                .ThenBy(finding => finding.Field, StringComparer.Ordinal)
                .ThenBy(finding => finding.Disposition)
                .ToArray();
        }

        public IReadOnlyList<ExchangeParityFinding> Findings { get; }

        public bool IsEquivalent => !Findings.Any(finding => finding.IsBlocking);

        public IReadOnlyList<ExchangeParityFinding> BlockingFindings =>
            Findings.Where(finding => finding.IsBlocking).ToArray();
    }
}
