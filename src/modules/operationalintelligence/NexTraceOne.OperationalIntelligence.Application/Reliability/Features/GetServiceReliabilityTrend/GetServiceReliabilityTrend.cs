using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityTrend;

/// <summary>
/// Feature: GetServiceReliabilityTrend — tendência histórica de confiabilidade de um serviço.
/// Baseada em ReliabilitySnapshot persistidos pelo domínio Reliability.
/// </summary>
public static class GetServiceReliabilityTrend
{
    public sealed record Query(string ServiceId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IReliabilitySnapshotRepository snapshotRepository,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var history = await snapshotRepository.GetHistoryAsync(
                request.ServiceId, tenant.Id, 30, cancellationToken);

            if (history.Count == 0)
            {
                return Result<Response>.Success(new Response(
                    request.ServiceId,
                    TrendDirection.Stable,
                    "30d",
                    "Insufficient data to determine trend.",
                    []));
            }

            var dataPoints = history
                .OrderBy(s => s.ComputedAt)
                .Select(s => new TrendDataPoint(
                    s.ComputedAt,
                    DeriveReliabilityStatus(s.OverallScore),
                    s.OverallScore,
                    ComputeErrorRateFromScore(s.RuntimeHealthScore)))
                .ToList();

            var latest = history[0];
            var previous = history.Count > 1 ? history[1] : null;
            var trend = ComputeTrend(latest.OverallScore, previous?.OverallScore);

            var summary = trend switch
            {
                TrendDirection.Improving => $"Reliability improved: score {previous?.OverallScore:F1} → {latest.OverallScore:F1}.",
                TrendDirection.Declining => $"Reliability declined: score {previous?.OverallScore:F1} → {latest.OverallScore:F1}.",
                _ => $"Reliability stable at {latest.OverallScore:F1} over the last {history.Count} snapshots."
            };

            return Result<Response>.Success(new Response(
                request.ServiceId, trend, "30d", summary, dataPoints));
        }

        private static TrendDirection ComputeTrend(decimal current, decimal? previous)
        {
            if (previous is null) return TrendDirection.Stable;
            if (current > previous.Value + 5m) return TrendDirection.Improving;
            if (current < previous.Value - 5m) return TrendDirection.Declining;
            return TrendDirection.Stable;
        }

        private static ReliabilityStatus DeriveReliabilityStatus(decimal overallScore) =>
            overallScore switch
            {
                >= 75m => ReliabilityStatus.Healthy,
                >= 60m => ReliabilityStatus.NeedsAttention,
                >= 40m => ReliabilityStatus.Degraded,
                _ => ReliabilityStatus.Unavailable
            };

        private static decimal ComputeErrorRateFromScore(decimal runtimeHealthScore) =>
            runtimeHealthScore switch
            {
                100m => 0m,
                60m => 5m,
                20m => 15m,
                _ => 0m
            };
    }

    public sealed record TrendDataPoint(
        DateTimeOffset Timestamp,
        ReliabilityStatus Status,
        decimal OverallScore,
        decimal ErrorRatePercent);

    public sealed record Response(
        string ServiceId,
        TrendDirection Direction,
        string Timeframe,
        string Summary,
        IReadOnlyList<TrendDataPoint> DataPoints);
}
