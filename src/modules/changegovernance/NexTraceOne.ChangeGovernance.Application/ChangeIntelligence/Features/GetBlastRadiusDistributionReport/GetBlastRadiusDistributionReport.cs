using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusDistributionReport;

/// <summary>
/// Feature: GetBlastRadiusDistributionReport — distribuição de blast radius das releases no período.
///
/// Agrega os relatórios de blast radius calculados para as releases no período e produz:
/// - total de releases no período e quantas têm blast radius calculado
/// - média de consumidores diretos e totais por release com blast radius
/// - distribuição por bucket de impacto:
///   <c>Zero</c> (0 consumidores), <c>Small</c> (1–5), <c>Medium</c> (6–20), <c>Large</c> (&gt;20)
/// - top releases com maior blast radius total (direto + transitivo)
/// - top serviços cujas releases produzem maior blast radius em média
///
/// Permite que Architect e Tech Lead compreendam o risco real de propagação de mudanças
/// no ecossistema de serviços, alimentando decisões de promoção e janelas de change.
///
/// Wave Q.3 — Blast Radius Distribution Report (ChangeGovernance ChangeIntelligence).
/// </summary>
public static class GetBlastRadiusDistributionReport
{
    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–365, default 90).</para>
    /// <para><c>MaxTopReleases</c>: número máximo de releases no ranking de blast radius (1–100, default 10).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente (null = todos).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int MaxTopReleases = 10,
        string? Environment = null) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Distribuição de releases por bucket de impacto de blast radius.</summary>
    public sealed record BlastRadiusBucketDistribution(
        int ZeroCount,
        int SmallCount,
        int MediumCount,
        int LargeCount);

    /// <summary>Release com maior blast radius calculado.</summary>
    public sealed record ReleaseBlastRadiusEntry(
        string ServiceName,
        string Version,
        string Environment,
        int DirectConsumers,
        int TransitiveConsumers,
        int TotalConsumers);

    /// <summary>Serviço cujas releases produzem maior blast radius médio.</summary>
    public sealed record ServiceBlastRadiusEntry(
        string ServiceName,
        int ReleaseCount,
        decimal AvgDirectConsumers,
        decimal AvgTotalConsumers,
        int MaxTotalConsumers);

    /// <summary>Resultado do relatório de distribuição de blast radius.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalReleasesInPeriod,
        int ReleasesWithBlastRadius,
        int ReleasesWithoutBlastRadius,
        decimal AvgDirectConsumers,
        decimal AvgTotalConsumers,
        int MaxTotalConsumers,
        BlastRadiusBucketDistribution BucketDistribution,
        IReadOnlyList<ReleaseBlastRadiusEntry> TopReleasesByBlastRadius,
        IReadOnlyList<ServiceBlastRadiusEntry> TopServicesByAvgBlastRadius);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.MaxTopReleases).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    private const int SmallBucketMax = 5;
    private const int MediumBucketMax = 20;

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IReleaseRepository _releaseRepo;
        private readonly IBlastRadiusRepository _blastRadiusRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IReleaseRepository releaseRepo,
            IBlastRadiusRepository blastRadiusRepo,
            IDateTimeProvider clock)
        {
            _releaseRepo = Guard.Against.Null(releaseRepo);
            _blastRadiusRepo = Guard.Against.Null(blastRadiusRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);
            var tenantId = Guid.Parse(query.TenantId);

            var releases = await _releaseRepo.ListInRangeAsync(from, now, query.Environment, tenantId, cancellationToken);

            int withBlast = 0, withoutBlast = 0;
            int zeroCount = 0, smallCount = 0, mediumCount = 0, largeCount = 0;
            long sumDirect = 0, sumTotal = 0;
            int maxTotal = 0;

            var releaseEntries = new List<ReleaseBlastRadiusEntry>();
            var serviceMap = new Dictionary<string, List<int>>();

            foreach (var release in releases)
            {
                var blast = await _blastRadiusRepo.GetByReleaseIdAsync(release.Id, cancellationToken);
                if (blast is null)
                {
                    withoutBlast++;
                    continue;
                }

                withBlast++;
                int direct = blast.DirectConsumers.Count;
                int total = blast.TotalAffectedConsumers;

                sumDirect += direct;
                sumTotal += total;
                if (total > maxTotal) maxTotal = total;

                if (total == 0) zeroCount++;
                else if (total <= SmallBucketMax) smallCount++;
                else if (total <= MediumBucketMax) mediumCount++;
                else largeCount++;

                releaseEntries.Add(new ReleaseBlastRadiusEntry(
                    ServiceName: release.ServiceName,
                    Version: release.Version,
                    Environment: release.Environment,
                    DirectConsumers: direct,
                    TransitiveConsumers: blast.TransitiveConsumers.Count,
                    TotalConsumers: total));

                if (!serviceMap.TryGetValue(release.ServiceName, out var svcList))
                {
                    svcList = [];
                    serviceMap[release.ServiceName] = svcList;
                }
                svcList.Add(total);
            }

            decimal avgDirect = withBlast > 0 ? Math.Round((decimal)sumDirect / withBlast, 2) : 0m;
            decimal avgTotal = withBlast > 0 ? Math.Round((decimal)sumTotal / withBlast, 2) : 0m;

            var topReleases = releaseEntries
                .OrderByDescending(r => r.TotalConsumers)
                .Take(query.MaxTopReleases)
                .ToList();

            var topServices = serviceMap
                .Select(kvp => new ServiceBlastRadiusEntry(
                    ServiceName: kvp.Key,
                    ReleaseCount: kvp.Value.Count,
                    AvgDirectConsumers: Math.Round((decimal)kvp.Value.Sum() / kvp.Value.Count, 2),
                    AvgTotalConsumers: Math.Round((decimal)kvp.Value.Sum() / kvp.Value.Count, 2),
                    MaxTotalConsumers: kvp.Value.Max()))
                .OrderByDescending(s => s.AvgTotalConsumers)
                .Take(query.MaxTopReleases)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalReleasesInPeriod: releases.Count,
                ReleasesWithBlastRadius: withBlast,
                ReleasesWithoutBlastRadius: withoutBlast,
                AvgDirectConsumers: avgDirect,
                AvgTotalConsumers: avgTotal,
                MaxTotalConsumers: maxTotal,
                BucketDistribution: new BlastRadiusBucketDistribution(zeroCount, smallCount, mediumCount, largeCount),
                TopReleasesByBlastRadius: topReleases,
                TopServicesByAvgBlastRadius: topServices));
        }
    }
}
