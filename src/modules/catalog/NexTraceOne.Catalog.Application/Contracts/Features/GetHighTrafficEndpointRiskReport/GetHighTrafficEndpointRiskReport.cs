using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetHighTrafficEndpointRiskReport;

/// <summary>
/// Feature: GetHighTrafficEndpointRiskReport — análise de risco de endpoints de alto tráfego.
///
/// Por endpoint com volume acima de limiar:
/// - <c>EndpointRiskScore</c> = (1 - ContractCoverage) × 30 + (1 - ChaosTested) × 25 + ErrorRate × 25 + LatencyP99 > threshold × 20
/// - <c>EndpointRiskTier</c>: Safe / Monitored / AtRisk / Critical
///
/// Surfaces <c>CriticalUncoveredEndpoints</c>, <c>DocumentationOpportunity</c>,
/// <c>ChaosGapByTrafficVolume</c>, <c>SloGapForHighTraffic</c>.
///
/// Wave AZ.2 — Service Mesh &amp; Runtime Traffic Intelligence (Catalog Contracts).
/// </summary>
public static class GetHighTrafficEndpointRiskReport
{
    // ── Defaults ───────────────────────────────────────────────────────────
    internal const double DefaultRpsThreshold = 100.0;
    internal const int DefaultTopN = 20;
    internal const int DefaultLookbackDays = 7;
    internal const double DefaultLatencyP99ThresholdMs = 1000.0;
    internal const int DefaultDocumentationPriorityThreshold = 1000;

