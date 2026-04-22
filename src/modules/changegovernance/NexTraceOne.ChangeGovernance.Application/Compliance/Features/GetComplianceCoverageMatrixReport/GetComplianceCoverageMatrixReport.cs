using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetComplianceCoverageMatrixReport;

/// <summary>
/// Feature: GetComplianceCoverageMatrixReport — matriz de cobertura de compliance por serviço.
///
/// Identifica "compliance blind spots" mostrando, para cada serviço activo no tenant,
/// quais standards de compliance foram avaliados e com que estado de conformidade.
///
/// Os serviços activos são derivados de releases no período via <see cref="IReleaseRepository"/>.
/// A cobertura per-serviço é obtida via <see cref="IComplianceServiceCoverageReader"/>
/// (honest-null por defeito — retorna <c>NotAssessed</c> enquanto não há bridge real).
///
/// Standards configuráveis via <c>EnabledStandards</c>. Padrão: os 8 standards implementados
/// (SOC2, ISO27001, PCI-DSS, HIPAA, GDPR, FedRAMP, NIS2, CMMC).
///
/// Classifica cada serviço por <c>CoverageLevel</c>:
/// - <c>Full</c>    — todos os standards activos avaliados (score = 100%)
/// - <c>Partial</c> — ≥ <c>PartialThresholdPct</c>% dos standards avaliados
/// - <c>Minimal</c> — ≥ 1 standard avaliado mas abaixo de Partial
/// - <c>None</c>    — nenhum standard avaliado (blind spot completo)
///
/// Produz:
/// - distribuição global de CoverageLevel no tenant
/// - top serviços com maior gap de compliance (piores primeiro)
/// - score de compliance agregado por standard (quantos serviços avaliaram)
/// - score médio de cobertura global do tenant
///
/// Orienta Auditor, Platform Admin e Executive na visão transversal de compliance.
///
/// Wave U.1 — Compliance Coverage Matrix Report (ChangeGovernance Compliance).
/// </summary>
public static class GetComplianceCoverageMatrixReport
{
    /// <summary>Standards activos por defeito — os 8 implementados na plataforma.</summary>
    public static readonly IReadOnlyList<string> DefaultEnabledStandards =
        ["SOC2", "ISO27001", "PCI-DSS", "HIPAA", "GDPR", "FedRAMP", "NIS2", "CMMC"];

