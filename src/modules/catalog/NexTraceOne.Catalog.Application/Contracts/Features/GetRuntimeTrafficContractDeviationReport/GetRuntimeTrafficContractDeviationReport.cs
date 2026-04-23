using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetRuntimeTrafficContractDeviationReport;

/// <summary>
/// Feature: GetRuntimeTrafficContractDeviationReport — desvios entre o tráfego real observado e o contrato declarado.
///
/// Detecta por serviço:
/// - <c>UndocumentedEndpointCalls</c> — chamadas a endpoints não declarados no contrato activo
/// - <c>UndeclaredConsumers</c> — serviços a chamar sem estarem registados como ContractConsumer
/// - <c>PayloadDeviationRate</c> — % respostas cujo payload diverge do schema contratado
/// - <c>ObservedVsContractedStatusCodes</c> — status codes retornados mas não documentados no contrato
///
/// <c>TrafficContractDeviationTier</c>: Aligned / MinorDrift / Significant / Critical
/// <c>TenantDeviationHealthScore</c> = % serviços com tier Aligned
///
/// Wave AZ.1 — Service Mesh &amp; Runtime Traffic Intelligence (Catalog Contracts).
/// </summary>
public static class GetRuntimeTrafficContractDeviationReport
{
    // ── Tier thresholds (defaults, overridden by config) ──────────────────
    internal const int DefaultMinorDriftThreshold = 3;
    internal const int DefaultCriticalUndeclaredConsumers = 1;
    internal const int DefaultLookbackDays = 7;
    internal const int DefaultTopDeviatingCount = 10;
    internal const int DefaultTopHotspotsCount = 10;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        int MinorDriftThreshold = DefaultMinorDriftThreshold,
        int CriticalUndeclaredConsumers = DefaultCriticalUndeclaredConsumers,
        int TopDeviatingCount = DefaultTopDeviatingCount,
        int TopHotspotsCount = DefaultTopHotspotsCount) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 90);
            RuleFor(x => x.MinorDriftThreshold).GreaterThanOrEqualTo(1);
            RuleFor(x => x.CriticalUndeclaredConsumers).GreaterThanOrEqualTo(1);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum TrafficContractDeviationTier { Aligned, MinorDrift, Significant, Critical }
    public enum DeviationTrendDirection { Improving, Stable, Worsening }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record UndocumentedEndpointHotspot(
        string ServiceId,
        string ServiceName,
        string Path,
        string Method,
        long CallCount);

    public sealed record ContractGapOpportunity(
        string ServiceId,
        string ServiceName,
        int UndocumentedEndpointCount,
        int UndeclaredConsumerCount);

    public sealed record ServiceDeviationEntry(
        string ServiceId,
        string ServiceName,
        string TeamName,
        int UndocumentedEndpointCalls,
        int UndeclaredConsumers,
        decimal PayloadDeviationRate,
        int UndocumentedStatusCodesCount,
        int TotalDeviations,
        TrafficContractDeviationTier DeviationTier);

    public sealed record TenantTrafficContractDeviationSummary(
        int ServicesWithUndocumentedEndpoints,
        int ServicesWithUndeclaredConsumers,
        decimal TenantDeviationHealthScore,
        IReadOnlyList<ServiceDeviationEntry> TopDeviatingServices);

    public sealed record Report(
        IReadOnlyList<ServiceDeviationEntry> ByService,
        TenantTrafficContractDeviationSummary Summary,
        IReadOnlyList<UndocumentedEndpointHotspot> UndocumentedEndpointHotspots,
        IReadOnlyList<ContractGapOpportunity> ContractGapOpportunities,
        DeviationTrendDirection HistoricalDeviationTrend);

    // ── Handler ───────────────────────────────────────────────────────────
    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly ITrafficObservationReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(ITrafficObservationReader reader, IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query request, CancellationToken ct)
        {
            var to = _clock.UtcNow;
            var from = to.AddDays(-request.LookbackDays);

            var entries = await _reader.ListByTenantAsync(request.TenantId, from, to, ct);
            var trendSnapshots = await _reader.GetDeviationTrendAsync(request.TenantId, from, to, ct);

            var byService = entries
                .Select(e => BuildServiceEntry(e, request))
                .ToList();

            var alignedCount = byService.Count(s => s.DeviationTier == TrafficContractDeviationTier.Aligned);
            var tenantScore = byService.Count > 0
                ? Math.Round((decimal)alignedCount / byService.Count * 100m, 1)
                : 100m;

            var topDeviating = byService
                .OrderByDescending(s => s.TotalDeviations)
                .Take(request.TopDeviatingCount)
                .ToList();

            var hotspots = BuildHotspots(entries, request.TopHotspotsCount);
            var gapOpportunities = BuildGapOpportunities(byService);
            var trend = ComputeTrend(trendSnapshots);

            var summary = new TenantTrafficContractDeviationSummary(
                ServicesWithUndocumentedEndpoints: byService.Count(s => s.UndocumentedEndpointCalls > 0),
                ServicesWithUndeclaredConsumers: byService.Count(s => s.UndeclaredConsumers > 0),
                TenantDeviationHealthScore: tenantScore,
                TopDeviatingServices: topDeviating);

            return Result<Report>.Success(new Report(
                ByService: byService,
                Summary: summary,
                UndocumentedEndpointHotspots: hotspots,
                ContractGapOpportunities: gapOpportunities,
                HistoricalDeviationTrend: trend));
        }

        private static ServiceDeviationEntry BuildServiceEntry(
            ITrafficObservationReader.ServiceTrafficObservationEntry e,
            Query request)
        {
            // Undocumented endpoints: observed endpoints not in contracted list
            var contractedSet = e.ContractedEndpoints
                .Select(NormaliseEndpointKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var undocumentedEndpoints = e.ObservedEndpoints
                .Where(ep => !contractedSet.Contains(NormaliseEndpointKey($"{ep.Method}:{ep.Path}")))
                .ToList();

            int undocumentedCount = undocumentedEndpoints.Count;

            // Undeclared consumers
            var registeredSet = new HashSet<string>(e.RegisteredConsumerIds, StringComparer.OrdinalIgnoreCase);
            int undeclaredConsumers = e.ObservedConsumerIds.Count(c => !registeredSet.Contains(c));

            // Payload deviation rate
            decimal payloadDeviationRate = e.TotalPayloadValidationEvents > 0
                ? Math.Round((decimal)e.PayloadDeviationEvents / e.TotalPayloadValidationEvents * 100m, 2)
                : 0m;

            // Undocumented status codes
            var contractedCodes = new HashSet<string>(e.ContractedStatusCodes, StringComparer.OrdinalIgnoreCase);
            int undocumentedStatusCodes = e.ObservedStatusCodes
                .Count(sc => !contractedCodes.Contains(sc.StatusCode.ToString()));

            int totalDeviations = undocumentedCount + undeclaredConsumers + undocumentedStatusCodes;

            var tier = ComputeTier(undocumentedCount, undeclaredConsumers, e.ServiceTier, request);

            return new ServiceDeviationEntry(
                e.ServiceId, e.ServiceName, e.TeamName,
                undocumentedCount, undeclaredConsumers, payloadDeviationRate,
                undocumentedStatusCodes, totalDeviations, tier);
        }

        internal static TrafficContractDeviationTier ComputeTier(
            int undocumentedEndpoints,
            int undeclaredConsumers,
            string serviceTier,
            Query request)
        {
            bool isCriticalService = string.Equals(serviceTier, "Critical", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(serviceTier, "High", StringComparison.OrdinalIgnoreCase);

            if (isCriticalService && undeclaredConsumers >= request.CriticalUndeclaredConsumers)
                return TrafficContractDeviationTier.Critical;

            if (undocumentedEndpoints == 0 && undeclaredConsumers == 0)
                return TrafficContractDeviationTier.Aligned;

            if (undocumentedEndpoints <= request.MinorDriftThreshold && undeclaredConsumers == 0)
                return TrafficContractDeviationTier.MinorDrift;

            if (undocumentedEndpoints > request.MinorDriftThreshold * 2 || undeclaredConsumers > 0)
                return TrafficContractDeviationTier.Significant;

            return TrafficContractDeviationTier.MinorDrift;
        }

        private static IReadOnlyList<UndocumentedEndpointHotspot> BuildHotspots(
            IReadOnlyList<ITrafficObservationReader.ServiceTrafficObservationEntry> entries,
            int topN)
        {
            var result = new List<UndocumentedEndpointHotspot>();

            foreach (var e in entries)
            {
                var contractedSet = e.ContractedEndpoints
                    .Select(NormaliseEndpointKey)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var ep in e.ObservedEndpoints)
                {
                    if (!contractedSet.Contains(NormaliseEndpointKey($"{ep.Method}:{ep.Path}")))
                        result.Add(new UndocumentedEndpointHotspot(e.ServiceId, e.ServiceName, ep.Path, ep.Method, ep.CallCount));
                }
            }

            return result
                .OrderByDescending(h => h.CallCount)
                .Take(topN)
                .ToList();
        }

        private static IReadOnlyList<ContractGapOpportunity> BuildGapOpportunities(
            IReadOnlyList<ServiceDeviationEntry> entries) =>
            entries
                .Where(e => e.UndocumentedEndpointCalls > 0 || e.UndeclaredConsumers > 0)
                .OrderByDescending(e => e.TotalDeviations)
                .Select(e => new ContractGapOpportunity(
                    e.ServiceId, e.ServiceName, e.UndocumentedEndpointCalls, e.UndeclaredConsumers))
                .ToList();

        internal static DeviationTrendDirection ComputeTrend(
            IReadOnlyList<ITrafficObservationReader.DailyDeviationSnapshot> snapshots)
        {
            if (snapshots.Count < 2) return DeviationTrendDirection.Stable;

            var first = snapshots[0].DeviationCount;
            var last = snapshots[snapshots.Count - 1].DeviationCount;
            var delta = last - first;

            return delta < -1 ? DeviationTrendDirection.Improving
                : delta > 1 ? DeviationTrendDirection.Worsening
                : DeviationTrendDirection.Stable;
        }

        private static string NormaliseEndpointKey(string key) =>
            key.Trim().ToUpperInvariant();
    }
}
