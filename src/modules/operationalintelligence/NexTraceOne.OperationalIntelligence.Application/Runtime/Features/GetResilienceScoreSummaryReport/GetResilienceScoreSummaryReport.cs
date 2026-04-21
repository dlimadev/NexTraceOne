using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetResilienceScoreSummaryReport;

/// <summary>
/// Feature: GetResilienceScoreSummaryReport — sumário agregado de scores de resiliência.
///
/// Agrega todos os ResilienceReport disponíveis (opcionalmente filtrados por serviço)
/// e produz:
/// - score médio de resiliência do tenant (0–100)
/// - distribuição de relatórios por tier de score (Poor / Fair / Good / Excellent)
/// - top serviços com maior score médio (mais resilientes)
/// - top serviços com menor score médio (mais vulneráveis)
/// - tempo médio de recuperação (AvgRecoveryTimeSeconds)
/// - desvio médio de blast radius (percentual real vs teórico)
/// - distribuição por tipo de experimento de chaos
///
/// Serve Tech Lead, Engineer e Platform Admin como painel de qualidade de resiliência
/// pós-chaos, complementando o GetChaosExperimentReport (Wave K.1) e o
/// GetOperationalReadinessReport (Wave L.3).
///
/// Wave P.2 — Resilience Score Summary Report (OperationalIntelligence Runtime).
/// </summary>
public static class GetResilienceScoreSummaryReport
{
    /// <summary>
    /// <para><c>ServiceName</c>: filtra relatórios de um serviço específico (opcional).</para>
    /// <para><c>MaxTopServices</c>: número máximo de serviços no ranking de resiliência (1–100, default 10).</para>
    /// <para><c>MaxTopExperimentTypes</c>: número máximo de tipos de experimento no ranking (1–50, default 10).</para>
    /// </summary>
    public sealed record Query(
        string? ServiceName = null,
        int MaxTopServices = 10,
        int MaxTopExperimentTypes = 10) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Classificação de tier de score de resiliência.</summary>
    public enum ResilienceScoreTier
    {
        /// <summary>Score &lt; 40 — serviço com resiliência fraca.</summary>
        Poor,
        /// <summary>Score 40–64 — resiliência moderada com pontos de melhoria.</summary>
        Fair,
        /// <summary>Score 65–84 — boa resiliência com pequenos gaps.</summary>
        Good,
        /// <summary>Score ≥ 85 — serviço altamente resiliente.</summary>
        Excellent
    }

    /// <summary>Distribuição de relatórios por tier de score de resiliência.</summary>
    public sealed record ScoreTierDistribution(
        int PoorCount,
        int FairCount,
        int GoodCount,
        int ExcellentCount);

    /// <summary>Entrada de um serviço no ranking de resiliência.</summary>
    public sealed record ServiceResilienceEntry(
        string ServiceName,
        int ReportCount,
        decimal AvgResilienceScore,
        ResilienceScoreTier ScoreTier,
        decimal? AvgRecoveryTimeSeconds,
        decimal? AvgBlastRadiusDeviation);

    /// <summary>Distribuição de relatórios por tipo de experimento de chaos.</summary>
    public sealed record ExperimentTypeEntry(
        string ExperimentType,
        int ReportCount,
        decimal AvgResilienceScore);