    private const decimal DefaultPartialThresholdPct = 50m;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal para obter serviços activos (7–180, default 30).</para>
    /// <para><c>TopGapCount</c>: número máximo de serviços com maior gap a listar (1–100, default 10).</para>
    /// <para><c>PartialThresholdPct</c>: threshold (%) para classificação Partial (1–99, default 50).</para>
    /// <para><c>EnabledStandards</c>: standards activos a considerar (null = todos os 8 padrões).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int TopGapCount = 10,
        decimal PartialThresholdPct = DefaultPartialThresholdPct,
        IReadOnlyList<string>? EnabledStandards = null) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Nível de cobertura de compliance de um serviço.</summary>
    public enum CoverageLevel
    {
        /// <summary>Todos os standards activos avaliados.</summary>
        Full,
        /// <summary>≥ PartialThreshold% dos standards avaliados.</summary>
        Partial,
        /// <summary>≥ 1 standard avaliado mas abaixo de Partial.</summary>
        Minimal,
        /// <summary>Nenhum standard avaliado — blind spot completo.</summary>
        None
    }

    /// <summary>Distribuição global de serviços por CoverageLevel.</summary>
    public sealed record CoverageLevelDistribution(
        int FullCount,
        int PartialCount,
        int MinimalCount,
        int NoneCount);

    /// <summary>Score de compliance de um standard para o tenant.</summary>
    public sealed record StandardCoverageEntry(
        string Standard,
        int AssessedServiceCount,
        int TotalServiceCount,
        decimal CoveredPct);

    /// <summary>Cobertura de compliance de um serviço por standard.</summary>
    public sealed record ServiceCoverageEntry(
        string ServiceName,
        decimal CoverageScorePct,
        CoverageLevel Level,
        IReadOnlyDictionary<string, ComplianceCoverageStatus> ByStandard);

    /// <summary>Resultado do relatório de matriz de cobertura de compliance.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalServicesAnalyzed,
        int TotalStandardsActive,
        decimal TenantAvgCoverageScorePct,
        CoverageLevelDistribution LevelDistribution,
        IReadOnlyList<StandardCoverageEntry> ByStandard,
        IReadOnlyList<ServiceCoverageEntry> TopGapServices,
        IReadOnlyList<ServiceCoverageEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 180);
            RuleFor(q => q.TopGapCount).InclusiveBetween(1, 100);
            RuleFor(q => q.PartialThresholdPct).InclusiveBetween(1m, 99m);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IReleaseRepository _releaseRepo;
        private readonly IComplianceServiceCoverageReader _coverageReader;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IReleaseRepository releaseRepo,
            IComplianceServiceCoverageReader coverageReader,
            IDateTimeProvider clock)
        {
            _releaseRepo = Guard.Against.Null(releaseRepo);
            _coverageReader = Guard.Against.Null(coverageReader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);
            var tenantId = Guid.Parse(query.TenantId);
            var activeStandards = (query.EnabledStandards ?? DefaultEnabledStandards)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (activeStandards.Count == 0)
                activeStandards = [.. DefaultEnabledStandards];

            // 1. Derive active services from releases in period
            var releases = await _releaseRepo.ListInRangeAsync(
                from, now, environment: null, tenantId: tenantId, cancellationToken: cancellationToken);

            var serviceNames = releases
                .Select(r => r.ServiceName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();

            // 2. Get per-service, per-standard coverage assessments
            var coverageData = await _coverageReader.ListCoverageAsync(
                query.TenantId, from, now, cancellationToken);

            // Build lookup: service → standard → status
            var coverageMap = coverageData
                .GroupBy(c => c.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(
                        c => c.Standard,
                        c => c.Status,
                        StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

            // 3. Build per-service entries
            var entries = new List<ServiceCoverageEntry>();

            foreach (var svc in serviceNames)
            {
                var byStd = new Dictionary<string, ComplianceCoverageStatus>(StringComparer.OrdinalIgnoreCase);
                int assessedCount = 0;

                coverageMap.TryGetValue(svc, out var svcCoverage);

                foreach (var std in activeStandards)
                {
                    var status = (svcCoverage != null && svcCoverage.TryGetValue(std, out var st))
                        ? st
                        : ComplianceCoverageStatus.NotAssessed;

                    byStd[std] = status;

                    if (status != ComplianceCoverageStatus.NotAssessed)
                        assessedCount++;
                }

                decimal coveragePct = activeStandards.Count > 0
                    ? Math.Round((decimal)assessedCount / activeStandards.Count * 100m, 2)
                    : 0m;

                entries.Add(new ServiceCoverageEntry(
                    ServiceName: svc,
                    CoverageScorePct: coveragePct,
                    Level: ClassifyLevel(coveragePct, query.PartialThresholdPct),
                    ByStandard: byStd));
            }

            if (entries.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    LookbackDays: query.LookbackDays,
                    TotalServicesAnalyzed: 0,
                    TotalStandardsActive: activeStandards.Count,
                    TenantAvgCoverageScorePct: 0m,
                    LevelDistribution: new CoverageLevelDistribution(0, 0, 0, 0),
                    ByStandard: activeStandards.Select(s => new StandardCoverageEntry(s, 0, 0, 0m)).ToList(),
                    TopGapServices: [],
                    AllServices: []));
            }

            // 4. Aggregate level distribution
            int fullCount = entries.Count(e => e.Level == CoverageLevel.Full);
            int partialCount = entries.Count(e => e.Level == CoverageLevel.Partial);
            int minimalCount = entries.Count(e => e.Level == CoverageLevel.Minimal);
            int noneCount = entries.Count(e => e.Level == CoverageLevel.None);

            decimal tenantAvg = Math.Round(entries.Average(e => e.CoverageScorePct), 2);

            // 5. Per-standard aggregation
            var byStandardEntries = activeStandards
                .Select(std =>
                {
                    int assessed = entries.Count(e =>
                        e.ByStandard.TryGetValue(std, out var s) && s != ComplianceCoverageStatus.NotAssessed);
                    int total = entries.Count;
                    decimal pct = total > 0 ? Math.Round((decimal)assessed / total * 100m, 2) : 0m;
                    return new StandardCoverageEntry(std, assessed, total, pct);
                })
                .ToList();

            // 6. Top gap services (worst coverage first)
            var topGap = entries
                .OrderBy(e => e.CoverageScorePct)
                .ThenBy(e => e.ServiceName)
                .Take(query.TopGapCount)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalServicesAnalyzed: entries.Count,
                TotalStandardsActive: activeStandards.Count,
                TenantAvgCoverageScorePct: tenantAvg,
                LevelDistribution: new CoverageLevelDistribution(fullCount, partialCount, minimalCount, noneCount),
                ByStandard: byStandardEntries,
                TopGapServices: topGap,
                AllServices: entries.OrderBy(e => e.ServiceName).ToList()));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static CoverageLevel ClassifyLevel(decimal scorePct, decimal partialThreshold) =>
            scorePct >= 100m ? CoverageLevel.Full
            : scorePct >= partialThreshold ? CoverageLevel.Partial
            : scorePct > 0m ? CoverageLevel.Minimal
            : CoverageLevel.None;
    }
}
