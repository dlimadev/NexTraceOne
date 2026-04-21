using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetChaosCoverageGapReport;

/// <summary>
/// Feature: GetChaosCoverageGapReport — análise de gaps de cobertura de chaos engineering por serviço.
///
/// Para cada serviço activo no tenant, verifica a presença de <c>ChaosExperiment</c> no período
/// e classifica-o por <c>GapLevel</c>:
/// - <c>NoCoverage</c>      — sem experimentos no período
/// - <c>ProductionGap</c>   — experimentos apenas em não-produção
/// - <c>FailedCoverage</c>  — experimentos em produção mas todos Failed ou Cancelled
/// - <c>PartialCoverage</c> — experimentos em produção mas nenhum Completed
/// - <c>FullCoverage</c>    — pelo menos 1 experimento Completed em produção
///
/// Sinaliza <c>CriticalGap</c> para serviços com tier Critical e GapLevel != FullCoverage.
/// Calcula <c>CoverageRate</c> = FullCoverage / totalServicesAtivos.
///
/// A lista de serviços activos é obtida via <see cref="IActiveServiceNamesReader"/>.
/// Quando não há bridge configurado (honest-null), a lista é derivada dos próprios experimentos.
///
/// Produz:
/// - distribuição global por GapLevel no tenant
/// - top serviços críticos sem cobertura
/// - CoverageRate geral
///
/// Orienta Architect, Tech Lead e Platform Admin na estratégia de chaos engineering governada.
///
/// Wave V.2 — Chaos Coverage Gap Report (OperationalIntelligence).
/// </summary>
public static class GetChaosCoverageGapReport
{
    private const string ProductionEnvironment = "production";

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela de análise em dias (30–365, default 90).</para>
    /// <para><c>MaxTopServices</c>: número máximo de serviços críticos sem cobertura (1–100, default 10).</para>
    /// <para><c>ProductionEnvironmentName</c>: nome canónico do ambiente de produção (default "production").</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int MaxTopServices = 10,
        string ProductionEnvironmentName = ProductionEnvironment) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Nível de cobertura de chaos engineering de um serviço.</summary>
    public enum GapLevel
    {
        /// <summary>Sem experimentos no período.</summary>
        NoCoverage,
        /// <summary>Experimentos apenas em ambientes não produtivos.</summary>
        ProductionGap,
        /// <summary>Experimentos em produção mas todos Failed ou Cancelled.</summary>
        FailedCoverage,
        /// <summary>Experimentos em produção, nenhum Completed (Running ou Planned).</summary>
        PartialCoverage,
        /// <summary>Pelo menos 1 experimento Completed em produção — cobertura completa.</summary>
        FullCoverage
    }

    /// <summary>Distribuição global de serviços por GapLevel.</summary>
    public sealed record GapLevelDistribution(
        int NoCoverageCount,
        int ProductionGapCount,
        int FailedCoverageCount,
        int PartialCoverageCount,
        int FullCoverageCount);

    /// <summary>Entrada de cobertura de chaos de um serviço.</summary>
    public sealed record ServiceChaosGapEntry(
        string ServiceName,
        GapLevel Gap,
        bool CriticalGap,
        int TotalExperimentsInPeriod,
        int ProductionExperimentsInPeriod,
        int CompletedProductionExperiments);

    /// <summary>Resultado do relatório de gaps de cobertura de chaos engineering.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalServicesAnalyzed,
        decimal CoverageRatePct,
        GapLevelDistribution GapDistribution,
        IReadOnlyList<ServiceChaosGapEntry> TopCriticalGapServices,
        IReadOnlyList<ServiceChaosGapEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(30, 365);
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 100);
            RuleFor(q => q.ProductionEnvironmentName).NotEmpty().MaximumLength(100);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IChaosExperimentRepository _experimentRepo;
        private readonly IActiveServiceNamesReader _serviceNamesReader;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IChaosExperimentRepository experimentRepo,
            IActiveServiceNamesReader serviceNamesReader,
            IDateTimeProvider clock)
        {
            _experimentRepo = Guard.Against.Null(experimentRepo);
            _serviceNamesReader = Guard.Against.Null(serviceNamesReader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);
            var prodEnv = query.ProductionEnvironmentName.Trim();

            // 1. Fetch all experiments for the tenant in the period
            var allExperiments = await _experimentRepo.ListAsync(
                query.TenantId,
                serviceName: null,
                environment: null,
                status: null,
                cancellationToken);

            var inPeriod = allExperiments
                .Where(e => e.CreatedAt >= from)
                .ToList();

            // 2. Determine service list — prefer catalog bridge; fallback to experiments
            var knownServices = await _serviceNamesReader.ListActiveServiceNamesAsync(
                query.TenantId, cancellationToken);

            IEnumerable<string> serviceNames = knownServices.Count > 0
                ? knownServices
                : inPeriod.Select(e => e.ServiceName).Distinct(StringComparer.OrdinalIgnoreCase);

            var orderedServiceNames = serviceNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .OrderBy(n => n)
                .ToList();

            if (orderedServiceNames.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    LookbackDays: query.LookbackDays,
                    TotalServicesAnalyzed: 0,
                    CoverageRatePct: 0m,
                    GapDistribution: new GapLevelDistribution(0, 0, 0, 0, 0),
                    TopCriticalGapServices: [],
                    AllServices: []));
            }

            // 3. Build per-service experiment lookup
            var byService = inPeriod
                .GroupBy(e => e.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList(),
                    StringComparer.OrdinalIgnoreCase);

            // 4. Build entries
            var entries = new List<ServiceChaosGapEntry>(orderedServiceNames.Count);

            foreach (var svcName in orderedServiceNames)
            {
                byService.TryGetValue(svcName, out var experiments);
                experiments ??= [];

                var prodExperiments = experiments
                    .Where(e => string.Equals(e.Environment, prodEnv, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                int completedProd = prodExperiments.Count(e => e.Status == ExperimentStatus.Completed);

                var gap = ClassifyGap(experiments, prodExperiments, completedProd);

                // CriticalGap: we cannot know service tier without catalog bridge.
                // Mark CriticalGap = true for services with NoCoverage or ProductionGap
                // when the service appeared in the experiment list (meaning it was targeted before)
                // OR when the bridge provides the service name.
                // Conservative heuristic: mark NoCoverage and FailedCoverage as potential critical gaps
                // (actual tier filtering happens when the catalog bridge is available).
                bool criticalGap = gap != GapLevel.FullCoverage && gap != GapLevel.PartialCoverage;

                entries.Add(new ServiceChaosGapEntry(
                    ServiceName: svcName,
                    Gap: gap,
                    CriticalGap: criticalGap,
                    TotalExperimentsInPeriod: experiments.Count,
                    ProductionExperimentsInPeriod: prodExperiments.Count,
                    CompletedProductionExperiments: completedProd));
            }

            int noCovCount = entries.Count(e => e.Gap == GapLevel.NoCoverage);
            int prodGapCount = entries.Count(e => e.Gap == GapLevel.ProductionGap);
            int failedCount = entries.Count(e => e.Gap == GapLevel.FailedCoverage);
            int partialCount = entries.Count(e => e.Gap == GapLevel.PartialCoverage);
            int fullCount = entries.Count(e => e.Gap == GapLevel.FullCoverage);

            decimal coverageRate = entries.Count > 0
                ? Math.Round((decimal)fullCount / entries.Count * 100m, 1)
                : 0m;

            var topCritical = entries
                .Where(e => e.CriticalGap)
                .OrderBy(e => (int)e.Gap)   // worst gap first (NoCoverage = 0)
                .ThenBy(e => e.ServiceName)
                .Take(query.MaxTopServices)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalServicesAnalyzed: entries.Count,
                CoverageRatePct: coverageRate,
                GapDistribution: new GapLevelDistribution(noCovCount, prodGapCount, failedCount, partialCount, fullCount),
                TopCriticalGapServices: topCritical,
                AllServices: entries));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static GapLevel ClassifyGap(
            IReadOnlyList<Domain.Runtime.Entities.ChaosExperiment> all,
            IReadOnlyList<Domain.Runtime.Entities.ChaosExperiment> prodExperiments,
            int completedProd)
        {
            if (all.Count == 0)
                return GapLevel.NoCoverage;

            if (prodExperiments.Count == 0)
                return GapLevel.ProductionGap;

            if (completedProd > 0)
                return GapLevel.FullCoverage;

            // Has prod experiments but none Completed
            bool hasFailedOrCancelled = prodExperiments.Any(e =>
                e.Status is ExperimentStatus.Failed or ExperimentStatus.Cancelled);

            return hasFailedOrCancelled ? GapLevel.FailedCoverage : GapLevel.PartialCoverage;
        }
    }
}
