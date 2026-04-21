using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseSuccessRateReport;

/// <summary>
/// Feature: GetReleaseSuccessRateReport — taxa de sucesso de releases por serviço e ambiente.
///
/// Agrega todas as releases de um tenant num período e produz, por serviço:
/// - taxa global de sucesso, falha e rollback
/// - distribuição de releases por DeploymentStatus (Succeeded/Failed/RolledBack/Running/Pending)
/// - distribuição de releases por ambiente (Production, Staging, etc.)
/// - top serviços com maior taxa de falha (para ação prioritária)
/// - tier de sucesso: Elite / High / Medium / Low
///
/// Complementa o GetTeamChangeVelocityReport (Wave M.2, por equipa) com granularidade
/// ao nível de serviço individual e perspetiva de ambiente, facilitando identificação de
/// serviços problemáticos para Engineer e Tech Lead.
///
/// Wave P.3 — Release Success Rate Report (ChangeGovernance ChangeIntelligence).
/// </summary>
public static class GetReleaseSuccessRateReport
{
    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–365, default 90).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente (null = todos).</para>
    /// <para><c>MaxServices</c>: número máximo de serviços no ranking de falha (1–200, default 20).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        string? Environment = null,
        int MaxServices = 20) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Classificação de tier de taxa de sucesso de releases.</summary>
    public enum SuccessRateTier
    {
        /// <summary>Taxa de sucesso ≥ 99% — excelência operacional.</summary>
        Elite,
        /// <summary>Taxa de sucesso ≥ 95% — alto desempenho.</summary>
        High,
        /// <summary>Taxa de sucesso ≥ 80% — desempenho médio.</summary>
        Medium,
        /// <summary>Taxa de sucesso &lt; 80% — necessita ação urgente.</summary>
        Low
    }

    /// <summary>Distribuição de releases por DeploymentStatus.</summary>
    public sealed record StatusDistribution(
        int PendingCount,
        int RunningCount,
        int SucceededCount,
        int FailedCount,
        int RolledBackCount);

    /// <summary>Distribuição de releases por ambiente.</summary>
    public sealed record EnvironmentEntry(
        string Environment,
        int TotalReleases,
        int SucceededCount,
        int FailedCount,
        decimal SuccessRatePercent);

    /// <summary>Métricas de sucesso de releases de um serviço.</summary>
    public sealed record ServiceSuccessRateEntry(
        string ServiceName,
        int TotalReleases,
        int SucceededCount,
        int FailedCount,
        int RolledBackCount,
        decimal SuccessRatePercent,
        decimal FailureRatePercent,
        decimal RollbackRatePercent,
        SuccessRateTier SuccessRateTier);

    /// <summary>Resultado do relatório de taxa de sucesso de releases.</summary>
    public sealed record Report(
        int TotalReleases,
        int TotalServicesWithReleases,
        decimal GlobalSuccessRatePercent,
        decimal GlobalFailureRatePercent,
        decimal GlobalRollbackRatePercent,
        SuccessRateTier GlobalSuccessRateTier,
        StatusDistribution ByStatus,
        IReadOnlyList<EnvironmentEntry> ByEnvironment,
        IReadOnlyList<ServiceSuccessRateEntry> TopServicesByFailureRate,
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
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var to = clock.UtcNow;
            var from = to.AddDays(-query.LookbackDays);

            if (!Guid.TryParse(query.TenantId, out var tenantGuid))
                return Error.Validation("tenantId.invalidFormat", "TenantId must be a valid GUID.");

            var releases = await releaseRepository.ListInRangeAsync(
                from, to, query.Environment, tenantGuid, cancellationToken);

            var total = releases.Count;

            if (total == 0)
            {
                return Result<Report>.Success(new Report(
                    TotalReleases: 0,
                    TotalServicesWithReleases: 0,
                    GlobalSuccessRatePercent: 0m,
                    GlobalFailureRatePercent: 0m,
                    GlobalRollbackRatePercent: 0m,
                    GlobalSuccessRateTier: SuccessRateTier.Low,
                    ByStatus: new StatusDistribution(0, 0, 0, 0, 0),
                    ByEnvironment: [],
                    TopServicesByFailureRate: [],
                    TenantId: query.TenantId,
                    LookbackDays: query.LookbackDays,
                    From: from,
                    To: to,
                    GeneratedAt: clock.UtcNow));
            }

            // Global status distribution
            var succeededCount = releases.Count(r => r.Status == DeploymentStatus.Succeeded);
            var failedCount = releases.Count(r => r.Status == DeploymentStatus.Failed);
            var rolledBackCount = releases.Count(r => r.Status == DeploymentStatus.RolledBack);
            var pendingCount = releases.Count(r => r.Status == DeploymentStatus.Pending);
            var runningCount = releases.Count(r => r.Status == DeploymentStatus.Running);

            var globalSuccessRate = Math.Round((decimal)succeededCount / total * 100m, 2);
            var globalFailureRate = Math.Round((decimal)failedCount / total * 100m, 2);
            var globalRollbackRate = Math.Round((decimal)rolledBackCount / total * 100m, 2);

            var byStatus = new StatusDistribution(pendingCount, runningCount, succeededCount, failedCount, rolledBackCount);

            // By environment
            var byEnvironment = releases
                .GroupBy(r => r.Environment, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var envTotal = g.Count();
                    var envSucceeded = g.Count(r => r.Status == DeploymentStatus.Succeeded);
                    var envFailed = g.Count(r => r.Status == DeploymentStatus.Failed);
                    var envSuccessRate = Math.Round((decimal)envSucceeded / envTotal * 100m, 2);
                    return new EnvironmentEntry(
                        Environment: g.Key,
                        TotalReleases: envTotal,
                        SucceededCount: envSucceeded,
                        FailedCount: envFailed,
                        SuccessRatePercent: envSuccessRate);
                })
                .OrderByDescending(e => e.TotalReleases)
                .ThenBy(e => e.Environment)
                .ToList();

            // Per-service metrics
            var byService = releases
                .GroupBy(r => r.ServiceName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var svcTotal = g.Count();
                    var svcSucceeded = g.Count(r => r.Status == DeploymentStatus.Succeeded);
                    var svcFailed = g.Count(r => r.Status == DeploymentStatus.Failed);
                    var svcRolledBack = g.Count(r => r.Status == DeploymentStatus.RolledBack);
                    var svcSuccessRate = Math.Round((decimal)svcSucceeded / svcTotal * 100m, 2);
                    var svcFailureRate = Math.Round((decimal)svcFailed / svcTotal * 100m, 2);
                    var svcRollbackRate = Math.Round((decimal)svcRolledBack / svcTotal * 100m, 2);
                    return new ServiceSuccessRateEntry(
                        ServiceName: g.Key,
                        TotalReleases: svcTotal,
                        SucceededCount: svcSucceeded,
                        FailedCount: svcFailed,
                        RolledBackCount: svcRolledBack,
                        SuccessRatePercent: svcSuccessRate,
                        FailureRatePercent: svcFailureRate,
                        RollbackRatePercent: svcRollbackRate,
                        SuccessRateTier: ClassifyTier(svcSuccessRate));
                })
                .ToList();

            // Top services by failure rate (descending), limited to MaxServices
            var topByFailure = byService
                .OrderByDescending(e => e.FailureRatePercent)
                .ThenByDescending(e => e.FailedCount)
                .ThenBy(e => e.ServiceName)
                .Take(query.MaxServices)
                .ToList();

            return Result<Report>.Success(new Report(
                TotalReleases: total,
                TotalServicesWithReleases: byService.Count,
                GlobalSuccessRatePercent: globalSuccessRate,
                GlobalFailureRatePercent: globalFailureRate,
                GlobalRollbackRatePercent: globalRollbackRate,
                GlobalSuccessRateTier: ClassifyTier(globalSuccessRate),
                ByStatus: byStatus,
                ByEnvironment: byEnvironment,
                TopServicesByFailureRate: topByFailure,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                From: from,
                To: to,
                GeneratedAt: clock.UtcNow));
        }

        private static SuccessRateTier ClassifyTier(decimal successRate) =>
            successRate >= 99m ? SuccessRateTier.Elite
            : successRate >= 95m ? SuccessRateTier.High
            : successRate >= 80m ? SuccessRateTier.Medium
            : SuccessRateTier.Low;
    }
}
