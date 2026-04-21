using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetServiceLoadDistributionReport;

/// <summary>
/// Feature: GetServiceLoadDistributionReport — distribuição de carga operacional por serviço,
/// correlacionada com custo por request.
///
/// Para cada serviço com snapshots de runtime no período:
/// - calcula médias de latência, taxa de erro e throughput (RequestsPerSecond) a partir de
///   <c>RuntimeSnapshot</c>
/// - agrega custo total do período via <c>ServiceCostAllocationRecord</c>
/// - calcula <c>CostPerRequestUsd</c> = TotalCostUsd / (AvgRps * PeriodSeconds)
/// - classifica por <c>LoadBand</c> com base no quartil de throughput:
///   - <c>HighLoad</c>  — top 25% de throughput
///   - <c>MediumLoad</c>— quartis 2 e 3 (50% central)
///   - <c>LowLoad</c>   — bottom 25%
/// - flag <c>WasteCandidate</c> — serviços <c>LowLoad</c> com custo acima da mediana de pares
/// - flag <c>HighCostEfficiency</c> — serviços <c>HighLoad</c> com CostPerRequest abaixo da mediana
///
/// Produz:
/// - distribuição global por LoadBand
/// - top 10 serviços com pior CostPerRequest (outliers de waste)
/// - médianas de throughput e custo por request do tenant
///
/// Orienta Platform Admin, FinOps e Architect em decisões de rightsizing.
///
/// Wave U.3 — Service Load Distribution Report (OperationalIntelligence Runtime).
/// </summary>
public static class GetServiceLoadDistributionReport
{
    private const int HighLoadQuartileThreshold = 75;  // top 25%
    private const int LowLoadQuartileThreshold = 25;   // bottom 25%

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant para filtro de custos (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal de análise (7–90, default 30).</para>
    /// <para><c>MaxTopServices</c>: número máximo de serviços no ranking de waste (1–100, default 10).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente (null = todos).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int MaxTopServices = 10,
        string? Environment = null) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Classificação de carga operacional por quartil de throughput.</summary>
    public enum LoadBand
    {
        /// <summary>Top 25% de throughput — serviço de alta carga.</summary>
        HighLoad,
        /// <summary>Quartis 2 e 3 — carga normal.</summary>
        MediumLoad,
        /// <summary>Bottom 25% de throughput — serviço de baixa carga.</summary>
        LowLoad
    }

    /// <summary>Distribuição de serviços por LoadBand.</summary>
    public sealed record LoadBandDistribution(
        int HighLoadCount,
        int MediumLoadCount,
        int LowLoadCount);

    /// <summary>Métricas de carga e custo para um serviço.</summary>
    public sealed record ServiceLoadEntry(
        string ServiceName,
        string? Environment,
        decimal AvgLatencyMs,
        decimal AvgErrorRate,
        decimal AvgRequestsPerSecond,
        decimal TotalCostUsd,
        decimal CostPerRequestUsd,
        LoadBand Band,
        bool WasteCandidate,
        bool HighCostEfficiency);