    /// <summary>Resultado do relatório de sumário de resiliência.</summary>
    public sealed record Report(
        int TotalReports,
        int TotalServicesAnalyzed,
        decimal OverallAvgResilienceScore,
        ResilienceScoreTier OverallScoreTier,
        ScoreTierDistribution ScoreDistribution,
        decimal? AvgRecoveryTimeSeconds,
        decimal? AvgBlastRadiusDeviationPercent,
        IReadOnlyList<ServiceResilienceEntry> TopResilientServices,
        IReadOnlyList<ServiceResilienceEntry> TopVulnerableServices,
        IReadOnlyList<ExperimentTypeEntry> ByExperimentType,
        string? ServiceNameFilter,
        DateTimeOffset GeneratedAt);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 100);
            RuleFor(q => q.MaxTopExperimentTypes).InclusiveBetween(1, 50);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IResilienceReportRepository resilienceReportRepository,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);

            var reports = await resilienceReportRepository.ListByServiceAsync(
                query.ServiceName, cancellationToken);

            if (reports.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TotalReports: 0,
                    TotalServicesAnalyzed: 0,
                    OverallAvgResilienceScore: 0m,
                    OverallScoreTier: ResilienceScoreTier.Poor,
                    ScoreDistribution: new ScoreTierDistribution(0, 0, 0, 0),
                    AvgRecoveryTimeSeconds: null,
                    AvgBlastRadiusDeviationPercent: null,
                    TopResilientServices: [],
                    TopVulnerableServices: [],
                    ByExperimentType: [],
                    ServiceNameFilter: query.ServiceName,
                    GeneratedAt: clock.UtcNow));
            }

            // Overall aggregates
            var overallAvgScore = Math.Round(reports.Average(r => (decimal)r.ResilienceScore), 1);
            var overallTier = ClassifyTier(overallAvgScore);

            // Score distribution
            var scoreDistribution = new ScoreTierDistribution(
                PoorCount: reports.Count(r => ClassifyTier(r.ResilienceScore) == ResilienceScoreTier.Poor),
                FairCount: reports.Count(r => ClassifyTier(r.ResilienceScore) == ResilienceScoreTier.Fair),
                GoodCount: reports.Count(r => ClassifyTier(r.ResilienceScore) == ResilienceScoreTier.Good),
                ExcellentCount: reports.Count(r => ClassifyTier(r.ResilienceScore) == ResilienceScoreTier.Excellent));

            // Recovery time and blast radius averages (only non-null values)
            var recoveryValues = reports
                .Where(r => r.RecoveryTimeSeconds.HasValue)
                .Select(r => (decimal)r.RecoveryTimeSeconds!.Value)
                .ToList();
            var avgRecovery = recoveryValues.Count > 0
                ? Math.Round(recoveryValues.Average(), 1)
                : (decimal?)null;

            var blastDeviationValues = reports
                .Where(r => r.BlastRadiusDeviation.HasValue)
                .Select(r => r.BlastRadiusDeviation!.Value)
                .ToList();
            var avgBlastDeviation = blastDeviationValues.Count > 0
                ? Math.Round(blastDeviationValues.Average(), 2)
                : (decimal?)null;

            // Per-service aggregates
            var byService = reports
                .GroupBy(r => r.ServiceName)
                .Select(g =>
                {
                    var avgScore = Math.Round(g.Average(r => (decimal)r.ResilienceScore), 1);
                    var svcRecovery = g.Where(r => r.RecoveryTimeSeconds.HasValue)
                                       .Select(r => (decimal)r.RecoveryTimeSeconds!.Value)
                                       .ToList();
                    var svcBlast = g.Where(r => r.BlastRadiusDeviation.HasValue)
                                    .Select(r => r.BlastRadiusDeviation!.Value)
                                    .ToList();
                    return new ServiceResilienceEntry(
                        ServiceName: g.Key,
                        ReportCount: g.Count(),
                        AvgResilienceScore: avgScore,
                        ScoreTier: ClassifyTier(avgScore),
                        AvgRecoveryTimeSeconds: svcRecovery.Count > 0 ? Math.Round(svcRecovery.Average(), 1) : null,
                        AvgBlastRadiusDeviation: svcBlast.Count > 0 ? Math.Round(svcBlast.Average(), 2) : null);
                })
                .ToList();

            var topResilient = byService
                .OrderByDescending(e => e.AvgResilienceScore)
                .ThenBy(e => e.ServiceName)
                .Take(query.MaxTopServices)
                .ToList();

            var topVulnerable = byService
                .OrderBy(e => e.AvgResilienceScore)
                .ThenBy(e => e.ServiceName)
                .Take(query.MaxTopServices)
                .ToList();

            // Distribution by experiment type
            var byExperimentType = reports
                .GroupBy(r => r.ExperimentType)
                .Select(g => new ExperimentTypeEntry(
                    ExperimentType: g.Key,
                    ReportCount: g.Count(),
                    AvgResilienceScore: Math.Round(g.Average(r => (decimal)r.ResilienceScore), 1)))
                .OrderByDescending(e => e.ReportCount)
                .ThenBy(e => e.ExperimentType)
                .Take(query.MaxTopExperimentTypes)
                .ToList();

            return Result<Report>.Success(new Report(
                TotalReports: reports.Count,
                TotalServicesAnalyzed: byService.Count,
                OverallAvgResilienceScore: overallAvgScore,
                OverallScoreTier: overallTier,
                ScoreDistribution: scoreDistribution,
                AvgRecoveryTimeSeconds: avgRecovery,
                AvgBlastRadiusDeviationPercent: avgBlastDeviation,
                TopResilientServices: topResilient,
                TopVulnerableServices: topVulnerable,
                ByExperimentType: byExperimentType,
                ServiceNameFilter: query.ServiceName,
                GeneratedAt: clock.UtcNow));
        }

        private static ResilienceScoreTier ClassifyTier(decimal score) =>
            score >= 85m ? ResilienceScoreTier.Excellent
            : score >= 65m ? ResilienceScoreTier.Good
            : score >= 40m ? ResilienceScoreTier.Fair
            : ResilienceScoreTier.Poor;
    }
}