    // ── Risk weights ───────────────────────────────────────────────────────
    private const decimal ContractCoverageWeight = 30m;
    private const decimal ChaosTestedWeight = 25m;
    private const decimal ErrorRateWeight = 25m;
    private const decimal LatencyWeight = 20m;

    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal SafeThreshold = 25m;
    private const decimal MonitoredThreshold = 50m;
    private const decimal AtRiskThreshold = 75m;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        double RpsThreshold = DefaultRpsThreshold,
        int TopN = DefaultTopN,
        double LatencyP99ThresholdMs = DefaultLatencyP99ThresholdMs,
        int DocumentationPriorityThreshold = DefaultDocumentationPriorityThreshold) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 90);
            RuleFor(x => x.TopN).InclusiveBetween(1, 100);
            RuleFor(x => x.RpsThreshold).GreaterThan(0);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum EndpointRiskTier { Safe, Monitored, AtRisk, Critical }
    public enum EndpointContractCoverage { Documented, Undocumented, Deprecated }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record EndpointRiskEntry(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string EndpointPath,
        string HttpMethod,
        long CallVolume,
        double RpsAvg,
        double LatencyP50Ms,
        double LatencyP95Ms,
        double LatencyP99Ms,
        double ErrorRatePct,
        EndpointContractCoverage ContractCoverage,
        bool HasActiveSlo,
        bool WasChaosTestedInPeriod,
        decimal EndpointRiskScore,
        EndpointRiskTier RiskTier);

    public sealed record TenantHighTrafficRiskSummary(
        int CriticalUncoveredEndpointCount,
        decimal TenantEndpointRiskScore,
        IReadOnlyList<EndpointRiskEntry> TopRiskByVolume);

    public sealed record Report(
        IReadOnlyList<EndpointRiskEntry> TopEndpoints,
        TenantHighTrafficRiskSummary Summary,
        IReadOnlyList<EndpointRiskEntry> DocumentationOpportunity,
        IReadOnlyList<EndpointRiskEntry> ChaosGapByTrafficVolume,
        IReadOnlyList<EndpointRiskEntry> SloGapForHighTraffic);

    // ── Handler ───────────────────────────────────────────────────────────
    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IHighTrafficEndpointReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(IHighTrafficEndpointReader reader, IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query request, CancellationToken ct)
        {
            var to = _clock.UtcNow;
            var from = to.AddDays(-request.LookbackDays);

            var raw = await _reader.ListByTenantAsync(request.TenantId, from, to, ct);

            // Filter: high-traffic endpoints
            var highTraffic = raw
                .Where(e => e.RpsAvg >= request.RpsThreshold)
                .OrderByDescending(e => e.CallVolume)
                .Take(request.TopN)
                .ToList();

            var riskEntries = highTraffic
                .Select(e => BuildEntry(e, request))
                .ToList();

            // Tenant score: volume-weighted average of EndpointRiskScore
            decimal tenantScore = riskEntries.Count > 0
                ? ComputeWeightedScore(riskEntries)
                : 0m;

            var criticalUncovered = riskEntries
                .Count(e => e.RiskTier == EndpointRiskTier.Critical &&
                            e.ContractCoverage == EndpointContractCoverage.Undocumented);

            var topRisk = riskEntries
                .OrderByDescending(e => e.CallVolume)
                .ThenByDescending(e => e.EndpointRiskScore)
                .Take(5)
                .ToList();

            var docOpportunity = riskEntries
                .Where(e => e.ContractCoverage == EndpointContractCoverage.Undocumented
                         && e.CallVolume >= request.DocumentationPriorityThreshold)
                .OrderByDescending(e => e.CallVolume)
                .ToList();

            var chaosGap = riskEntries
                .Where(e => !e.WasChaosTestedInPeriod)
                .OrderByDescending(e => e.CallVolume)
                .ToList();

            var sloGap = riskEntries
                .Where(e => !e.HasActiveSlo)
                .OrderByDescending(e => e.CallVolume)
                .ToList();

            var summary = new TenantHighTrafficRiskSummary(criticalUncovered, tenantScore, topRisk);

            return Result<Report>.Success(new Report(
                TopEndpoints: riskEntries,
                Summary: summary,
                DocumentationOpportunity: docOpportunity,
                ChaosGapByTrafficVolume: chaosGap,
                SloGapForHighTraffic: sloGap));
        }

        internal static EndpointRiskEntry BuildEntry(
            IHighTrafficEndpointReader.EndpointTrafficEntry e,
            Query request)
        {
            var coverage = e.IsDocumentedInContract
                ? EndpointContractCoverage.Documented
                : e.IsDeprecatedInContract
                    ? EndpointContractCoverage.Deprecated
                    : EndpointContractCoverage.Undocumented;

            decimal contractPenalty = coverage == EndpointContractCoverage.Undocumented ? ContractCoverageWeight : 0m;
            decimal chaosPenalty = e.WasChaosTestedInPeriod ? 0m : ChaosTestedWeight;
            decimal errorPenalty = (decimal)Math.Min(e.ErrorRatePct / 20.0, 1.0) * ErrorRateWeight;
            decimal latencyPenalty = e.LatencyP99Ms > request.LatencyP99ThresholdMs ? LatencyWeight : 0m;
            decimal riskScore = Math.Round(contractPenalty + chaosPenalty + errorPenalty + latencyPenalty, 1);

            var tier = riskScore < SafeThreshold ? EndpointRiskTier.Safe
                : riskScore < MonitoredThreshold ? EndpointRiskTier.Monitored
                : riskScore < AtRiskThreshold ? EndpointRiskTier.AtRisk
                : EndpointRiskTier.Critical;

            return new EndpointRiskEntry(
                e.ServiceId, e.ServiceName, e.TeamName,
                e.EndpointPath, e.HttpMethod,
                e.CallVolume, e.RpsAvg,
                e.LatencyP50Ms, e.LatencyP95Ms, e.LatencyP99Ms,
                e.ErrorRatePct, coverage, e.HasActiveSlo,
                e.WasChaosTestedInPeriod, riskScore, tier);
        }

        private static decimal ComputeWeightedScore(IReadOnlyList<EndpointRiskEntry> entries)
        {
            long totalVolume = entries.Sum(e => e.CallVolume);
            if (totalVolume == 0) return 0m;

            decimal weightedSum = entries.Sum(e => e.EndpointRiskScore * e.CallVolume);
            return Math.Round(weightedSum / totalVolume, 1);
        }
    }
}