    /// <summary>Resultado do relatório de distribuição de carga por serviço.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalServicesAnalyzed,
        decimal MedianRequestsPerSecond,
        decimal MedianCostPerRequestUsd,
        LoadBandDistribution BandDistribution,
        IReadOnlyList<ServiceLoadEntry> TopWasteServices,
        IReadOnlyList<ServiceLoadEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 90);
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IRuntimeSnapshotRepository _snapshotRepo;
        private readonly IServiceCostAllocationRepository _costRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IRuntimeSnapshotRepository snapshotRepo,
            IServiceCostAllocationRepository costRepo,
            IDateTimeProvider clock)
        {
            _snapshotRepo = Guard.Against.Null(snapshotRepo);
            _costRepo = Guard.Against.Null(costRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);
            decimal periodSeconds = query.LookbackDays * 86400m;

            // 1. Get service+environment pairs with recent snapshots
            var servicePairs = await _snapshotRepo.GetServicesWithRecentSnapshotsAsync(from, cancellationToken);

            if (servicePairs.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    LookbackDays: query.LookbackDays,
                    TotalServicesAnalyzed: 0,
                    MedianRequestsPerSecond: 0m,
                    MedianCostPerRequestUsd: 0m,
                    BandDistribution: new LoadBandDistribution(0, 0, 0),
                    TopWasteServices: [],
                    AllServices: []));
            }

            // Filter by environment if requested
            var filtered = query.Environment is null
                ? servicePairs
                : servicePairs.Where(p => string.Equals(p.Environment, query.Environment, StringComparison.OrdinalIgnoreCase)).ToList();

            // 2. Get cost records for the tenant in the period
            var costRecords = await _costRepo.ListByTenantAsync(
                query.TenantId, from, now,
                environment: query.Environment,
                category: null,
                ct: cancellationToken);

            // Build cost lookup: serviceName → total cost USD
            var costByService = costRecords
                .GroupBy(r => r.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r => r.AmountUsd),
                    StringComparer.OrdinalIgnoreCase);

            // 3. Aggregate snapshot metrics per service+environment
            var rawEntries = new List<(string ServiceName, string? Env, decimal AvgLatencyMs, decimal AvgRps, decimal AvgErrorRate, decimal TotalCostUsd)>();

            foreach (var (serviceName, env) in filtered)
            {
                var snapshots = await _snapshotRepo.ListByServiceAsync(
                    serviceName, env, page: 1, pageSize: 1000, cancellationToken: cancellationToken);

                var recent = snapshots.Where(s => s.CapturedAt >= from).ToList();
                if (recent.Count == 0)
                    continue;

                decimal avgLatency = Math.Round(recent.Average(s => s.AvgLatencyMs), 4);
                decimal avgRps = Math.Round(recent.Average(s => s.RequestsPerSecond), 4);
                decimal avgErrorRate = Math.Round(recent.Average(s => s.ErrorRate), 6);

                costByService.TryGetValue(serviceName, out var totalCost);

                rawEntries.Add((serviceName, env, avgLatency, avgRps, avgErrorRate, totalCost));
            }

            if (rawEntries.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    LookbackDays: query.LookbackDays,
                    TotalServicesAnalyzed: 0,
                    MedianRequestsPerSecond: 0m,
                    MedianCostPerRequestUsd: 0m,
                    BandDistribution: new LoadBandDistribution(0, 0, 0),
                    TopWasteServices: [],
                    AllServices: []));
            }

            // 4. Compute CostPerRequest for each service
            var withCost = rawEntries
                .Select(e =>
                {
                    decimal totalRequests = e.AvgRps * periodSeconds;
                    decimal costPerReq = totalRequests > 0m && e.TotalCostUsd > 0m
                        ? Math.Round(e.TotalCostUsd / totalRequests, 8)
                        : 0m;
                    return (e.ServiceName, e.Env, e.AvgLatencyMs, e.AvgRps, e.AvgErrorRate, e.TotalCostUsd, CostPerReq: costPerReq);
                })
                .ToList();

            // 5. Classify by LoadBand (quartile of AvgRps)
            var sortedRps = withCost.Select(e => e.AvgRps).OrderBy(x => x).ToList();
            decimal highLoadThreshold = Percentile(sortedRps, HighLoadQuartileThreshold);
            decimal lowLoadThreshold = Percentile(sortedRps, LowLoadQuartileThreshold);
            decimal medianRps = Percentile(sortedRps, 50);

            var sortedCostPerReq = withCost
                .Where(e => e.CostPerReq > 0m)
                .Select(e => e.CostPerReq)
                .OrderBy(x => x)
                .ToList();
            decimal medianCostPerReq = sortedCostPerReq.Count > 0
                ? Percentile(sortedCostPerReq, 50)
                : 0m;

            var entries = withCost
                .Select(e =>
                {
                    var band = ClassifyBand(e.AvgRps, highLoadThreshold, lowLoadThreshold);
                    bool wasteCandidate = band == LoadBand.LowLoad && e.TotalCostUsd > medianCostPerReq;
                    bool highEfficiency = band == LoadBand.HighLoad && e.CostPerReq > 0m && e.CostPerReq < medianCostPerReq;

                    return new ServiceLoadEntry(
                        ServiceName: e.ServiceName,
                        Environment: e.Env,
                        AvgLatencyMs: e.AvgLatencyMs,
                        AvgErrorRate: e.AvgErrorRate,
                        AvgRequestsPerSecond: e.AvgRps,
                        TotalCostUsd: e.TotalCostUsd,
                        CostPerRequestUsd: e.CostPerReq,
                        Band: band,
                        WasteCandidate: wasteCandidate,
                        HighCostEfficiency: highEfficiency);
                })
                .ToList();

            int highCount = entries.Count(e => e.Band == LoadBand.HighLoad);
            int medCount = entries.Count(e => e.Band == LoadBand.MediumLoad);
            int lowCount = entries.Count(e => e.Band == LoadBand.LowLoad);

            var topWaste = entries
                .OrderByDescending(e => e.CostPerRequestUsd)
                .Take(query.MaxTopServices)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalServicesAnalyzed: entries.Count,
                MedianRequestsPerSecond: Math.Round(medianRps, 4),
                MedianCostPerRequestUsd: Math.Round(medianCostPerReq, 8),
                BandDistribution: new LoadBandDistribution(highCount, medCount, lowCount),
                TopWasteServices: topWaste,
                AllServices: entries.OrderBy(e => e.ServiceName).ThenBy(e => e.Environment).ToList()));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static LoadBand ClassifyBand(decimal rps, decimal highThreshold, decimal lowThreshold) =>
            rps >= highThreshold ? LoadBand.HighLoad
            : rps > lowThreshold ? LoadBand.MediumLoad
            : LoadBand.LowLoad;

        /// <summary>Percentile interpolado de uma lista ordenada.</summary>
        private static decimal Percentile(IReadOnlyList<decimal> sorted, int percentile)
        {
            if (sorted.Count == 0) return 0m;
            if (sorted.Count == 1) return sorted[0];

            decimal rank = (percentile / 100.0m) * (sorted.Count - 1);
            int lower = (int)Math.Floor((double)rank);
            int upper = (int)Math.Ceiling((double)rank);

            if (lower == upper)
                return sorted[lower];

            decimal fraction = rank - lower;
            return Math.Round(sorted[lower] + fraction * (sorted[upper] - sorted[lower]), 8);
        }
    }
}
