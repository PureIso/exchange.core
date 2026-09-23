using System;
using System.Collections.Generic;
using System.Linq;

namespace exchange.core.parity
{
    public sealed class ExchangeParitySoakCriteria
    {
        public int RequiredSamples { get; init; } = 60;

        public TimeSpan RequiredDuration { get; init; } = TimeSpan.FromHours(1);

        public int MaximumBlockingSamples { get; init; }

        public int MaximumConsecutiveBlockingSamples { get; init; }

        public int MaximumCollectionFailures { get; init; }

        internal void Validate()
        {
            if (RequiredSamples <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(RequiredSamples),
                    "At least one soak sample is required.");
            if (RequiredDuration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(
                    nameof(RequiredDuration),
                    "Required soak duration cannot be negative.");
            if (MaximumBlockingSamples < 0 ||
                MaximumConsecutiveBlockingSamples < 0 ||
                MaximumCollectionFailures < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(MaximumBlockingSamples),
                    "Soak failure thresholds cannot be negative.");
        }
    }

    public sealed class ExchangeParitySoakObservation
    {
        public DateTimeOffset Timestamp { get; init; }

        public int BlockingFindingCount { get; init; }

        public bool CollectionFailed { get; init; }

        public ExchangeParityCollectionFailureCategory? FailureCategory { get; init; }
    }

    public enum ExchangeParityCollectionFailureCategory
    {
        LegacyCapture,
        ReplacementCapture,
        Comparison,
        ObservationTimeout
    }

    public sealed class ExchangeParitySoakReport
    {
        public int ObservationCount { get; init; }

        public int SampleCount { get; init; }

        public TimeSpan ObservedDuration { get; init; }

        public int BlockingSampleCount { get; init; }

        public int MaximumConsecutiveBlockingSamples { get; init; }

        public int CollectionFailureCount { get; init; }

        public IReadOnlyList<string> RollbackReasons { get; init; }

        public bool Passed => RollbackReasons.Count == 0;
    }

    public sealed class ExchangeParitySoakSession
    {
        private readonly ExchangeReadOnlyParityComparator _comparator;
        private readonly List<ExchangeParitySoakObservation> _observations =
            new List<ExchangeParitySoakObservation>();

        public ExchangeParitySoakSession(
            ExchangeReadOnlyParityComparator comparator = null)
        {
            _comparator = comparator ?? new ExchangeReadOnlyParityComparator();
        }

        public IReadOnlyList<ExchangeParitySoakObservation> Observations =>
            _observations.ToArray();

        public ExchangeParitySoakObservation Observe(
            DateTimeOffset timestamp,
            ExchangeReadOnlySnapshot legacy,
            ExchangeReadOnlySnapshot replacement)
        {
            ExchangeParityReport comparison = _comparator.Compare(legacy, replacement);
            ExchangeParitySoakObservation observation =
                new ExchangeParitySoakObservation
                {
                    Timestamp = timestamp,
                    BlockingFindingCount = comparison.BlockingFindings.Count
                };
            _observations.Add(observation);
            return observation;
        }

        public ExchangeParitySoakObservation RecordCollectionFailure(
            DateTimeOffset timestamp,
            ExchangeParityCollectionFailureCategory failureCategory)
        {
            ExchangeParitySoakObservation observation =
                new ExchangeParitySoakObservation
                {
                    Timestamp = timestamp,
                    CollectionFailed = true,
                    FailureCategory = failureCategory
                };
            _observations.Add(observation);
            return observation;
        }
    }

    public sealed class ExchangeParitySoakEvaluator
    {
        private readonly ExchangeParitySoakCriteria _criteria;

        public ExchangeParitySoakEvaluator(ExchangeParitySoakCriteria criteria)
        {
            _criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
            _criteria.Validate();
        }

        public ExchangeParitySoakReport Evaluate(
            IEnumerable<ExchangeParitySoakObservation> observations)
        {
            ExchangeParitySoakObservation[] samples =
                (observations ?? throw new ArgumentNullException(nameof(observations)))
                .OrderBy(observation => observation.Timestamp)
                .ToArray();
            int blockingSamples = samples.Count(sample =>
                !sample.CollectionFailed && sample.BlockingFindingCount > 0);
            int collectionFailures = samples.Count(sample => sample.CollectionFailed);
            int successfulSamples = samples.Length - collectionFailures;
            int maximumConsecutive = MaximumConsecutiveBlocking(samples);
            TimeSpan duration = samples.Length < 2
                ? TimeSpan.Zero
                : samples[^1].Timestamp - samples[0].Timestamp;
            List<string> reasons = new List<string>();

            if (successfulSamples < _criteria.RequiredSamples)
                reasons.Add("insufficient_samples");
            if (duration < _criteria.RequiredDuration)
                reasons.Add("insufficient_duration");
            if (blockingSamples > _criteria.MaximumBlockingSamples)
                reasons.Add("blocking_parity_threshold_exceeded");
            if (maximumConsecutive > _criteria.MaximumConsecutiveBlockingSamples)
                reasons.Add("consecutive_parity_threshold_exceeded");
            if (collectionFailures > _criteria.MaximumCollectionFailures)
                reasons.Add("collection_failure_threshold_exceeded");

            return new ExchangeParitySoakReport
            {
                ObservationCount = samples.Length,
                SampleCount = successfulSamples,
                ObservedDuration = duration,
                BlockingSampleCount = blockingSamples,
                MaximumConsecutiveBlockingSamples = maximumConsecutive,
                CollectionFailureCount = collectionFailures,
                RollbackReasons = reasons
            };
        }

        private static int MaximumConsecutiveBlocking(
            IEnumerable<ExchangeParitySoakObservation> observations)
        {
            int current = 0;
            int maximum = 0;
            foreach (ExchangeParitySoakObservation observation in observations)
            {
                if (!observation.CollectionFailed &&
                    observation.BlockingFindingCount > 0)
                {
                    current++;
                    maximum = Math.Max(maximum, current);
                }
                else
                {
                    current = 0;
                }
            }
            return maximum;
        }
    }
}
