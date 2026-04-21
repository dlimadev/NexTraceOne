using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetSloServiceRankingReport;

/// <summary>
/// Feature: GetSloServiceRankingReport — relatório de ranking de conformidade SLO por serviço.
///
/// Agrega todas as observações de SLO de um tenant num período e classifica cada serviço por:
/// - percentagem de observações Met (compliance rate)
/// - número de breaches e warnings
/// - valor médio observado e alvo médio de SLO
/// - classificação de saúde de SLO: Excellent / Good / Struggling / Unknown
///
/// Permite a Tech Lead e Platform Admin identificar quais serviços cumprem ou violam os seus SLOs
/// e priorizar ações de melhoria de confiabilidade.
///
/// Wave N.1 — SLO Service Ranking Report (OperationalIntelligence Runtime).
/// </summary>
public static class GetSloServiceRankingReport
{
    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>Environment</c>: ambiente para filtrar (opcional — null = todos).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–365, default 30).</para>
    /// <para><c>MaxServices</c>: número máximo de serviços no ranking (1–200, default 50).</para>
    /// <para><c>ExcellentThreshold</c>: compliance rate mínima para Excellent (50–100, default 95.0).</para>
    /// <para><c>GoodThreshold</c>: compliance rate mínima para Good (0–99, default 80.0).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        string? Environment = null,
        int LookbackDays = 30,
        int MaxServices = 50,
        decimal ExcellentThreshold = 95.0m,
        decimal GoodThreshold = 80.0m) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Classificação de saúde de SLO de um serviço no período.</summary>
    public enum SloHealthTier
    {
        /// <summary>Compliance rate ≥ ExcellentThreshold — serviço exemplar.</summary>
        Excellent,
        /// <summary>Compliance rate ≥ GoodThreshold — serviço saudável.</summary>
        Good,
        /// <summary>Compliance rate < GoodThreshold — serviço com violações frequentes.</summary>
        Struggling,
        /// <summary>Sem observações no período — não avaliado.</summary>
        Unknown
    }

    /// <summary>Métricas de conformidade SLO de um serviço.</summary>
    public sealed record ServiceSloMetrics(
        string ServiceName,
        string? Environment,
        int TotalObservations,
        int MetCount,
        int WarningCount,
        int BreachedCount,
        decimal ComplianceRatePercent,
        decimal AvgObservedValue,
        decimal AvgSloTarget,
        SloHealthTier HealthTier);

    /// <summary>Resultado do relatório de ranking de SLO por serviço.</summary>
    public sealed record Report(
        int TotalServices,
        int ServicesExcellent,
        int ServicesGood,
        int ServicesStruggling,
        int TotalObservations,
        int TotalBreaches,
        decimal TenantAvgComplianceRate,
        IReadOnlyList<ServiceSloMetrics> ServiceRanking,
        string TenantId,
        int LookbackDays,
        DateTimeOffset From,
        DateTimeOffset To,
        DateTimeOffset GeneratedAt);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.MaxServices).InclusiveBetween(1, 200);
            RuleFor(q => q.ExcellentThreshold).InclusiveBetween(50m, 100m);
            RuleFor(q => q.GoodThreshold).InclusiveBetween(0m, 99m);
            RuleFor(q => q.ExcellentThreshold)
                .GreaterThan(q => q.GoodThreshold)
                .WithMessage("ExcellentThreshold must be greater than GoodThreshold.");
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        ISloObservationRepository sloObservationRepository,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var to = clock.UtcNow;
            var from = to.AddDays(-query.LookbackDays);

            var allObservations = await sloObservationRepository.ListByTenantAsync(
                query.TenantId, from, to, statusFilter: null, ct: cancellationToken);

            var observations = query.Environment is null
                ? allObservations
                : allObservations
                    .Where(o => string.Equals(o.Environment, query.Environment, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (observations.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TotalServices: 0,
                    ServicesExcellent: 0,
                    ServicesGood: 0,
                    ServicesStruggling: 0,
                    TotalObservations: 0,
                    TotalBreaches: 0,
                    TenantAvgComplianceRate: 0m,
                    ServiceRanking: [],
                    TenantId: query.TenantId,
                    LookbackDays: query.LookbackDays,
                    From: from,
                    To: to,
                    GeneratedAt: clock.UtcNow));
            }

            var totalBreaches = observations.Count(o => o.Status == SloObservationStatus.Breached);

            // Group by service + environment, compute per-service SLO metrics
            var byService = observations
                .GroupBy(o => (o.ServiceName, o.Environment))
                .Select(g =>
                {
                    var total = g.Count();
                    var met = g.Count(o => o.Status == SloObservationStatus.Met);
                    var warning = g.Count(o => o.Status == SloObservationStatus.Warning);
                    var breached = g.Count(o => o.Status == SloObservationStatus.Breached);
                    var complianceRate = Math.Round((decimal)met / total * 100m, 2);

                    return new ServiceSloMetrics(
                        ServiceName: g.Key.ServiceName,
                        Environment: g.Key.Environment,
                        TotalObservations: total,
                        MetCount: met,
                        WarningCount: warning,
                        BreachedCount: breached,
                        ComplianceRatePercent: complianceRate,
                        AvgObservedValue: Math.Round(g.Average(o => o.ObservedValue), 4),
                        AvgSloTarget: Math.Round(g.Average(o => o.SloTarget), 4),
                        HealthTier: ClassifyHealthTier(complianceRate, query.ExcellentThreshold, query.GoodThreshold));
                })
                .OrderByDescending(s => s.ComplianceRatePercent)
                .ThenBy(s => s.ServiceName)
                .Take(query.MaxServices)
                .ToList();

            var tenantAvgCompliance = byService.Count == 0
                ? 0m
                : Math.Round(byService.Average(s => s.ComplianceRatePercent), 2);

            return Result<Report>.Success(new Report(
                TotalServices: byService.Count,
                ServicesExcellent: byService.Count(s => s.HealthTier == SloHealthTier.Excellent),
                ServicesGood: byService.Count(s => s.HealthTier == SloHealthTier.Good),
                ServicesStruggling: byService.Count(s => s.HealthTier == SloHealthTier.Struggling),
                TotalObservations: observations.Count,
                TotalBreaches: totalBreaches,
                TenantAvgComplianceRate: tenantAvgCompliance,
                ServiceRanking: byService,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                From: from,
                To: to,
                GeneratedAt: clock.UtcNow));
        }

        private static SloHealthTier ClassifyHealthTier(
            decimal complianceRate, decimal excellentThreshold, decimal goodThreshold)
            => complianceRate >= excellentThreshold ? SloHealthTier.Excellent
             : complianceRate >= goodThreshold ? SloHealthTier.Good
             : SloHealthTier.Struggling;
    }
}
